using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private Transform[] waypoints; // Массив точек, по которым движется объект
    [SerializeField] private float speed = 5f;        // Скорость перемещения
    [SerializeField] private bool loop = true;        // Циклическое движение или остановка в конце

    private int currentWaypointIndex = 0;

    void Update()
    {
        // Если точки не заданы, ничего не делаем
        if (waypoints == null || waypoints.Length == 0) return;

        MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        // Получаем позицию текущей целевой точки
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // Плавно перемещаем объект к цели
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Проверяем, достиг ли объект точки (с небольшой погрешностью)
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            SetNextWaypoint();
        }
    }

    private void SetNextWaypoint()
    {
        currentWaypointIndex++;

        // Если дошли до конца списка точек
        if (currentWaypointIndex >= waypoints.Length)
        {
            if (loop)
            {
                currentWaypointIndex = 0; // Возвращаемся к первой точке
            }
            else
            {
                currentWaypointIndex = waypoints.Length - 1; // Остаемся на последней точке
                enabled = false; // Отключаем скрипт, так как движение окончено
            }
        }
    }
}