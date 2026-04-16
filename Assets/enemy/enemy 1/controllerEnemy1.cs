using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float randomOffset = 2f;
    [SerializeField] private float waitTimeAtEdge = 1f;

    [Header("Настройки обнаружения")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Настройки атаки")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;

    [Header("Настройки звука")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip screamSound;

    [Header("Настройки анимации")]
    [SerializeField] private Animator animator;

    private float leftBoundary;
    private float rightBoundary;
    private float currentWaitTime;
    private bool isWaiting;
    private bool isChasing;
    private bool isAttacking;
    private float lastAttackTime;
    private int facingDirection = 1;
    private float screamCooldown = 0f;

    private enum EnemyState
    {
        Idle,
        Walking,
        Chasing,
        Attacking
    }

    private EnemyState currentState = EnemyState.Walking;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("Player not found! Please assign Player transform in inspector.");
            }
        }

        float randomPatrolDistance = patrolDistance + Random.Range(-randomOffset, randomOffset);
        leftBoundary = transform.position.x - randomPatrolDistance;
        rightBoundary = transform.position.x + randomPatrolDistance;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (player == null) return;

        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer && !isChasing)
        {
            OnPlayerDetected();
        }

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        UpdateAnimation();
        UpdateFacingDirection();

        if (screamCooldown > 0)
            screamCooldown -= Time.deltaTime;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange, obstacleLayer))
        {
            if (hit.transform.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private void OnPlayerDetected()
    {
        if (!isChasing)
        {
            isChasing = true;
            currentState = EnemyState.Chasing;

            if (screamSound != null && audioSource != null && screamCooldown <= 0)
            {
                audioSource.PlayOneShot(screamSound);
                screamCooldown = 1f;
            }

            Debug.Log("Enemy detected player! Starting chase!");
        }
    }

    private void Patrol()
    {
        float currentX = transform.position.x;

        if (!isWaiting && (currentX <= leftBoundary || currentX >= rightBoundary))
        {
            isWaiting = true;
            currentWaitTime = waitTimeAtEdge;
            currentState = EnemyState.Idle;
            return;
        }

        if (isWaiting)
        {
            currentWaitTime -= Time.deltaTime;
            if (currentWaitTime <= 0)
            {
                isWaiting = false;
                currentState = EnemyState.Walking;
                facingDirection *= -1;
            }
            return;
        }

        currentState = EnemyState.Walking;
        float moveDirection = facingDirection;
        Vector3 movement = new Vector3(moveDirection * moveSpeed * Time.deltaTime, 0, 0);
        transform.position += movement;
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
            currentState = EnemyState.Attacking;
            return;
        }

        if (distanceToPlayer > detectionRange * 1.5f)
        {
            isChasing = false;
            currentState = EnemyState.Walking;
            Debug.Log("Enemy lost player, returning to patrol");
            return;
        }

        currentState = EnemyState.Chasing;
        float direction = player.position.x > transform.position.x ? 1 : -1;
        facingDirection = (int)direction; // Явное приведение типа

        Vector3 movement = new Vector3(direction * chaseSpeed * Time.deltaTime, 0, 0);
        transform.position += movement;
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        //if (player != null)
        //{
        //    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        //    if (playerHealth != null)
        //    {
        //        playerHealth.TakeDamage(attackDamage);
        //        Debug.Log($"Enemy attacked player for {attackDamage} damage!");
        //    }
        //}

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Invoke(nameof(ResetAttack), 0.5f);
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // Определяем текущую скорость для аниматора
        float currentAnimSpeed = 0f;
        if (currentState == EnemyState.Walking) currentAnimSpeed = 0.5f; // Для Walk
        if (currentState == EnemyState.Chasing) currentAnimSpeed = 1f;   // Для Walk (быстрее)
        if (currentState == EnemyState.Idle) currentAnimSpeed = 0f;      // Для Idle

        animator.SetFloat("Speed", currentAnimSpeed);
    }

    private void UpdateFacingDirection()
    {
        if (facingDirection != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDirection;
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(leftBoundary, transform.position.y - 1, transform.position.z),
                           new Vector3(leftBoundary, transform.position.y + 1, transform.position.z));
            Gizmos.DrawLine(new Vector3(rightBoundary, transform.position.y - 1, transform.position.z),
                           new Vector3(rightBoundary, transform.position.y + 1, transform.position.z));
        }
    }
}