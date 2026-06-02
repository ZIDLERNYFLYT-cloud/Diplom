using UnityEngine;

public class WaveTrigger : MonoBehaviour
{
    [Header("Настройки")]
    public WaveSpawner waveSpawner;
    public bool oneTimeUse = true;
    public float delayBeforeStart = 0f;

    [Header("Визуальная обратная связь")]
    public bool showDebugLogs = true;

    private bool triggered = false;

    void Start()
    {
        // Проверка наличия WaveSpawner при старте
        if (waveSpawner == null)
        {
            waveSpawner = FindObjectOfType<WaveSpawner>();
            if (waveSpawner == null)
                Debug.LogError($"WaveTrigger на {gameObject.name}: WaveSpawner не найден!");
        }

        // Проверка коллайдера
        Collider col = GetComponent<Collider>();
        if (col == null)
            Debug.LogError($"WaveTrigger на {gameObject.name}: Нет Collider компонента!");
        else if (!col.isTrigger)
            Debug.LogWarning($"WaveTrigger на {gameObject.name}: Collider не является триггером!");
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверка: только игрок
        if (!other.CompareTag("Player"))
            return;

        // Проверка: уже активирован
        if (triggered && oneTimeUse)
            return;

        // Проверка: WaveSpawner существует
        if (waveSpawner == null)
        {
            Debug.LogError("WaveTrigger: WaveSpawner не назначен!");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"WaveTrigger: Игрок {other.name} активировал зону!");

        triggered = true;

        if (delayBeforeStart > 0)
            Invoke(nameof(StartWaves), delayBeforeStart);
        else
            StartWaves();
    }

    void StartWaves()
    {
        if (waveSpawner != null)
        {
            Debug.Log("WaveTrigger: Запуск волн!");
            waveSpawner.StartWaves();
        }
    }

    public void ResetTrigger()
    {
        triggered = false;
        if (showDebugLogs)
            Debug.Log($"WaveTrigger на {gameObject.name}: Триггер сброшен");
    }
}