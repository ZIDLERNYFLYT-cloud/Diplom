using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private bool useSmoothing = true;

    [Header("Pixel Filter")]
    [SerializeField] private Material pixelMaterial; // Ваш материал для пиксельного фильтра
    [SerializeField] private bool enablePixelFilter = true;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Убеждаемся, что камера рендерит на экран
        if (cam.targetTexture != null)
        {
            cam.targetTexture = null;
            Debug.Log("Target texture cleared - camera now renders to Display");
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (useSmoothing)
        {
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
        else
        {
            transform.position = desiredPosition;
        }
    }

    // Этот метод автоматически вызывается Unity после рендера камеры
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (enablePixelFilter && pixelMaterial != null)
        {
            // Применяем пиксельный фильтр и выводим на экран
            Graphics.Blit(source, destination, pixelMaterial);
        }
        else
        {
            // Если фильтр отключен или нет материала - просто копируем изображение
            Graphics.Blit(source, destination);
        }
    }
}