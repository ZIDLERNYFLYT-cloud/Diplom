using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HealthEnemy))]
public class EnemyAI3D : MonoBehaviour
{
    [Header("Движение и Обнаружение")]
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public float chaseRange = 7f;
    public float attackRange = 1.5f;
    public float flipSpeed = 12f;

    [Header("Параметры Боя")]
    public float attackCooldown = 1.5f;
    public int attackDamage = 15;
    public float attackAnimationSpeed = 1.5f;
    public Transform attackPoint;
    public float attackRadius = 1f;
    public LayerMask playerLayer;

    [Header("Звуки")]
    public AudioSource audioSource;
    public AudioClip screamSound;

    private Transform player;
    private Animator anim;
    private Rigidbody rb;
    private HealthEnemy health;

    private enum EnemyState { Idle, Patrol, Chase, Attack, Rage, Hit }
    private EnemyState currentState = EnemyState.Idle;

    private bool isFacingRight = true;
    private float targetRotationY = 90f;
    private bool canAttack = true;
    private int comboStep = 1;
    private bool isPerformingAction = false;
    private float originalAnimationSpeed;
    private Coroutine stateMachineCoroutine;
    private bool hasScreamed = false;
    private float originalRunSpeed;
    private float originalAttackCooldown;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<HealthEnemy>();

        if (health == null)
        {
            Debug.LogError("HealthEnemy компонент не найден на " + gameObject.name);
            return;
        }

        originalRunSpeed = runSpeed;
        originalAttackCooldown = attackCooldown;

        if (anim != null)
            originalAnimationSpeed = anim.speed;

        targetRotationY = transform.eulerAngles.y;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        health.OnDeath += HandleDeath;

        if (!health.IsDead() && stateMachineCoroutine == null)
        {
            stateMachineCoroutine = StartCoroutine(StateMachineRoutine());
        }
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        Debug.Log("EnemyAI3D: HandleDeath вызван!");

        if (stateMachineCoroutine != null)
        {
            StopCoroutine(stateMachineCoroutine);
            stateMachineCoroutine = null;
        }

        StopAllCoroutines();
        isPerformingAction = true;
        canAttack = false;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (anim != null)
        {
            anim.speed = originalAnimationSpeed;
            ResetAllVelocityAnims();
        }

        Destroy(gameObject, 3f);
    }

    void Update()
    {
        if (health == null || health.IsDead() || isPerformingAction) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, targetRotationY, 0), Time.deltaTime * flipSpeed);

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (currentState != EnemyState.Attack && currentState != EnemyState.Hit)
        {
            if (distanceToPlayer <= attackRange)
            {
                SwitchState(EnemyState.Attack);
            }
            else if (distanceToPlayer <= chaseRange)
            {
                if (!hasScreamed && screamSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(screamSound);
                    hasScreamed = true;
                }
                SwitchState(EnemyState.Chase);
            }
            else if (currentState == EnemyState.Chase)
            {
                SwitchState(EnemyState.Idle);
            }
        }
    }

    void FixedUpdate()
    {
        if (health == null || health.IsDead() || isPerformingAction)
        {
            if (rb != null)
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        if (currentState == EnemyState.Chase && player != null)
        {
            MoveTowardsPlayer();
        }

        if (currentState == EnemyState.Patrol && !isPerformingAction && player == null)
        {
            float moveX = isFacingRight ? walkSpeed : -walkSpeed;
            rb.velocity = new Vector3(moveX, rb.velocity.y, 0);
        }
    }

    void MoveTowardsPlayer()
    {
        if (player == null) return;

        float direction = player.position.x - transform.position.x;

        if (direction > 0 && !isFacingRight) Flip();
        else if (direction < 0 && isFacingRight) Flip();

        float moveX = direction > 0 ? runSpeed : -runSpeed;
        rb.velocity = new Vector3(moveX, rb.velocity.y, 0);

        if (anim != null)
        {
            anim.SetBool("run", true);
            anim.SetBool("walk", false);
        }
    }

    IEnumerator StateMachineRoutine()
    {
        while (!health.IsDead())
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    yield return StartCoroutine(IdleRoutine());
                    break;
                case EnemyState.Patrol:
                    yield return StartCoroutine(PatrolRoutine());
                    break;
                case EnemyState.Chase:
                    yield return null;
                    break;
                case EnemyState.Attack:
                    yield return StartCoroutine(AttackRoutine());
                    break;
                case EnemyState.Hit:
                    yield return StartCoroutine(HitRoutine());
                    break;
            }
            yield return null;
        }
    }

    IEnumerator IdleRoutine()
    {
        if (anim == null) yield break;

        string idleAnim = Random.Range(0, 2) == 0 ? "idle1" : "idle2";
        anim.SetBool(idleAnim, true);
        anim.SetBool("walk", false);
        anim.SetBool("run", false);

        float idleTime = Random.Range(2f, 4f);
        float timer = 0;

        while (timer < idleTime && currentState == EnemyState.Idle && !isPerformingAction)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == EnemyState.Idle && !isPerformingAction)
        {
            SwitchState(EnemyState.Patrol);
        }
    }

    IEnumerator PatrolRoutine()
    {
        if (anim == null) yield break;

        anim.SetBool("walk", true);
        anim.SetBool("run", false);

        float patrolTime = Random.Range(3f, 5f);
        float timer = 0;

        while (timer < patrolTime && currentState == EnemyState.Patrol && !isPerformingAction)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == EnemyState.Patrol && !isPerformingAction)
        {
            Flip();
            SwitchState(EnemyState.Idle);
        }
    }

    IEnumerator AttackRoutine()
    {
        if (anim == null) yield break;

        isPerformingAction = true;
        canAttack = false;

        rb.velocity = new Vector3(0, rb.velocity.y, 0);

        anim.SetBool("walk", false);
        anim.SetBool("run", false);

        float previousSpeed = anim.speed;
        anim.speed = attackAnimationSpeed;

        string attackTrigger = "atack" + comboStep;
        anim.SetTrigger(attackTrigger);

        comboStep++;
        if (comboStep > 2) comboStep = 1;

        float attackAnimLength = 0.9f / attackAnimationSpeed;
        yield return new WaitForSeconds(attackAnimLength);

        if (!health.IsDead() && anim != null)
            anim.speed = previousSpeed;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
        isPerformingAction = false;

        if (!health.IsDead())
        {
            SwitchState(EnemyState.Idle);
        }
    }

    IEnumerator HitRoutine()
    {
        if (anim == null) yield break;

        isPerformingAction = true;

        rb.velocity = new Vector3(0, rb.velocity.y, 0);
        anim.SetTrigger("gethit");

        yield return new WaitForSeconds(0.5f);

        if (!health.IsDead() && health.GetHealthPercent() < 0.4f)
        {
            anim.SetTrigger("rage");
            runSpeed *= 1.3f;
            attackCooldown *= 0.7f;
            yield return new WaitForSeconds(1f);
        }

        isPerformingAction = false;

        if (!health.IsDead())
        {
            canAttack = true;
            SwitchState(EnemyState.Idle);
            if (stateMachineCoroutine == null)
                stateMachineCoroutine = StartCoroutine(StateMachineRoutine());
        }
    }

    void SwitchState(EnemyState newState)
    {
        if (health == null || health.IsDead()) return;
        if (currentState == newState) return;
        if (isPerformingAction && newState != EnemyState.Hit) return;

        if (anim != null && currentState != EnemyState.Attack && currentState != EnemyState.Hit)
        {
            ResetAllVelocityAnims();
        }

        currentState = newState;
    }

    public void EnemyPerformAttackDamage()
    {
        if (health == null || health.IsDead() || attackPoint == null) return;

        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);

        foreach (Collider p in hitPlayers)
        {
            if (p != null)
            {
                PlayerHealth playerHp = p.GetComponent<PlayerHealth>();
                if (playerHp != null)
                {
                    playerHp.TakeDamage(attackDamage);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (health != null && !health.IsDead())
        {
            health.TakeDamage(damage);
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        targetRotationY = isFacingRight ? 90f : 270f;
    }

    void ResetAllVelocityAnims()
    {
        if (anim == null) return;

        anim.SetBool("walk", false);
        anim.SetBool("run", false);
        anim.SetBool("idle1", false);
        anim.SetBool("idle2", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}