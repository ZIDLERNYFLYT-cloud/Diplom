using UnityEngine;

public class CameraFollowe : MonoBehaviour
{
    [Header("Следование")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Поворот за мышкой (ПКМ)")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float xLimit = 20f;
    [SerializeField] private float yLimit = 30f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Quaternion originalRotation;

    private void Start()
    {
        if (target != null)
        {
            // Устанавливаем позицию сразу
            transform.position = target.position + offset;
        }
        originalRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // ВОЗВРАЩАЕМ СТАРУЮ ЛОГИКУ: Жесткая привязка позиции без Lerp
        // Это убирает микро-тряску (джиттер)
        transform.position = target.position + offset;

        // Оставляем логику поворота
        HandleMouseLook();
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButton(1))
        {
            rotationY += Input.GetAxis("Mouse X") * sensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * sensitivity;

            rotationX = Mathf.Clamp(rotationX, -xLimit, xLimit);
            rotationY = Mathf.Clamp(rotationY, -yLimit, yLimit);

            Quaternion rotationOffset = Quaternion.Euler(rotationX, rotationY, 0);
            // Поворот можно оставить плавным (Slerp), он не вызывает тряску позиции
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation * rotationOffset, Time.deltaTime * 10f);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * 5f);
            rotationX = Mathf.Lerp(rotationX, 0, Time.deltaTime * 5f);
            rotationY = Mathf.Lerp(rotationY, 0, Time.deltaTime * 5f);
        }
    }
}