using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    public Transform player;          // Персонаж
    public Light spotLight;           // Ссылка на ваш Spot Light

    [Header("Настройки обнаружения")]
    public float visionDistance = 12f; // Дальность луча (должна совпадать с Range света)
    [Range(0, 180)]
    public float viewAngle = 40f;      // Угол обзора (должен совпадать со Spot Angle света)
    public LayerMask obstacleLayer;    // Слой стен

    [Header("Патрулирование")]
    public float patrolAngleRange = 45f;
    public float patrolSpeed = 1.5f;
    public float trackingSpeed = 5f;   // Скорость поворота за игроком

    [Header("Цвета света")]
    public Color normalColor = Color.white;
    public Color alertColor = Color.red;

    private Quaternion initialRotation;
    private bool isPlayerDetected = false;

    void Start()
    {
        initialRotation = transform.rotation;

        // Синхронизируем настройки света со скриптом при старте
        if (spotLight != null)
        {
            spotLight.type = LightType.Spot;
            spotLight.range = visionDistance;
            spotLight.spotAngle = viewAngle;
            spotLight.color = normalColor;
        }
    }

    void Update()
    {
        if (player == null || spotLight == null) return;

        if (CanSeePlayer())
        {
            isPlayerDetected = true;
            TrackPlayer();
            spotLight.color = Color.Lerp(spotLight.color, alertColor, Time.deltaTime * 10f);
        }
        else
        {
            isPlayerDetected = false;
            Patrol();
            spotLight.color = Color.Lerp(spotLight.color, normalColor, Time.deltaTime * 2f);
        }
    }

    void TrackPlayer()
    {
        // Вычисляем направление на игрока
        Vector3 direction = player.position - transform.position;
        // Создаем вращение в сторону игрока
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // В 2.5D мы часто хотим ограничить вращение только одной осью (например, Y)
        // Если вам нужно полное вращение (вверх/вниз и влево/вправо), используйте это:
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * trackingSpeed);
    }

    void Patrol()
    {
        // Качание камеры влево-вправо относительно начального поворота
        float angle = Mathf.Sin(Time.time * patrolSpeed) * patrolAngleRange;
        Quaternion rotationOffset = Quaternion.AngleAxis(angle, Vector3.up); // Используйте Vector3.up или Vector3.forward в зависимости от оси

        transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation * rotationOffset, Time.deltaTime * patrolSpeed);
    }

    bool CanSeePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Проверка дистанции
        if (distanceToPlayer < visionDistance)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleBetweenCamAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);

            // 2. Проверка, входит ли игрок в угол конуса света
            if (angleBetweenCamAndPlayer < viewAngle / 2f)
            {
                // 3. Проверка препятствий (Raycast)
                RaycastHit hit;
                if (Physics.Raycast(transform.position, dirToPlayer, out hit, visionDistance, ~0))
                {
                    if (hit.collider.transform == player)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Для визуальной настройки в редакторе
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle / 2f, transform.up) * transform.forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle / 2f, transform.up) * transform.forward;

        Gizmos.DrawRay(transform.position, leftBoundary * visionDistance);
        Gizmos.DrawRay(transform.position, rightBoundary * visionDistance);
        Gizmos.DrawLine(transform.position + leftBoundary * visionDistance, transform.position + rightBoundary * visionDistance);
    }
}