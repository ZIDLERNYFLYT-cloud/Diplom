using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    public int targetFPS = 60; // Задайте нужный лимит здесь

    void Awake()
    {
        // 1. Отключаем вертикальную синхронизацию (иначе лимит не сработает)
        QualitySettings.vSyncCount = 0;

        // 2. Устанавливаем ограничение кадров
        Application.targetFrameRate = targetFPS;
    }
}
