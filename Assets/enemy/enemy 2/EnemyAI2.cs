using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI3D : MonoBehaviour
{
    [Header("Движение и Обнаружение")]
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public float chaseRange = 7f;
    public float attackRange = 1.5f;
    public float flipSpeed = 12f;

    [Header("Параметры Боя")]
    public int maxHealth = 100;
    private int currentHealth;
    public float attackCooldown = 1.5f;
    public int attackDamage = 15;
    public float attackAnimationSpeed = 1.5f;
    public Transform attackPoint;
    public float attackRadius = 1f;
    public LayerMask playerLayer;

    private Transform player;
    private Animator anim;
    private Rigidbody rb;

    private enum EnemyState { Idle, Patrol, Chase, Attack, Rage, Hit, Dead }
    private EnemyState currentState = EnemyState.Idle;

    private bool isFacingRight = true;
    private float targetRotationY = 90f;
    private bool canAttack = true;
    private bool isDead = false;
    private int comboStep = 1;
    private bool isPerformingAction = false;
    private float originalAnimationSpeed;
    private Coroutine stateMachineCoroutine;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        if (anim != null)
            originalAnimationSpeed = anim.speed;

        targetRotationY = transform.eulerAngles.y;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Запускаем StateMachine только если враг жив
        if (!isDead && stateMachineCoroutine == null)
        {
            stateMachineCoroutine = StartCoroutine(StateMachineRoutine());
        }
    }

    void Update()
    {
        if (isDead || isPerformingAction) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, targetRotationY, 0), Time.deltaTime * flipSpeed);

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (currentState != EnemyState.Attack && currentState != EnemyState.Hit && currentState != EnemyState.Dead)
        {
            if (distanceToPlayer <= attackRange)
            {
                SwitchState(EnemyState.Attack);
            }
            else if (distanceToPlayer <= chaseRange)
            {
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
        if (isDead || isPerformingAction || currentState == EnemyState.Dead)
        {
            if (rb != null)
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        if (currentState == EnemyState.Chase && player != null)
        {
            MoveTowardsPlayer();
        }

        // Patrol движение через FixedUpdate для плавности
        if (currentState == EnemyState.Patrol && !isPerformingAction && !isDead && player == null)
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
        while (!isDead && currentState != EnemyState.Dead)
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
                case EnemyState.Dead:
                    yield return null;
                    break;
                default:
                    yield return null;
                    break;
            }
            yield return null;
        }
    }

    IEnumerator IdleRoutine()
    {
        if (isDead || anim == null) yield break;

        string idleAnim = Random.Range(0, 2) == 0 ? "idle1" : "idle2";
        anim.SetBool(idleAnim, true);
        anim.SetBool("walk", false);
        anim.SetBool("run", false);

        float idleTime = Random.Range(2f, 4f);
        float timer = 0;

        while (timer < idleTime && currentState == EnemyState.Idle && !isPerformingAction && !isDead)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == EnemyState.Idle && !isPerformingAction && !isDead)
        {
            SwitchState(EnemyState.Patrol);
        }
    }

    IEnumerator PatrolRoutine()
    {
        if (isDead || anim == null) yield break;

        anim.SetBool("walk", true);
        anim.SetBool("run", false);

        float patrolTime = Random.Range(3f, 5f);
        float timer = 0;

        while (timer < patrolTime && currentState == EnemyState.Patrol && !isPerformingAction && !isDead)
        {
            // Движение теперь в FixedUpdate
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == EnemyState.Patrol && !isPerformingAction && !isDead)
        {
            Flip();
            SwitchState(EnemyState.Idle);
        }
    }

    IEnumerator AttackRoutine()
    {
        if (isDead || anim == null) yield break;

        isPerformingAction = true;
        canAttack = false;

        rb.velocity = new Vector3(0, rb.velocity.y, 0);
        anim.SetBool("walk", false);
        anim.SetBool("run", false);

        // Ускоряем анимацию атаки
        float previousSpeed = anim.speed;
        anim.speed = attackAnimationSpeed;

        string attackTrigger = "atack" + comboStep;
        anim.SetTrigger(attackTrigger);

        comboStep++;
        if (comboStep > 2) comboStep = 1;

        float attackAnimLength = 0.9f / attackAnimationSpeed;
        yield return new WaitForSeconds(attackAnimLength);

        // Восстанавливаем скорость
        if (!isDead && anim != null)
            anim.speed = previousSpeed;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
        isPerformingAction = false;

        if (!isDead && currentState != EnemyState.Dead)
        {
            SwitchState(EnemyState.Idle);
        }
    }

    IEnumerator HitRoutine()
    {
        if (isDead || anim == null) yield break;

        isPerformingAction = true;
        rb.velocity = new Vector3(0, rb.velocity.y, 0);

        anim.SetTrigger("gethit");

        yield return new WaitForSeconds(0.5f);

        // Проверяем условие для ярости
        if (currentHealth < maxHealth * 0.4f && !isDead && anim != null)
        {
            anim.SetTrigger("rage");
            runSpeed *= 1.3f;
            attackCooldown *= 0.7f;
            yield return new WaitForSeconds(1f);
        }

        isPerformingAction = false;

        if (!isDead && currentState != EnemyState.Dead)
        {
            canAttack = true;
            SwitchState(EnemyState.Idle);
        }
    }

    void SwitchState(EnemyState newState)
    {
        if (isDead || currentState == EnemyState.Dead)
            return;

        if (currentState == newState)
            return;

        // Нельзя переключаться во время действия
        if (isPerformingAction && newState != EnemyState.Hit && newState != EnemyState.Dead)
            return;

        // Очищаем анимации при смене состояния
        if (currentState != EnemyState.Attack && currentState != EnemyState.Hit && anim != null)
        {
            ResetAllVelocityAnims();
        }

        currentState = newState;

        // Если перешли в состояние Dead - вызываем смерть
        if (currentState == EnemyState.Dead)
        {
            Die();
        }
    }

    public void EnemyPerformAttackDamage()
    {
        if (isDead || attackPoint == null) return;

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
        if (isDead) return;

        // Нельзя получить урон во время смерти
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;

        // Останавливаем все текущие действия
        if (stateMachineCoroutine != null)
        {
            StopCoroutine(stateMachineCoroutine);
            stateMachineCoroutine = null;
        }

        isPerformingAction = false;

        if (anim != null)
        {
            ResetAllVelocityAnims();
            // Восстанавливаем скорость анимации если она была изменена
            anim.speed = originalAnimationSpeed;
        }

        if (rb != null)
            rb.velocity = new Vector3(0, rb.velocity.y, 0);

        if (currentHealth <= 0)
        {
            // Переключаемся в состояние смерти
            SwitchState(EnemyState.Dead);
        }
        else
        {
            currentState = EnemyState.Hit;
            // Запускаем HitRoutine напрямую, не через StateMachine
            StartCoroutine(HitAndRestoreRoutine());
        }
    }

    IEnumerator HitAndRestoreRoutine()
    {
        yield return StartCoroutine(HitRoutine());

        // Перезапускаем StateMachine если враг еще жив
        if (!isDead && currentState != EnemyState.Dead && stateMachineCoroutine == null)
        {
            stateMachineCoroutine = StartCoroutine(StateMachineRoutine());
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = EnemyState.Dead;
        isPerformingAction = true; // Блокируем любые действия

        // Останавливаем все корутины
        if (stateMachineCoroutine != null)
        {
            StopCoroutine(stateMachineCoroutine);
            stateMachineCoroutine = null;
        }

        StopAllCoroutines();

        // Останавливаем движение
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Отключаем коллайдер
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Восстанавливаем скорость анимации
        if (anim != null)
        {
            anim.speed = originalAnimationSpeed;
            // Сбрасываем все анимации и запускаем смерть
            ResetAllVelocityAnims();
            anim.SetTrigger("death");
        }

        // Уничтожаем объект через 3 секунды
        Destroy(gameObject, 3f);
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