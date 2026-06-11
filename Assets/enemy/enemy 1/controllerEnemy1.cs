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

    [Header("Настройки атаки")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask playerLayer;
    public int damage = 10;

    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private bool hasScreamed = false;
    private float stateTimer;

    private HealthEnemy health; // Ссылка на компонент здоровья
    private bool isDead = false;

    void Start()
    {
        health = GetComponent<HealthEnemy>();
        if (health == null)
        {
            Debug.LogError("HealthEnemy компонент не найден на " + gameObject.name);
            return;
        }

        startPosition = transform.position;
        SetNewPatrolTarget();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // Если мёртв или скрипт отключен (HealthEnemy отключит его при смерти) — ничего не делаем
        if (isDead || health == null || health.IsDead()) return;

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

    public void TakeDamage(int damageAmount)
    {
        if (health != null && !health.IsDead())
        {
            health.TakeDamage(damageAmount);
            if (!health.IsDead() && (currentState == State.Idle || currentState == State.Patrol))
            {
                TransitionToChase();
            }
        }
    }

    void UpdateIdleState(float distance)
    {
        if (anim != null) anim.SetBool("isWalking", false);
        stateTimer -= Time.deltaTime;
        if (distance < detectionRange) TransitionToChase();
        else if (stateTimer <= 0) currentState = State.Patrol;
    }

    void UpdatePatrolState(float distance)
    {
        if (anim != null) anim.SetBool("isWalking", true);

        Vector3 direction = (patrolTarget.x > transform.position.x) ? Vector3.right : Vector3.left;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, direction, out hit, 0.7f))
        {
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
        if (anim != null) anim.SetBool("isWalking", true);
        MoveTowards(player.position, chaseSpeed);
        if (xDistance <= stopDistance) currentState = State.Attack;
    }

    void UpdateAttackState(float distance)
    {
        if (anim != null) anim.SetBool("isWalking", false);
        LookAtTarget(player.position);
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            if (anim != null) anim.SetTrigger("punch");
            stateTimer = 1.5f;
        }
        if (distance > stopDistance + 0.2f) currentState = State.Chase;
    }

    void TransitionToChase()
    {
        if (!hasScreamed)
        {
            if (audioSource != null && screamSound != null)
                audioSource.PlayOneShot(screamSound);
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

    // Этот метод вызывается из анимации атаки
    public void EnemyAttackHit()
    {
        if (isDead || (health != null && health.IsDead())) return;
        if (attackPoint == null) return;

        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);
        foreach (Collider playerObj in hitPlayers)
        {
            PlayerHealth healthPlayer = playerObj.GetComponent<PlayerHealth>();
            if (healthPlayer != null) healthPlayer.TakeDamage(damage);
        }
    }

    // Метод, который вызовет HealthEnemy перед смертью (если нужно дополнительно отключить что‑то)
    public void DisableBeforeDeath()
    {
        isDead = true;
        // Можно остановить анимацию движения
        if (anim != null) anim.SetBool("isWalking", false);
    }
}