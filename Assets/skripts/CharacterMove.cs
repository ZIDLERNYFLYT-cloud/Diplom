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
    [SerializeField] private float lowJumpMultiplier = 3f; // Насколько быстро мы будем падать при коротком нажатииы

    [Header("Поворот")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Анимации")]
    [SerializeField] private Animator animator;
    [SerializeField] private float minFallDistance = 1.5f;

    [Header("Проверка земли")]
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;

    [SerializeField] private float wallCheckDistance = 0.1f;

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
    [SerializeField] private AudioClip DashSound;
    

    [Header("Доступные способности")]
    [SerializeField] private bool canJump = false;

    [Header("Ссылки")]
    [SerializeField] private CharacterFootsteps footstepScript;

    [Header("Рывок и Бег")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField] private float sprintThreshold = 0.2f; // Время зажатия для перехода в бег
    [SerializeField] private Color sprintColor = Color.blue;
    [SerializeField] private Renderer characterRenderer; // Ссылка на MeshRenderer модели

    [Header("Настройки силы рывка")]
    [SerializeField] private float dashForceHorizontal = 15f; // Сила для WASD
    [SerializeField] private float dashForceVertical = 20f;   // Сила для прыжка вверх

    [Header("Приседание")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchColliderHeight = 0.7f; // Высота коллайдера при приседе
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    [SerializeField]  public CapsuleCollider playerCollider;
    private bool isCrouching = false;

    private bool isDashing = false;
    private bool isSprinting = false;
    private float shiftPressTime = 0f;
    private Color originalColor;
    private Material charMaterial;


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
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>(); // Берем коллайдер

        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }

        if (animator == null) animator = GetComponent<Animator>();

        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        // Работа с цветом
        if (characterRenderer != null)
        {
            charMaterial = characterRenderer.material;
            originalColor = charMaterial.color;
        }

        targetRotation = Quaternion.Euler(0f, 90f, 0f);
        transform.rotation = targetRotation;
        lockedZ = transform.position.z;
        topLayerIndex = animator.GetLayerIndex("TopLayer");
    }

    private void Update()
    {
        HandleCrouch();

        HandleSprintAndDash();

        // Проверка земли
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Логика прицеливания
        //IsAiming = Input.GetMouseButton(1);
        //animator.SetBool("isAiming", IsAiming);

        // Обновление веса слоя анимации
        UpdateAimLayerWeight();

        // Ввод движения (блокируется при прицеливании или блокировке)
        if (CanMove && !isMovementLocked && !IsAiming && !isDashing)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        else if (isDashing) { /* не меняем horizontalInput */ }
        else { horizontalInput = 0f; }

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

        // В Update() вашего PlayerSideController:
        if (Input.GetMouseButtonDown(1)) // Например, выстрел на ПКМ
        {
            animator.SetTrigger("shoot");
            // Больше ничего делать не нужно — анимация сама вызовет ShootEvent
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
        //if (!IsAiming)
        //{
            HandleRotation();
        //}

        // Анимации
        UpdateAnimations();

        // Отслеживание падения
        CheckFallingState();
    }

    private void HandleCrouch()
    {
        // Приседаем только если на земле
        if (isGrounded)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (!isCrouching)
                {
                    isCrouching = true;
                    UpdateCollider();
                }
            }
            else // Клавиша Ctrl ОТПУЩЕНА
            {
                if (isCrouching)
                {
                    // Проверка: нет ли над головой потолка?
                    // Пускаем луч вверх от центра персонажа. 
                    // Расстояние луча должно быть равно разнице высот + небольшой запас.
                    float checkDistance = originalColliderHeight - crouchColliderHeight + 0.2f;
                    bool headBlocked = Physics.Raycast(transform.position + Vector3.up * crouchColliderHeight, Vector3.up, checkDistance, groundLayer);

                    if (!headBlocked)
                    {
                        isCrouching = false;
                        UpdateCollider();
                    }
                    else
                    {
                        // Если потолок мешает, оставляем isCrouching = true
                        // Персонаж встанет автоматически, как только выйдет из-под препятствия
                        Debug.Log("Потолок мешает встать!");
                    }
                }
            }
        }
        else if (isCrouching) // Если в воздухе - встаем автоматически
        {
            isCrouching = false;
            UpdateCollider();
        }
    }

    private void UpdateCollider()
    {
        // Если по какой-то причине ссылка пропала, пробуем найти снова
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();

        if (playerCollider == null)
        {
            Debug.LogError("CapsuleCollider не найден на персонаже!");
            return;
        }

        if (isCrouching)
        {
            playerCollider.height = crouchColliderHeight;

            // Математически точное выравнивание по низу
            float offset = (originalColliderHeight - crouchColliderHeight) / 2f;
            playerCollider.center = new Vector3(originalColliderCenter.x, originalColliderCenter.y - offset, originalColliderCenter.z);

            Debug.Log("Коллайдер УМЕНЬШЕН. Текущая высота: " + playerCollider.height);
        }
        else
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
            isCrouching = false;
            Debug.Log("Коллайдер ВОССТАНОВЛЕН. Текущая высота: " + playerCollider.height);
        }
    }
    private void HandleSprintAndDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftPressTime = Time.time;
            // Сразу запускаем рывок при нажатии
            if (!isDashing)
            {
                StartCoroutine(DashRoutine());
            }
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            // Если удерживаем дольше порога — переходим в бег
            if (Time.time - shiftPressTime > sprintThreshold && !isSprinting)
            {
                StartSprint();
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            StopSprint();
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        animator.SetTrigger("Dash");

        audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
        audioSource.PlayOneShot(DashSound, volume);

        if (charMaterial) charMaterial.color = sprintColor;

        float vInput = Input.GetAxisRaw("Vertical");
        float hInput = Input.GetAxisRaw("Horizontal");

        // Считаем направление
        Vector3 dashDirection = new Vector3(hInput, vInput, 0f).normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = facingRight ? Vector3.right : Vector3.left;

        // ОБНУЛЯЕМ скорость перед рывком, чтобы результат всегда был предсказуемым
        rb.velocity = Vector3.zero;

        // Применяем разную силу для разных осей
        float finalDashForceX = dashDirection.x * dashForceHorizontal;
        float finalDashForceY = dashDirection.y * dashForceVertical;

        rb.velocity = new Vector3(finalDashForceX, finalDashForceY, 0f);

        // Временно отключаем гравитацию, чтобы персонаж летел ровно по вектору
        rb.useGravity = false;

        yield return new WaitForSeconds(dashDuration);

        // Возвращаем гравитацию и сбрасываем состояние
        rb.useGravity = true;
        isDashing = false;

        
            charMaterial.color = originalColor;
    }

    private void StartSprint()
    {
        isSprinting = true;
        // Если мы уже бежим, убеждаемся, что цвет синий
        if (charMaterial) charMaterial.color = originalColor;
        animator.SetBool("IsSprinting", true);
    }

    private void StopSprint()
    {
        isSprinting = false;
        // Возвращаем цвет, только если не идет процесс рывка
        if (!isDashing && charMaterial)
        {
            charMaterial.color = originalColor;
        }
        animator.SetBool("IsSprinting", false);
    }

    private void FixedUpdate()
    {
        ApplyVariableJumpHeight();

        if (!isMovementLocked && !IsAiming && CanMove && !isDashing)
        {
            // Считаем скорость с учетом приседа
            float currentMaxSpeed = moveSpeed;
            if (isSprinting) currentMaxSpeed *= sprintSpeedMultiplier;
            if (isCrouching) currentMaxSpeed *= crouchSpeedMultiplier; // Замедляем на корточках

            float targetSpeed = horizontalInput * currentMaxSpeed;

            // ... (ваша существующая логика проверки стен и AddForce) ...
            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDiff * accelRate * Time.fixedDeltaTime;
            rb.AddForce(movement * Vector3.right, ForceMode.VelocityChange);
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, lockedZ);

        if (!isMovementLocked && !IsAiming && CanMove && !isDashing)
        {
            // 1. Считаем целевую скорость
            float currentMaxSpeed = isSprinting ? moveSpeed * sprintSpeedMultiplier : moveSpeed;
            float targetSpeed = horizontalInput * currentMaxSpeed;

            // 2. ПРОВЕРКА СТЕНЫ (Raycast)
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                // Пускаем 3 луча (у колен, у пояса, у головы), чтобы точно поймать стену
                bool hittingWall =
                    Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer) ||
                    Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer) ||
                    Physics.Raycast(transform.position + Vector3.up * 1.8f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer);

                if (hittingWall)
                {
                    // Если стена впереди, обнуляем целевую скорость только в сторону стены
                    targetSpeed = 0;

                    // Дополнительно: мягко обнуляем текущую горизонтальную скорость Rigidbody
                    rb.velocity = new Vector3(0, rb.velocity.y, 0);
                }
            }

            // 3. Прикладываем силу
            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDiff * accelRate * Time.fixedDeltaTime;

            rb.AddForce(movement * Vector3.right, ForceMode.VelocityChange);
        }

        // Принудительная фиксация Z (чтобы не вылетал из плоскости при ударах)
        transform.position = new Vector3(transform.position.x, transform.position.y, lockedZ);
    }

    private void ApplyVariableJumpHeight()
    {
        // Если мы летим ВВЕРХ, но при этом НЕ держим кнопку прыжка
        if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            // Применяем дополнительную силу тяжести, чтобы прыжок был коротким
            // Physics.gravity.y * (lowJumpMultiplier - 1) — это добавочная гравитация
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
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
        animator.SetBool("IsCrouching", isCrouching);

        float moveMagnitude = Mathf.Abs(horizontalInput);

        // Обычная скорость для ходьбы/бега
        float animSpeed = isGrounded ? moveMagnitude : 0f;
        float speedMultiplier = isSprinting ? 2f : 1f;
        animator.SetBool("IsCrouching", isGrounded && isCrouching);
        animator.SetFloat("Speed", moveMagnitude * (isSprinting ? 2f : 1f));

        animator.SetFloat("Speed", animSpeed * speedMultiplier);

        // Логика корточек
        animator.SetBool("IsCrouching", isCrouching);

        // Логика для корточек
        if (isCrouching)
        {
            // Если стоим - скорость анимации 0 (пауза на 1 кадре)
            // Если идем - скорость 1 (анимация играет)
            float crouchAnimSpeed = (moveMagnitude > 0.01f) ? 1.0f : 0.0f;
            animator.SetFloat("CrouchSpeed", crouchAnimSpeed);
        }

        // Если бежим, увеличиваем значение Speed для перехода в анимацию бега (или используем IsSprinting)


        //float animSpeed = isGrounded ? Mathf.Abs(horizontalInput) : 0f;
        animator.SetFloat("Speed", animSpeed);

        //float speedMultiplier = isSprinting ? 2f : 1f;
        animator.SetFloat("Speed", animSpeed * speedMultiplier);

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
        // Если прицеливаемся — разворачиваем персонажа корпусом в сторону центра экрана
        if (IsAiming)
        {
            // Находим центр экрана в мире
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 centerPoint = ray.GetPoint(dist);

                // Если центр экрана справа от персонажа — смотрим вправо
                if (centerPoint.x > transform.position.x && !facingRight)
                {
                    facingRight = true;
                    targetRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                // Если слева — смотрим влево
                else if (centerPoint.x < transform.position.x && facingRight)
                {
                    facingRight = false;
                    targetRotation = Quaternion.Euler(0f, 270f, 0f);
                }
            }
        }
        // Если не прицеливаемся — стандартный поворот по кнопкам движения
        else
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