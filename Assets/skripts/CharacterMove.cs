using UnityEngine;
using System.Collections;

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
    [SerializeField] private float minFallDistance = 1.5f;

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

    [Header("Доступные способности")]
    [SerializeField] private bool canJump = false;

    [Header("Ссылки")]
    [SerializeField] private CharacterFootsteps footstepScript;

    // Публичные свойства для других скриптов
    public bool IsAiming { get; private set; }
    public bool CanMove { get; private set; } = true;

    // Приватные переменные
    private Rigidbody rb;
    private float horizontalInput;
    private bool facingRight = true;
    private Quaternion targetRotation;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private float lastJumpTime;
    private bool wasFalling;
    private float fallStartHeight;
    private float currentFallDistance;
    private float lockedZ;
    private bool isMovementLocked = false;
    private int topLayerIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        targetRotation = Quaternion.Euler(0f, 90f, 0f);
        transform.rotation = targetRotation;
        lockedZ = transform.position.z;

        topLayerIndex = animator.GetLayerIndex("TopLayer");
    }

    private void Update()
    {
        // Проверка земли
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Логика прицеливания
        IsAiming = Input.GetMouseButton(1);
        animator.SetBool("isAiming", IsAiming);

        // Обновление веса слоя анимации
        UpdateAimLayerWeight();

        // Ввод движения (блокируется при прицеливании или блокировке)
        if (CanMove && !isMovementLocked && !IsAiming)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        else
        {
            horizontalInput = 0f;
        }

        // Таймеры прыжка
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Звук приземления
        if (isGrounded && !wasGrounded && Time.time - lastJumpTime > 0.2f)
        {
            if (footstepScript != null)
                footstepScript.PlayJumpOrLandSound();
        }

        // Логика прыжка
        if (canJump && jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            ApplyJump();
        }

        // Атака
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                Attack();
            }
        }

        // Поворот персонажа (только когда не прицеливаемся)
        if (!IsAiming)
        {
            HandleRotation();
        }

        // Анимации
        UpdateAnimations();

        // Отслеживание падения
        CheckFallingState();
    }

    private void FixedUpdate()
    {
        // Движение (блокируется при прицеливании или блокировке)
        if (!isMovementLocked && !IsAiming && CanMove)
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDiff * accelRate * Time.fixedDeltaTime;

            rb.AddForce(movement * Vector3.right, ForceMode.VelocityChange);
        }
    }

    private void UpdateAimLayerWeight()
    {
        if (topLayerIndex == -1) return;

        float targetWeight = IsAiming ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(topLayerIndex);
        float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * 5f);
        animator.SetLayerWeight(topLayerIndex, newWeight);
    }

    private void CheckFallingState()
    {
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

            if (audioSource != null && kickSound != null)
            {
                audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
                audioSource.PlayOneShot(kickSound, volume);
            }
        }
    }

    private void Attack()
    {
        StartCoroutine(MovementLockCoroutine(0.5f));
        rb.velocity = new Vector3(0, rb.velocity.y, 0);

        comboStep = (comboStep == 1) ? 2 : 1;
        animator.SetInteger("AttackType", comboStep);
        animator.SetTrigger("Attack");

        nextAttackTime = Time.time + attackRate;
    }

    private void ApplyJump()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);

        wasFalling = false;
        currentFallDistance = 0f;
        animator.SetBool("IsFalling", false);
        animator.SetBool("IsJumping", true);

        if (footstepScript != null)
            footstepScript.PlayTakeoffSound();

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        lastJumpTime = Time.time;
    }

    private void UpdateAnimations()
    {
        float animSpeed = isGrounded ? Mathf.Abs(horizontalInput) : 0f;
        animator.SetFloat("Speed", animSpeed);

        if (!isGrounded && rb.velocity.y > 0.5f)
        {
            animator.SetBool("IsJumping", true);
        }

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

    private IEnumerator MovementLockCoroutine(float duration)
    {
        isMovementLocked = true;
        yield return new WaitForSeconds(duration);
        isMovementLocked = false;
    }

    // Публичный метод для временной блокировки движения из других скриптов
    public void LockMovement(float duration)
    {
        StartCoroutine(MovementLockCoroutine(duration));
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