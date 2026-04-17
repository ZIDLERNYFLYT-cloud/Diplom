using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    private Animator anim;
    private Camera mainCam;
    private PlayerSideController movementScript; // Ссылка на ваш основной скрипт движения

    [Header("Настройки анимации")]
    [SerializeField] private float aimLayerWeightSpeed = 8f;
    private int topLayerIndex;
    private bool isAiming;

    [Header("Стрельба")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletForce = 25f;
    [SerializeField] private float fireRate = 0.25f;
    private float nextFireTime;

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;

    [Header("IK и Поворот (Вид сбоку)")]
    [SerializeField] private Transform spineBone;
    [SerializeField] private Vector3 rotationOffset = new Vector3(0, 90, 0); // Подберите под вашу модель

    void Start()
    {
        anim = GetComponent<Animator>();
        mainCam = Camera.main;
        movementScript = GetComponent<PlayerSideController>();
        topLayerIndex = anim.GetLayerIndex("TopLayer");

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        HandleInput();
        UpdateLayers();
        HandleFaceDirection(); // Поворот лица к мышке
    }

    // LateUpdate критически важен для ручного поворота костей поверх анимации
    void LateUpdate()
    {
        if (isAiming && spineBone != null)
        {
            RotateSpineTowardsMouse();
        }
    }

    void HandleInput()
    {
        // Прицеливание на ПКМ
        isAiming = Input.GetMouseButton(1);
        anim.SetBool("isAiming", isAiming);

        // Стрельба на ЛКМ (только если целимся)
        if (isAiming && Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        anim.SetTrigger("Shoot");

        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound);

        // Рассчитываем направление в плоскости (без Z)
        Vector3 targetPoint = GetMouseWorldPosition();
        Vector3 shootDir = (targetPoint - firePoint.position).normalized;
        shootDir.z = 0;

        // Создаем пулю
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDir));

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.AddForce(shootDir * bulletForce, ForceMode.Impulse);
        }
    }

    void RotateSpineTowardsMouse()
    {
        Vector3 targetPoint = GetMouseWorldPosition();
        Vector3 direction = targetPoint - spineBone.position;
        direction.z = 0; // Игнорируем глубину для вида сбоку

        if (direction != Vector3.zero)
        {
            // Находим нужный поворот
            Quaternion targetRot = Quaternion.LookRotation(direction);
            // Накладываем смещение, чтобы руки смотрели правильно
            spineBone.rotation = targetRot * Quaternion.Euler(rotationOffset);
        }
    }

    // Поворачивает всё тело персонажа (влево/вправо) в зависимости от положения мыши
    void HandleFaceDirection()
    {
        if (!isAiming) return;

        Vector3 mousePos = GetMouseWorldPosition();

        // Если мышь правее персонажа
        if (mousePos.x > transform.position.x)
        {
            // Поворачиваем вправо (угол 90 взят из вашего PlayerSideController)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 90f, 0), Time.deltaTime * 15f);
        }
        // Если мышь левее персонажа
        else if (mousePos.x < transform.position.x)
        {
            // Поворачиваем влево (угол 270 взят из вашего PlayerSideController)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 270f, 0), Time.deltaTime * 15f);
        }
    }

    // Вспомогательный метод для определения точной точки клика в 2D плоскости
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        // Создаем плоскость на оси Z персонажа
        Plane groundPlane = new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position + transform.right;
    }

    void UpdateLayers()
    {
        if (topLayerIndex == -1) return;

        float targetWeight = isAiming ? 1f : 0f;
        float currentWeight = anim.GetLayerWeight(topLayerIndex);
        anim.SetLayerWeight(topLayerIndex, Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * aimLayerWeightSpeed));
    }
}