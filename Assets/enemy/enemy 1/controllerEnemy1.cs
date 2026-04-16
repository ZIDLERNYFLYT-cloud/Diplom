using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }
    public State currentState = State.Idle;

    [Header("Настройки движения")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolRadius = 5f;
    public float stopDistance = 1.5f;
    public float detectionRange = 7f;

    [Header("Ссылки")]
    public Transform player;
    public Animator anim;
    public AudioSource audioSource;
    public AudioClip screamSound;

    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private bool hasScreamed = false;
    private float stateTimer;

    [Header("Настройки атаки")]
    public Transform attackPoint; // Пустая пустышка (GameObject) перед руками зомби
    public float attackRange = 0.5f; // Радиус удара
    public LayerMask playerLayer; // Выбери Layer "Player" в инспекторе
    public int damage = 10;

    void Start()
    {
        startPosition = transform.position;
        SetNewPatrolTarget();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // Считаем дистанцию только по горизонтали (X)
        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);
        // Для общей проверки (на всякий случай учитываем и высоту Y)
        float fullDistance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                UpdateIdleState(fullDistance);
                break;
            case State.Patrol:
                UpdatePatrolState(fullDistance);
                break;
            case State.Chase:
                UpdateChaseState(distanceToPlayer); // Передаем X-дистанцию
                break;
            case State.Attack:
                UpdateAttackState(distanceToPlayer); // Передаем X-дистанцию
                break;
        }
    }

    // --- Логика состояний ---

    void UpdateIdleState(float distance)
    {
        anim.SetBool("isWalking", false);
        stateTimer -= Time.deltaTime;

        if (distance < detectionRange) TransitionToChase();
        else if (stateTimer <= 0) currentState = State.Patrol;
    }

    void UpdatePatrolState(float distance)
    {
        anim.SetBool("isWalking", true);
        MoveTowards(patrolTarget, walkSpeed);

        if (distance < detectionRange) TransitionToChase();
        else if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
        {
            stateTimer = Random.Range(1f, 3f); // Пауза для естественности
            SetNewPatrolTarget();
            currentState = State.Idle;
        }
    }

    void UpdateChaseState(float xDistance)
    {
        anim.SetBool("isWalking", true);
        MoveTowards(player.position, chaseSpeed);

        // Если подошли вплотную по X
        if (xDistance <= stopDistance)
        {
            currentState = State.Attack;
        }
    }

    void UpdateAttackState(float distance)
    {
        anim.SetBool("isWalking", false);

        // Поворачиваемся к игроку только по горизонтали (ось Y)
        LookAtTarget(player.position);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            anim.SetTrigger("punch");
            stateTimer = 1.5f;
        }

        if (distance > stopDistance + 0.2f) currentState = State.Chase;
    }

    // --- Помощники ---

    void TransitionToChase()
    {
        if (!hasScreamed)
        {
            audioSource.PlayOneShot(screamSound);
            hasScreamed = true;
        }
        currentState = State.Chase;
    }

    void MoveTowards(Vector3 target, float speed)
    {
        // Движение только по оси X и Y (игнорируем Z для 2D-подобного движения)
        Vector3 targetPos = new Vector3(target.x, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Поворот в сторону цели
        LookAtTarget(target);
    }

    void SetNewPatrolTarget()
    {
        float randomX = Random.Range(-patrolRadius, patrolRadius);
        patrolTarget = startPosition + new Vector3(randomX, 0, 0);
    }

    void LookAtTarget(Vector3 target)
    {
        if (target.x > transform.position.x)
        {
            // Смотрим вправо
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            // Смотрим влево
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        // ПРИМЕЧАНИЕ: Если модель изначально повернута спиной, 
        // поменяй углы 90 и -90 местами или на 270 и 90.
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    public void EnemyAttackHit()
    {
        if (attackPoint == null) return;

        // Создаем невидимую сферу и проверяем, попал ли в нее игрок
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);

        foreach (Collider playerObj in hitPlayers)
        {
            Debug.Log("Попал по игроку!");

            
            playerObj.GetComponent<PlayerHealth>().TakeDamage(damage);
        }
    }
}