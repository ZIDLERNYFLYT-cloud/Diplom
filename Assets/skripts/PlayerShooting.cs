using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    private Animator anim;
    private Camera mainCam;
    private PlayerSideController movementScript;

    [Header("Настройки костей")]
    [SerializeField] private Transform armBone; // Плечо правой руки
    [SerializeField] private Vector3 armRotationOffset = new Vector3(0, 90, 0);
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Стрельба")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletForce = 25f;
    [SerializeField] private float fireRate = 0.25f;
    private float nextFireTime;

    private int topLayerIndex;
    private bool isAiming;

    void Start()
    {
        anim = GetComponent<Animator>();
        mainCam = Camera.main;
        movementScript = GetComponent<PlayerSideController>();
        topLayerIndex = anim.GetLayerIndex("TopLayer");
    }

    void Update()
    {
        if (movementScript == null) return;

        isAiming = movementScript.IsAiming;
        anim.SetBool("isAiming", isAiming);

        if (isAiming && Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        UpdateLayers();
    }

    void LateUpdate()
    {
        // Вращаем руку в LateUpdate, чтобы анимация её не перебивала
        if (isAiming && armBone != null)
        {
            RotateArmTowardsMouse();
        }
    }

    private void RotateArmTowardsMouse()
    {
        Vector3 targetWorldPos = GetMouseWorldPosition();

        // Вектор от плеча до точки мыши в мире
        Vector3 aimDirection = targetWorldPos - armBone.position;

        // КРИТИЧЕСКИ ВАЖНО: обнуляем Z разницу. 
        // Пуля и рука должны двигаться только в плоскости X-Y.
        aimDirection.z = 0;

        if (aimDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimDirection);
            // Применяем вращение плеча
            armBone.rotation = Quaternion.Slerp(
                armBone.rotation,
                targetRot * Quaternion.Euler(armRotationOffset),
                Time.deltaTime * rotationSpeed
            );
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 targetWorldPos = GetMouseWorldPosition();
        // Направление полета: строго от дула к точке прицеливания
        Vector3 shootDir = (targetWorldPos - firePoint.position).normalized;

        // Гарантируем, что пуля не полетит "вглубь" (по Z)
        shootDir.z = 0;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Поворачиваем пулю «носом» по направлению полета
        bullet.transform.right = shootDir;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootDir * bulletForce;
        }

        Destroy(bullet, 5f);
    }

    private Vector3 GetMouseWorldPosition()
    {
        // Создаем луч из камеры в точку мыши
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // Плоскость, проходящая через персонажа (Z = его позиция)
        // Направлена вперед (Vector3.forward), значит сама плоскость вертикальная
        Plane aimPlane = new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));

        if (aimPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            // Принудительно ставим Z игрока, чтобы избежать погрешностей
            hitPoint.z = transform.position.z;
            return hitPoint;
        }

        // Если луч почему-то не попал (камера смотрит параллельно плоскости),
        // стреляем просто перед собой
        return transform.position + transform.right * 10f;
    }

    private void UpdateLayers()
    {
        if (topLayerIndex == -1) return;
        float targetWeight = isAiming ? 1f : 0f;
        float currentWeight = anim.GetLayerWeight(topLayerIndex);
        anim.SetLayerWeight(topLayerIndex, Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * 10f));
    }
}