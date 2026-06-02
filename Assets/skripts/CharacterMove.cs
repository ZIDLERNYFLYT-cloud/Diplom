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
    [SerializeField] private float lowJumpMultiplier = 3f;
    [SerializeField] private float minFallDistance = 1.5f;
    [SerializeField] private float longFallDistance = 7f;

    [Header("Прыжок от стены")]
    [SerializeField] private float wallJumpUpForce = 16f;
    [SerializeField] private float wallJumpHorizontalForce = 11f;
    [SerializeField] private float wallJumpUpMultiplier = 1.15f;

    [Header("Вскарабкивание на стену")]
    [SerializeField] private float climbHeight = 2.2f;
    [SerializeField] private float climbForwardOffset = 0.8f;
    [SerializeField] private float climbDuration = 0.6f;
    [SerializeField] private float climbCheckHeight = 2.0f;
    [SerializeField] private float climbCheckDistance = 0.6f;
    [SerializeField, Range(0, 1)] private float hangAnimationFrame = 0.1f;
    [SerializeField] private float chest = 0.25f;
    [SerializeField] private float feet = 0.25f;

    [Header("Поворот")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Анимации")]
    [SerializeField] private Animator animator;

    [Header("Проверка земли и стен")]
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float wallCheckHeight = 1.5f;
    [SerializeField] private float wallGrabCheckDistance = 0.3f;

    [Header("Цепляние за стену")]
    [SerializeField] private float wallSlideSpeed = 1.5f;
    [SerializeField] private PhysicMaterial wallGrabMaterial;
    [SerializeField] private PhysicMaterial defaultMaterial;

    [Header("Атака пинком (ОТКЛЮЧАЕТСЯ ПРИ МЕЧЕ)")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRate = 0.5f;
    private float nextAttackTime = 0f;
    private int comboStep = 0;
    [SerializeField] private float attackAnimationSpeed = 1.5f;

    [Header("Звуки")]
    [SerializeField] private AudioClip kickSound;
    [SerializeField] private AudioSource audioSource;
    [Range(0, 1)][SerializeField] private float volume = 0.5f;
    [SerializeField] private float pitchRange = 0.2f;
    [SerializeField] private AudioClip DashSound;

    [Header("Доступные способности")]
    [SerializeField] private bool canJump = false;
    [SerializeField] private bool canWallGrabAbility = false;

    [Header("Ссылки")]
    [SerializeField] private CharacterFootsteps footstepScript;
    [SerializeField] private GameObject Canvas;

    [Header("Рывок и Бег")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField] private float sprintThreshold = 0.2f;
    [SerializeField] private Color sprintColor = Color.blue;
    [SerializeField] private Renderer characterRenderer;
    [SerializeField] private Transform characterModel;
    [SerializeField] private float shakeIntensity = 0.2f;
    [SerializeField] private float visualDashDuration = 0.5f;

    [Header("Настройки силы рывка")]
    [SerializeField] private float dashForceHorizontal = 15f;
    [SerializeField] private float dashForceVertical = 20f;

    [Header("Предсказание приземления")]
    [SerializeField] private float landingCheckDistance = 2.0f;

    [Header("Приседание")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchColliderHeight = 0.7f;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    [SerializeField] public CapsuleCollider playerCollider;
    private bool isCrouching = false;

    [Header("СИСТЕМА АТАКИ МЕЧОМ")]
    [SerializeField] private PlayerCombat swordCombat;
    [SerializeField] private bool useSwordInsteadOfKick = true;

    private bool isDashing = false;
    private bool isSprinting = false;
    private float shiftPressTime = 0f;
    private Color originalColor;
    private Material charMaterial;
    private Vector3 staticModelPos;

    public bool IsAiming { get; private set; }
    public bool CanMove { get; private set; } = true;

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
    private Vector3 wallNormal;
    private bool isWallGrabbing = false;
    private bool isOnWall = false;
    private bool isClimbing = false;
    private float originalDrag;

    private void Start()
    {
        mainCam = Camera.main;
        Canvas.SetActive(true);
        if (characterModel != null)
        {
            staticModelPos = characterModel.localPosition;
        }
        originalDrag = rb.drag;

        if (swordCombat == null)
        {
            swordCombat = GetComponent<PlayerCombat>();
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;

            if (defaultMaterial != null)
                playerCollider.material = defaultMaterial;
        }

        if (animator == null) animator = GetComponent<Animator>();

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

    void Update()
    {
        if (isClimbing)
        {
            horizontalInput = 0f;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            return;
        }

        HandleCrouch();
        HandleSprintAndDash();

        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        UpdateAimLayerWeight();

        if (CanMove && !isMovementLocked && !IsAiming && !isDashing && !isWallGrabbing)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        else if (!isDashing && !isWallGrabbing)
        {
            horizontalInput = 0f;
        }

        CheckWallGrab();

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (canJump && jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !isWallGrabbing)
        {
            ApplyJump();
        }

        if (Input.GetButtonDown("Jump") && isWallGrabbing && canWallGrabAbility)
        {
            JumpFromWall();
        }

        if (isWallGrabbing && (Input.GetAxisRaw("Horizontal") == 0 || isGrounded))
        {
            ReleaseWall();
        }

        if (Input.GetMouseButtonDown(1))
        {
            animator.SetTrigger("shoot");
        }

        // ИСПРАВЛЕНА АТАКА - убираем конфликт с мечом
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1)) // Используем GetMouseButtonDown вместо GetMouseButton
            {
                if (!useSwordInsteadOfKick || swordCombat == null)
                {
                    KickAttack();
                }
                // Если меч включен, ничего не делаем - меч сам обрабатывает атаку
            }
        }

        HandleRotation();
        UpdateAnimations();
        CheckFallingState();
    }

    private void FixedUpdate()
    {
        if (isClimbing)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        ApplyVariableJumpHeight();

        if (!isMovementLocked && !IsAiming && CanMove && !isDashing && !isWallGrabbing)
        {
            float currentMaxSpeed = moveSpeed;
            if (isSprinting) currentMaxSpeed *= sprintSpeedMultiplier;
            if (isCrouching) currentMaxSpeed *= crouchSpeedMultiplier;

            float targetSpeed = horizontalInput * currentMaxSpeed;

            if (Mathf.Abs(horizontalInput) > 0.01f && !isWallGrabbing)
            {
                bool hittingWall =
                    Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer) ||
                    Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer) ||
                    Physics.Raycast(transform.position + Vector3.up * 1.8f, Vector3.right * horizontalInput, wallCheckDistance, groundLayer);

                if (hittingWall) targetSpeed = 0;
            }

            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDiff * accelRate * Time.fixedDeltaTime;
            rb.AddForce(movement * Vector3.right, ForceMode.VelocityChange);
        }

        if (isWallGrabbing && !isClimbing)
        {
            rb.velocity = new Vector3(0f, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, 2f), 0f);
        }

        rb.position = new Vector3(rb.position.x, rb.position.y, lockedZ);
    }

    private void CheckWallGrab()
    {
        if (!canWallGrabAbility || isGrounded || isDashing || isClimbing)
        {
            if (isWallGrabbing) ReleaseWall();
            return;
        }

        float direction = horizontalInput != 0 ? Mathf.Sign(horizontalInput) : (facingRight ? 1f : -1f);
        Vector3 checkDir = Vector3.right * direction;

        bool wallAtFeet = Physics.Raycast(transform.position + Vector3.up * 0.2f, checkDir, out RaycastHit wallHit, wallGrabCheckDistance, groundLayer);

        if (wallAtFeet)
        {
            try
            {
                if (wallHit.collider == null || !wallHit.collider.CompareTag("Wall"))
                {
                    if (isWallGrabbing) ReleaseWall();
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Тег 'Wall' не создан! Ошибка: {e.Message}");
                if (isWallGrabbing) ReleaseWall();
                return;
            }

            if (Mathf.Abs(horizontalInput) > 0.05f)
            {
                Vector3 rayStart = transform.position + Vector3.up * climbCheckHeight + checkDir * (wallGrabCheckDistance + 0.1f);

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit ledgeHit, climbCheckHeight, groundLayer))
                {
                    float platformHeight = ledgeHit.point.y - transform.position.y;

                    if (platformHeight > 0.4f)
                    {
                        if (transform.position.y + 1.5f >= ledgeHit.point.y)
                        {
                            StartCoroutine(ClimbWallRoutine(direction, ledgeHit.point));
                            return;
                        }
                        else
                        {
                            GrabWall(wallHit);
                            return;
                        }
                    }
                }
            }
        }
        else
        {
            if (isWallGrabbing) ReleaseWall();
        }
    }

    private IEnumerator ClimbWallRoutine(float direction, Vector3 ledgePoint)
    {
        isClimbing = true;
        isWallGrabbing = true;

        rb.velocity = Vector3.zero;
        rb.useGravity = false;
        playerCollider.enabled = false;

        if (animator != null)
            animator.SetTrigger("ClimbFinish");

        Vector3 startPos = transform.position;

        Vector3 targetPos = new Vector3(
            ledgePoint.x + (direction * 0.1f),
            ledgePoint.y + 0.05f,
            lockedZ
        );

        float elapsed = 0f;
        while (elapsed < climbDuration)
        {
            float t = elapsed / climbDuration;
            float curve = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, targetPos, curve);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        playerCollider.enabled = true;
        rb.useGravity = true;
        isClimbing = false;
        isWallGrabbing = false;

        if (animator != null)
            animator.SetBool("IsWallGrabbing", false);
    }

    private void GrabWall(RaycastHit hit)
    {
        isWallGrabbing = true;
        isOnWall = true;
        wallNormal = hit.normal;

        if (wallGrabMaterial != null && playerCollider != null)
            playerCollider.material = wallGrabMaterial;

        rb.velocity = new Vector3(0f, Mathf.Max(rb.velocity.y, -wallSlideSpeed), 0f);
        rb.drag = 10f;
        rb.useGravity = false;

        if (animator != null)
        {
            animator.SetBool("IsWallGrabbing", true);
            animator.SetBool("IsOnWall", true);
        }

        wasFalling = false;
        currentFallDistance = 0f;

        if (animator != null)
        {
            animator.SetBool("IsFalling", false);
            animator.SetBool("IsLongFall", false);
        }
    }

    private void ReleaseWall()
    {
        isWallGrabbing = false;
        isOnWall = false;

        if (playerCollider != null && defaultMaterial != null)
            playerCollider.material = defaultMaterial;

        rb.drag = originalDrag;
        rb.useGravity = true;

        if (animator != null)
        {
            animator.SetBool("IsWallGrabbing", false);
            animator.SetBool("IsOnWall", false);
        }
    }

    private void JumpFromWall()
    {
        if (!isWallGrabbing || isClimbing) return;

        float horizontalDir = -Mathf.Sign(wallNormal.x);
        float horizontalPower = wallJumpHorizontalForce;
        float verticalPower = wallJumpUpForce * wallJumpUpMultiplier;

        rb.velocity = new Vector3(horizontalDir * horizontalPower, verticalPower, 0f);
        ReleaseWall();

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("IsOnWall", false);
            animator.SetBool("IsWallGrabbing", false);
        }

        if (footstepScript != null)
            footstepScript.PlayTakeoffSound();
    }

    private void HandleCrouch()
    {
        if (isGrounded && !isWallGrabbing && !isClimbing)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (!isCrouching)
                {
                    isCrouching = true;
                    UpdateCollider();
                }
            }
            else
            {
                if (isCrouching)
                {
                    float checkDistance = originalColliderHeight - crouchColliderHeight + 0.2f;
                    bool headBlocked = Physics.Raycast(transform.position + Vector3.up * crouchColliderHeight, Vector3.up, checkDistance, groundLayer);

                    if (!headBlocked)
                    {
                        isCrouching = false;
                        UpdateCollider();
                    }
                }
            }
        }
        else if (isCrouching)
        {
            isCrouching = false;
            UpdateCollider();
        }
    }

    private void UpdateCollider()
    {
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();

        if (playerCollider == null)
        {
            Debug.LogError("CapsuleCollider не найден!");
            return;
        }

        if (isCrouching)
        {
            playerCollider.height = crouchColliderHeight;
            float offset = (originalColliderHeight - crouchColliderHeight) / 2f;
            playerCollider.center = new Vector3(originalColliderCenter.x, originalColliderCenter.y - offset, originalColliderCenter.z);
        }
        else
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
            isCrouching = false;
        }
    }

    private void HandleSprintAndDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isWallGrabbing && !isClimbing)
        {
            shiftPressTime = Time.time;
            if (!isDashing)
            {
                StartCoroutine(DashRoutine());
            }
        }

        if (Input.GetKey(KeyCode.LeftShift) && !isWallGrabbing && !isClimbing)
        {
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
        if (isDashing) yield break;

        isDashing = true;

        if (charMaterial) charMaterial.color = sprintColor;
        audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
        audioSource.PlayOneShot(DashSound, volume);

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

        characterModel.localPosition = staticModelPos;

        if (charMaterial) charMaterial.color = originalColor;

        rb.useGravity = true;
        isDashing = false;
    }

    private void StartSprint()
    {
        isSprinting = true;
        if (charMaterial) charMaterial.color = originalColor;
        if (animator != null) animator.SetBool("IsSprinting", true);
    }

    private void StopSprint()
    {
        isSprinting = false;
        if (!isDashing && charMaterial)
        {
            charMaterial.color = originalColor;
        }
        if (animator != null) animator.SetBool("IsSprinting", false);
    }

    private void ApplyVariableJumpHeight()
    {
        if (rb.velocity.y > 0 && !Input.GetButton("Jump") && !isWallGrabbing && !isClimbing)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void UpdateAimLayerWeight()
    {
        if (topLayerIndex == -1) return;

        float targetWeight = IsAiming ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(topLayerIndex);
        float newWeight = Mathf.Lerp(currentWeight, targetWeight, 1.0f - Mathf.Exp(-10f * Time.deltaTime));
        animator.SetLayerWeight(topLayerIndex, newWeight);
    }

    private void CheckFallingState()
    {
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

        if (rb.velocity.y < -0.1f && !isWallGrabbing && !isClimbing)
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

                if (currentFallDistance >= longFallDistance && !animator.GetBool("IsLongFall"))
                {
                    animator.SetBool("IsLongFall", true);
                    animator.SetBool("IsJumping", false);
                }

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

        if (!isGrounded && !wasFalling && rb.velocity.y < 0 && !isWallGrabbing && !isClimbing)
        {
            wasFalling = true;
            fallStartHeight = transform.position.y;
        }

        if (wasFalling && rb.velocity.y < -0.1f && !isWallGrabbing && !isClimbing)
        {
            currentFallDistance = fallStartHeight - transform.position.y;

            if (currentFallDistance >= minFallDistance)
            {
                animator.SetBool("IsFalling", true);
            }

            if (currentFallDistance >= longFallDistance)
            {
                animator.SetBool("IsLongFall", true);
                animator.SetBool("IsJumping", false);
            }

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

    // АТАКА ПИНКОМ (вызывается только если меч отключен)
    private void KickAttack()
    {
        if (isWallGrabbing || isClimbing) return;

        StartCoroutine(MovementLockCoroutine(0.3f));
        rb.velocity = new Vector3(0, rb.velocity.y, 0);

        comboStep = (comboStep == 1) ? 2 : 1;
        animator.SetInteger("AttackType", comboStep);

        StartCoroutine(AttackAnimationRoutine());
        animator.SetTrigger("Attack");

        nextAttackTime = Time.time + attackRate;
    }

    private IEnumerator AttackAnimationRoutine()
    {
        if (animator == null) yield break;

        float originalSpeed = animator.speed;
        animator.speed = attackAnimationSpeed;

        float attackDuration = 0.3f / attackAnimationSpeed;
        yield return new WaitForSeconds(attackDuration);

        animator.speed = originalSpeed;
    }

    // Метод для нанесения урона пинком (вызывается из анимации)
    public void Hit()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            HealthEnemy health = enemy.GetComponent<HealthEnemy>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
                Debug.Log($"Kick hit! Damage {attackDamage} to {enemy.name}");
            }
        }

        if (audioSource != null && kickSound != null)
        {
            audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
            audioSource.PlayOneShot(kickSound, volume);
        }
    }

    private void ApplyJump()
    {
        if (isWallGrabbing || isClimbing) return;

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
        if (animator == null) return;

        animator.SetBool("IsCrouching", isCrouching);

        float moveMagnitude = Mathf.Abs(horizontalInput);
        float animSpeed = isGrounded ? moveMagnitude : 0f;
        float speedMultiplier = isSprinting ? 2f : 1f;
        animator.SetFloat("Speed", moveMagnitude * (isSprinting ? 2f : 1f));

        if (isCrouching)
        {
            float crouchAnimSpeed = (moveMagnitude > 0.01f) ? 1.0f : 0.0f;
            animator.SetFloat("CrouchSpeed", crouchAnimSpeed);
        }

        if (!isGrounded && rb.velocity.y > 0.5f && !isWallGrabbing && !isClimbing)
        {
            animator.SetBool("IsJumping", true);
        }

        if (isGrounded && animator.GetBool("IsJumping"))
        {
            animator.SetBool("IsJumping", false);
        }

        animator.SetBool("IsWallGrabbing", isWallGrabbing);

        if (isWallGrabbing && !isClimbing)
        {
            animator.Play("WallHang", 0, hangAnimationFrame);
        }

        animator.SetBool("IsClimbing", isClimbing);
    }

    private void HandleRotation()
    {
        if (IsAiming)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 centerPoint = ray.GetPoint(dist);

                if (centerPoint.x > transform.position.x && !facingRight)
                {
                    facingRight = true;
                    targetRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else if (centerPoint.x < transform.position.x && facingRight)
                {
                    facingRight = false;
                    targetRotation = Quaternion.Euler(0f, 270f, 0f);
                }
            }
        }
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

    public void LockMovement(float duration)
    {
        StartCoroutine(MovementLockCoroutine(duration));
    }

    public void UnlockJump()
    {
        canJump = true;
        Debug.Log("Прыжок разблокирован!");
    }

    public void UnlockWallGrab()
    {
        canWallGrabAbility = true;
        Debug.Log("Цепляние за стены разблокировано!");
    }

    public void SetUseSword(bool useSword)
    {
        useSwordInsteadOfKick = useSword;
    }

    public bool IsSwordAttackEnabled()
    {
        return useSwordInsteadOfKick && swordCombat != null;
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