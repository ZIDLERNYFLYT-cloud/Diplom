using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    // Добавляем состояние Death в enum
    public enum State { Idle, Patrol, Chase, Attack, Death }
    public State currentState = State.Idle;

    [Header("Настройки здоровья")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

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
    public AudioClip hitSound; // Звук при получении урона
    public AudioClip deathSound; // Звук смерти

    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private bool hasScreamed = false;
    private float stateTimer;

    [Header("Настройки атаки")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask playerLayer;
    public int damage = 10;

    private float lockedZ; // Переменная для хранения Z

    void Start()
    {
        currentHealth = maxHealth; // Инициализация здоровья
        startPosition = transform.position;
        SetNewPatrolTarget();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lockedZ = transform.position.z;
    }

    void Update()
    {
        if (isDead) return; // Если мертв, ничего не делаем

        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);
        float fullDistance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle: UpdateIdleState(fullDistance); break;
            case State.Patrol: UpdatePatrolState(fullDistance); break;
            case State.Chase: UpdateChaseState(distanceToPlayer); break;
            case State.Attack: UpdateAttackState(distanceToPlayer); break;
        }
      
    }

    // --- НОВЫЙ МЕТОД: ПОЛУЧЕНИЕ УРОНА ---
    public void TakeDamage(int damageAmount)
    {
        
        if (isDead) return;
        Debug.Log(gameObject.name + " получил урон: " + damageAmount); // Это должно появиться в консоли

        currentHealth -= damageAmount;
        

        if (audioSource && hitSound) audioSource.PlayOneShot(hitSound);

        if (currentHealth > 0) anim.SetTrigger("getHit");
        if (currentHealth <= 0) Die();
        if (currentState == State.Idle || currentState == State.Patrol)
        {
            TransitionToChase();
        }

        
    }

    void Die()
    {
        isDead = true;
        currentState = State.Death;

        // Звук смерти
        if (audioSource && deathSound) audioSource.PlayOneShot(deathSound);

        // Отключаем физику, чтобы тело не мешало игроку
        if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
        if (GetComponent<Rigidbody>()) GetComponent<Rigidbody>().isKinematic = true;

        // Анимация смерти
        int randomDeath = Random.Range(1, 3);
        //anim.SetInteger("deathType", randomDeath);
        anim.SetTrigger("die");

        Debug.Log("Враг повержен!");

        // Опционально: удалить объект через 5 секунд
        Destroy(gameObject, 5f);
    }

    // --- Логика состояний (Остается вашей) ---

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

        // Пускаем луч вперед на небольшое расстояние
        Vector3 direction = (patrolTarget.x > transform.position.x) ? Vector3.right : Vector3.left;
        RaycastHit hit;

        // Проверяем, нет ли стены впереди (на расстоянии 0.7 метра)
        // Убедитесь, что стены имеют слой, который вы укажете (например, "Ground")
        if (Physics.Raycast(transform.position + Vector3.up, direction, out hit, 0.7f))
        {
            // Если луч попал в стену - останавливаемся и меняем цель
            StopAndPickNewTarget();
            return;
        }

        MoveTowards(patrolTarget, walkSpeed);

        if (distance < detectionRange) TransitionToChase();
        else if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
        {
            StopAndPickNewTarget();
        }
    }

    void StopAndPickNewTarget()
    {
        stateTimer = Random.Range(1f, 3f);
        SetNewPatrolTarget();
        currentState = State.Idle;
    }

    void UpdateChaseState(float xDistance)
    {
        anim.SetBool("isWalking", true);
        MoveTowards(player.position, chaseSpeed);
        if (xDistance <= stopDistance) currentState = State.Attack;
    }

    void UpdateAttackState(float distance)
    {
        anim.SetBool("isWalking", false);
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
            if (audioSource && screamSound) audioSource.PlayOneShot(screamSound);
            hasScreamed = true;
        }
        currentState = State.Chase;
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 targetPos = new Vector3(target.x, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        LookAtTarget(target);
    }

    void SetNewPatrolTarget()
    {
        float randomX = Random.Range(-patrolRadius, patrolRadius);
        patrolTarget = startPosition + new Vector3(randomX, 0, 0);
    }

    void LookAtTarget(Vector3 target)
    {
        float targetY = (target.x > transform.position.x) ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, targetY, 0);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    public void EnemyAttackHit()
    {
        if (attackPoint == null || isDead) return;
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);
        foreach (Collider playerObj in hitPlayers)
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);
        }
    }
}