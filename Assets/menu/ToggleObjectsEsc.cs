using UnityEngine;

public class ToggleObjectsEsc : MonoBehaviour
{
    [Header("Объекты для управления")]
    [Tooltip("Первый объект")]
    public GameObject firstObject;

    [Tooltip("Второй объект")]
    public GameObject secondObject;

    void Update()
    {
        // Проверяем, нажата ли клавиша Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Переключаем первый объект на противоположное состояние
            if (firstObject != null)
            {
                firstObject.SetActive(!firstObject.activeSelf);
            }

            // Переключаем второй объект на противоположное состояние
            if (secondObject != null)
            {
                secondObject.SetActive(!secondObject.activeSelf);
            }
        }
    }
}
