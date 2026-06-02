using UnityEngine;

public class TeleportObject : MonoBehaviour
{
    // Перетащите сюда точку-цель (пустой объект) в инспекторе
    public Transform targetPosition;

    private void Start()
    {
        MoveToPoint();
    }

    public void MoveToPoint()
    {
        if (targetPosition != null)
        {
            // Меняем позицию объекта на позицию цели
            transform.position = targetPosition.position;
        }
    }
}
