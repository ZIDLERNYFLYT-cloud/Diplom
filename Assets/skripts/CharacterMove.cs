using UnityEngine;

public class PlayerSideController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 30f;

    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Поворот")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Анимации")]
    [SerializeField] private Animator animator;
    [SerializeField] private float minFallDistance = 1.5f; // Минимальное расстояние падения для активации анимации

    [Header("Проверка земли")]
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;

    [Header("Атака")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRate = 0.5f;
    private float nextAttackTime = 0f;
    private int comboStep = 0;

    [Header("Звуки")]
    [SerializeField] private AudioClip kickSound;
    [SerializeField] private AudioSource audioSource;
    [Range(0, 1)][SerializeField] private float volume = 0.5f;
    [SerializeField] private float pitchRange = 0.2f;

    private Rigidbody rb;
    private float horizontalInput;
    private bool facingRight = true;
    private Quaternion targetRotation;
    private bool wasGrounded;

    [Header("Звуки")]
    [SerializeField] private CharacterFootsteps footstepScript;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private float lastJumpTime;

    // Для отслеживания состояния падения
    private bool wasFalling;
    private float fallStartHeight; // Высота на которой началось падение
    private float currentFallDistance; // Текущее пройденное расстояние падения

    [Header("Доступные способности")]
    [SerializeField] private bool canJump = false; // По умолчанию прыжок закрыт
    private float lockedZ; // Переменная для хранения Z

    private void Start()
    {
        lockedZ = transform.position.z;
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        targetRotation = Quaternion.Euler(0f, 90f, 0f);
        transform.rotation = targetRotation;
    }

    private void Update()
    {
        wasGrounded = isGrounded;

        // 1. Проверка земли
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (Time.time >= nextAttackTime)
        {
            if (Input.GetMouseButton(0))
            {
                Attack();
            }
        }

        // 2. Ввод данных
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 3. Таймеры прыжка и койота
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (isGrounded && !wasGrounded && Time.time - lastJumpTime > 0.2f)
        {
            if (footstepScript != null) footstepScript.PlayJumpOrLandSound();
        }

        // 4. Логика прыжка
        
        if (canJump && jumpBufferCounter > 0 && coyoteTimeCounter > 0) // Добавили canJump
        {
            ApplyJump();
        }

        // 5. Поворот персонажа
        HandleRotation();

        // 6. Управление анимациями
        UpdateAnimations();

        // 7. Отслеживание падения для анимации
        CheckFallingState();

        
    }

    private void CheckFallingState()
    {
        // Если на земле — сбрасываем всё
        if (isGrounded)
        {
            if (wasFalling || animator.GetBool("IsFalling"))
            {
                wasFalling = false;
                animator.SetBool("IsFalling", false);
                currentFallDistance = 0f;
            }
            return;
        }

        // Если в воздухе и летим вниз
        if (rb.velocity.y < -0.5f)
        {
            if (!wasFalling)
            {
                wasFalling = true;
                fallStartHeight = transform.position.y;
                currentFallDistance = 0f;
            }
            else
            {
                currentFallDistance = fallStartHeight - transform.position.y;

                // Включаем анимацию только один раз при достижении дистанции
                if (currentFallDistance >= minFallDistance && !animator.GetBool("IsFalling"))
                {
                    animator.SetBool("IsFalling", true);
                    animator.SetBool("IsJumping", false);
                }
            }
        }
    }

    public void Hit()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeDamage(attackDamage);
            }

            audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
            audioSource.PlayOneShot(kickSound, volume);
        }
    }

    private void Attack()
    {
        rb.velocity = new Vector3(0, rb.velocity.y, 0);

        comboStep = (comboStep == 1) ? 2 : 1;

        animator.SetInteger("AttackType", comboStep);
        animator.SetTrigger("Attack");

        nextAttackTime = Time.time + attackRate;
    }

    private void ApplyJump()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);

        // Сбрасываем флаг падения при прыжке вверх
        wasFalling = false;
        currentFallDistance = 0f;
        animator.SetBool("IsFalling", false);
        animator.SetBool("IsJumping", true);

        if (footstepScript != null) footstepScript.PlayTakeoffSound();

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        lastJumpTime = Time.time;
    }

    private void UpdateAnimations()
    {
        // Анимация бега
        float animSpeed = isGrounded ? Mathf.Abs(horizontalInput) : 0f;
        animator.SetFloat("Speed", animSpeed);

        // Логика прыжка (взлет)
        if (!isGrounded && rb.velocity.y > 0.5f)
        {
            animator.SetBool("IsJumping", true);
        }

        // Приземление: сброс прыжка
        if (isGrounded && animator.GetBool("IsJumping"))
        {
            animator.SetBool("IsJumping", false);
        }
    }

    public void UnlockJump()
    {
        canJump = true;
        Debug.Log("Прыжок разблокирован!");
    }

    private void HandleRotation()
    {
        if (horizontalInput > 0.1f && !facingRight)
        {
            facingRight = true;
            targetRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else if (horizontalInput < -0.1f && facingRight)
        {
            facingRight = false;
            targetRotation = Quaternion.Euler(0f, 270f, 0f);
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void FixedUpdate()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDiff * accelRate * Time.fixedDeltaTime;

        rb.AddForce(movement * Vector3.right, ForceMode.VelocityChange);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}