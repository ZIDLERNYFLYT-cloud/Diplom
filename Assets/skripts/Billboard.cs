using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCamera;

    void Start() => mainCamera = Camera.main.transform;

    void LateUpdate()
    {
        // Заставляет объект всегда смотреть в сторону камеры
        transform.LookAt(transform.position + mainCamera.forward);
    }
}