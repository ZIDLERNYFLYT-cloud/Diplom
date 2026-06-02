using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки объекта")]
    // Перетащите сюда объект, который должен увеличиваться
    public Transform targetObject;

    [Header("Настройки анимации")]
    public float targetScale = 1.3f;   // Во сколько раз увеличить (1.3 = +30%)
    public float animationSpeed = 8f;  // Скорость изменения размера

    private Vector3 originalScale;
    private Vector3 currentTargetScale;

    void Start()
    {
        if (targetObject != null)
        {
            // Запоминаем исходный размер объекта
            originalScale = targetObject.localScale;
            currentTargetScale = originalScale;
        }
        else
        {
            Debug.LogWarning($"На кнопке {gameObject.name} не назначен Target Object!");
        }
    }

    void Update()
    {
        if (targetObject == null) return;

        // Плавно приближаем текущий размер объекта к целевому
        targetObject.localScale = Vector3.Lerp(
            targetObject.localScale,
            currentTargetScale,
            Time.deltaTime * animationSpeed
        );
    }

    // Срабатывает автоматически при наведении курсора на кнопку
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetObject != null)
        {
            currentTargetScale = originalScale * targetScale;
        }
    }

    // Срабатывает автоматически, когда курсор уходит с кнопки
    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObject != null)
        {
            currentTargetScale = originalScale;
        }
    }
}
