using UnityEngine;

public class CameraFollowe : MonoBehaviour
{
    [Header("Следование")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Смещение за мышью (ПКМ)")]
    [SerializeField] private float mouseOffsetFactor = 5f; // Насколько сильно камера тянется к мыши
    [SerializeField] private float followSmoothness = 10f; // Плавность следования

    

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Quaternion originalRotation;

    private void Start()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
        originalRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetCameraPos;

        // Если зажата ПКМ — камера смещается к курсору
        //if (Input.GetMouseButton(1))
        //{
        //    // Получаем положение мыши в процентах от экрана (от -0.5 до 0.5)
        //    Vector3 mouseViewportPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        //    Vector3 mouseInfluence = new Vector3(mouseViewportPos.x - 0.5f, mouseViewportPos.y - 0.5f, 0);

        //    // Смещение: Позиция цели + базовый оффсет + влияние мыши
        //    // Если вы хотите, чтобы центр был ПРЯМО на курсоре, увеличьте mouseOffsetFactor
        //    targetCameraPos = target.position + offset + (mouseInfluence * mouseOffsetFactor * 2f);
        //}
        //else
        //{
        // Если ПКМ не нажата — просто жестко следует за игроком
        targetCameraPos = target.position + offset;
        //}

        //// Используем Lerp, чтобы камера не прыгала мгновенно при нажатии кнопки
        transform.position = Vector3.Lerp(transform.position, targetCameraPos, Time.deltaTime * followSmoothness);


    }

    
}