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
    [SerializeField] private float minFallDistance = 1.5f; // Минимальная высота для обычной анимации падения
    [SerializeField] private float longFallDistance = 7f; // !!! ПРЕДЕЛ ВЫСОТЫ, указываемый вручную !!!

    [Header("Поворот")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Анимации")]
    [SerializeField] private Animator animator;
    

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
    [SerializeField] private GameObject Canvas;

    [Header("Рывок и Бег")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField] private float sprintThreshold = 0.2f; // Время зажатия для перехода в бег
    [SerializeField] private Color sprintColor = Color.blue;
    [SerializeField] private Renderer characterRenderer; // Ссылка на MeshRenderer модели
    [SerializeField] private Transform characterModel; // Перетащите сюда объект с моделью (меш)
    [SerializeField] private float shakeIntensity = 0.2f; // Сила тряски
    [SerializeField] private float visualDashDuration = 0.5f; // Сколько времени персонаж будет синим и будет трястись

    [Header("Настройки силы рывка")]
    [SerializeField] private float dashForceHorizontal = 15f; // Сила для WASD
    [SerializeField] private float dashForceVertical = 20f;   // Сила для прыжка вверх

    [Header("Предсказание приземления")]
    [SerializeField] private float landingCheckDistance = 2.0f; // Расстояние до земли для срабатывания анимации приземления

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
    private Vector3 staticModelPos; // Настоящий "центр" модели

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
        Canvas.SetActive(true);
        if (characterModel != null)
        {
            staticModelPos = characterModel.localPosition;
        }
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

        wasFalling = false;
        fallStartHeight = transform.position.y;
        currentFallDistance = 0f;
    }

    private void Update()
    {
        HandleCrouch();
        HandleSprintAndDash();

        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        UpdateAimLayerWeight();

        if (CanMove && !isMovementLocked && !IsAiming && !isDashing)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        else if (!isDashing)
        {
            horizontalInput = 0f;
        }

        // Таймеры (Time.deltaTime здесь уместен, так как это просто отсчет времени)
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
            if (footstepScript != null)
                footstepScript.PlayJumpOrLandSound();
        }

        if (canJump && jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            ApplyJump();
        }

        if (Input.GetMouseButtonDown(1))
        {
            animator.SetTrigger("shoot");
        }

        if (Time.time >= nextAttackTime)
        {
            if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                Attack();
            }
        }

        HandleRotation();
        UpdateAnimations();
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
        // Блокируем повторный запуск, чтобы не сбить позицию
        if (isDashing) yield break;

        isDashing = true;

        // Эффекты
        if (charMaterial) charMaterial.color = sprintColor;
        audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
        audioSource.PlayOneShot(DashSound, volume);

        // Физика
        float vInput = Input.GetAxisRaw("Vertical");
        float hInput = Input.GetAxisRaw("Horizontal");
        Vector3 dashDirection = new Vector3(hInput, vInput, 0f).normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = facingRight ? Vector3.right : Vector3.left;

        rb.velocity = Vector3.zero;
        rb.velocity = new Vector3(dashDirection.x * dashForceHorizontal, dashDirection.y * dashForceVertical, 0f);
        rb.useGravity = false;

        float elapsed = 0f;
        bool physicsEnded = false;

        while (elapsed < visualDashDuration)
        {
            // Тряска относительно ГЛОБАЛЬНО зафиксированного центра
            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            characterModel.localPosition = staticModelPos + new Vector3(offsetX, offsetY, 0);

            if (!physicsEnded && elapsed >= dashDuration)
            {
                rb.useGravity = true;
                isDashing = false;
                physicsEnded = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ЖЕСТКИЙ ВОЗВРАТ в исходную точку
        characterModel.localPosition = staticModelPos;

        if (charMaterial) charMaterial.color = originalColor;

        // Страховка возврата управления
        rb.useGravity = true;
        isDashing = false;
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
            float currentMaxSpeed = moveSpeed;
            if (isSprinting) currentMaxSpeed *= sprintSpeedMultiplier;
            if (isCrouching) currentMaxSpeed *= crouchSpeedMultiplier;

            float targetSpeed = horizontalInput * currentMaxSpeed;

            // ПРОВЕРКА СТЕНЫ
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                bool hittingWall =
                    Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer) ||
                    Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer) ||
                    Physics.Raycast(transform.position + Vector3.up * 1.8f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer);

                if (hittingWall) targetSpeed = 0;
            }

            // ИСПРАВЛЕНИЕ: Для VelocityChange НЕ нужно умножать на Time.fixedDeltaTime
            // Мы вычисляем разницу скоростей и применяем её сразу.
            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            // Чтобы ускорение было плавным, мы ограничиваем изменение скорости
            float movement = speedDiff * accelRate * Time.fixedDeltaTime;

            rb.AddForce(movement * Vector3.right, ForceMode.VelocityChange);
        }

        // Фиксация Z
        rb.position = new Vector3(rb.position.x, rb.position.y, lockedZ);
    }

    private void ApplyVariableJumpHeight()
    {
        // ИСПРАВЛЕНИЕ: Для физических манипуляций в FixedUpdate
        if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            // Умножаем на fixedDeltaTime, так как это постепенное накопление силы (как гравитация)
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }


    private void UpdateAimLayerWeight()
    {
        if (topLayerIndex == -1) return;

        float targetWeight = IsAiming ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(topLayerIndex);
        // Используем 1 - exp для независимости Lerp от FPS
        float newWeight = Mathf.Lerp(currentWeight, targetWeight, 1.0f - Mathf.Exp(-10f * Time.deltaTime));
        animator.SetLayerWeight(topLayerIndex, newWeight);
    }

    private void CheckFallingState()
    {
        // --- БЛОК ПРИЗЕМЛЕНИЯ (ФАКТИЧЕСКОЕ КАСАНИЕ) ---
        if (isGrounded)
        {
            if (wasFalling || animator.GetBool("IsFalling") || animator.GetBool("IsLongFall"))
            {
                wasFalling = false;
                currentFallDistance = 0f;
                animator.SetBool("IsFalling", false);
                animator.SetBool("IsLongFall", false);
                animator.SetBool("IsJumping", false);
            }
            return;
        }

        // --- БЛОК ПАДЕНИЯ (В ВОЗДУХЕ) ---
        if (rb.velocity.y < -0.1f)
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
                }

                // Логика высокого падения
                if (currentFallDistance >= longFallDistance && !animator.GetBool("IsLongFall"))
                {
                    animator.SetBool("IsLongFall", true);
                    animator.SetBool("IsJumping", false);
                }

                // --- НОВАЯ ЛОГИКА: ТРИГГЕР ПЕРЕД ЗЕМЛЕЙ ---
                if (animator.GetBool("IsLongFall"))
                {
                    // Пускаем луч вниз от позиции groundCheck
                    RaycastHit hit;
                    if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, landingCheckDistance, groundLayer))
                    {
                        // Если земля близко, активируем триггер приземления
                        animator.SetTrigger("NearGround");
                    }
                }
            }
        }

        if (!isGrounded && !wasFalling && rb.velocity.y < 0)
        {
            wasFalling = true;
            fallStartHeight = transform.position.y;
        }

        if (wasFalling && rb.velocity.y < -0.1f)
        {
            currentFallDistance = fallStartHeight - transform.position.y;

            // Обычное падение
            if (currentFallDistance >= minFallDistance)
            {
                animator.SetBool("IsFalling", true);
            }

            // Высокое падение
            if (currentFallDistance >= longFallDistance)
            {
                animator.SetBool("IsLongFall", true);
                animator.SetBool("IsJumping", false);
            }

            // Предсказание земли для триггера приземления
            if (animator.GetBool("IsLongFall"))
            {
                RaycastHit hit;
                if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, landingCheckDistance, groundLayer))
                {
                    animator.SetTrigger("NearGround");
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
        animator.SetBool("IsLongFall", false); 
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

        float smoothness = 1.0f - Mathf.Exp(-rotationSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            smoothness
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