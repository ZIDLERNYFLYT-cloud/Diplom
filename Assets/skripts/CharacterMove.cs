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

    [Header("Проверка земли")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;

    private Rigidbody rb;
    private float horizontalInput;
    private bool facingRight = true;
    private Quaternion targetRotation;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private float lastJumpTime; // Задержка для стабильности анимации

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
        // 1. Проверка земли
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

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

        // 4. Логика прыжка
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            ApplyJump();
        }

        // 5. Поворот персонажа
        HandleRotation();

        // 6. Управление анимациями
        UpdateAnimations();
    }

    private void ApplyJump()
    {
        // Сбрасываем вертикальную скорость перед прыжком для стабильности
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);

        animator.SetBool("IsJumping", true);

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        lastJumpTime = Time.time; // Фиксируем время прыжка
    }

    private void UpdateAnimations()
    {
        // Анимация бега (только когда на земле)
        float animSpeed = isGrounded ? Mathf.Abs(horizontalInput) : 0f;
        animator.SetFloat("Speed", animSpeed);

        // Сброс анимации прыжка
        // Добавляем условие (Time.time - lastJumpTime > 0.1f), чтобы анимация 
        // не выключалась в момент отрыва от земли, пока CheckSphere еще касается пола.
        if (isGrounded && rb.velocity.y <= 0.1f && Time.time - lastJumpTime > 0.1f)
        {
            animator.SetBool("IsJumping", false);
        }

        // Если падаем (например, сошли с уступа), тоже включаем анимацию прыжка
        if (!isGrounded && rb.velocity.y < -0.5f)
        {
            animator.SetBool("IsJumping", true);
        }
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
        // Движение через AddForce для правильной работы физики
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
    }
}