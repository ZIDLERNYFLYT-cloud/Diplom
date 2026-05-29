using System.Collections;
using UnityEngine;

public class FlashingLight : MonoBehaviour
{
    [Header("Настройки света")]
    [SerializeField] private Light flashlight; // Ссылка на компонент Light

    [Header("Интервалы мигания (в секундах)")]
    [SerializeField] private float minTurnOnTime = 0.1f;  // Мин. время во включенном состоянии
    [SerializeField] private float maxTurnOnTime = 0.5f;  // Макс. время во включенном состоянии
    [SerializeField] private float minTurnOffTime = 0.05f; // Мин. время в выключенном состоянии
    [SerializeField] private float maxTurnOffTime = 0.3f;  // Макс. время в выключенном состоянии

    private void Start()
    {
        // Если ссылка на свет не задана в инспекторе, пытаемся найти её на этом же объекте
        if (flashlight == null)
        {
            flashlight = GetComponent<Light>();
        }

        // Запускаем бесконечный цикл мигания
        if (flashlight != null)
        {
            StartCoroutine(FlashRoutine());
        }
        else
        {
            Debug.LogError("Компонент Light не найден!");
        }
    }

    private IEnumerator FlashRoutine()
    {
        while (true)
        {
            // Выключаем свет на случайное время
            flashlight.enabled = false;
            yield return new WaitForSeconds(Random.Range(minTurnOffTime, maxTurnOffTime));

            // Включаем свет на случайное время
            flashlight.enabled = true;
            yield return new WaitForSeconds(Random.Range(minTurnOnTime, maxTurnOnTime));
        }
    }
}
