using UnityEngine;
using UnityEngine.UI;

public class PixelRenderTextureToggle : MonoBehaviour
{
    [Header("Ссылки на UI")]
    [Tooltip("Перетащите сюда ваш UI Toggle (чекбокс)")]
    public Toggle menuToggle;

    [Tooltip("Перетащите сюда ваш RawImage")]
    public RawImage pixelRawImageComponent;

    [Header("Настройки камеры")]
    [Tooltip("Перетащите сюда основную камеру")]
    public Camera targetCamera;

    [Tooltip("Перетащите сюда RenderTexture 'pixel'")]
    public RenderTexture pixelTexture;

    void OnEnable()
    {
        if (menuToggle != null)
        {
            // Очищаем старые подписки на всякий случай
            menuToggle.onValueChanged.RemoveListener(OnToggleClicked);
            // Жестко привязываем метод к чекбоксу через код
            menuToggle.onValueChanged.AddListener(OnToggleClicked);

            // Сразу настраиваем сцену под текущее положение галочки
            ApplyFilterState(menuToggle.isOn);
        }
    }

    void OnDisable()
    {
        if (menuToggle != null)
        {
            menuToggle.onValueChanged.RemoveListener(OnToggleClicked);
        }
    }

    // Этот метод автоматически вызывается при клике по галочке
    private void OnToggleClicked(bool isChecked)
    {
        ApplyFilterState(isChecked);
    }

    // Основная логика переключения
    private void ApplyFilterState(bool isChecked)
    {
        if (targetCamera == null || pixelTexture == null)
        {
            Debug.LogError("Критическая ошибка: Проверьте ссылки на Камеру или Текстуру в инспекторе!");
            return;
        }

        // Принудительно выключаем камеру на мгновение, чтобы сбросить буфер рендера
        targetCamera.enabled = false;

        if (isChecked)
        {
            // Направляем камеру в RenderTexture
            targetCamera.targetTexture = pixelTexture;

            if (pixelRawImageComponent != null)
            {
                pixelRawImageComponent.enabled = true;
                pixelRawImageComponent.texture = pixelTexture;
            }
        }
        else
        {
            // Полностью очищаем Target Texture у камеры
            targetCamera.targetTexture = null;

            if (pixelRawImageComponent != null)
            {
                pixelRawImageComponent.enabled = false;
            }
        }

        // Включаем камеру обратно — теперь Unity обязана применить новую текстуру
        targetCamera.enabled = true;
    }
}
