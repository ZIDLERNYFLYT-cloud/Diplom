using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Настройки чекпоинта")]
    public int checkpointID = 0;
    public bool isActive = false;

    [Header("Настройки спавна")]
    public float spawnHeightOffset = 0f; // Смещение по Y от позиции чекпоинта

    [Header("Визуальные эффекты")]
    public GameObject checkpointEffect;
    public Light checkpointLight;

    [Header("Аудио")]
    public AudioClip checkpointSound;
    [Range(0, 2)] public float soundVolume = 1f;
    public float soundMaxDistance = 30f;
    public float soundMinDistance = 1f;

    [Header("Цвета индикации")]
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.green;

    private Renderer checkpointRenderer;
    private bool isTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        checkpointRenderer = GetComponent<Renderer>();
        SetupAudioSource();
        UpdateVisualState();
    }

    void SetupAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.minDistance = soundMinDistance;
        audioSource.volume = soundVolume;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        if (isTriggered) return;

        isTriggered = true;
        isActive = true;

        RespawnSystem respawnSystem = FindObjectOfType<RespawnSystem>();
        if (respawnSystem != null)
        {
            // Вычисляем позицию спавна с учётом смещения по Y
            Vector3 spawnPoint = transform.position;
            spawnPoint.y += spawnHeightOffset;
            respawnSystem.SetRespawnPoint(spawnPoint, checkpointID);
        }

        if (checkpointSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(checkpointSound, soundVolume);
        }
        else if (checkpointSound != null)
        {
            AudioSource.PlayClipAtPoint(checkpointSound, transform.position, soundVolume * 2f);
        }
        else
        {
            Debug.LogWarning("Checkpoint sound not assigned!");
        }

        if (checkpointEffect != null)
        {
            Instantiate(checkpointEffect, transform.position, Quaternion.identity);
        }

        UpdateVisualState();
        DeactivatePreviousCheckpoints();

        // Для отладки выводим итоговую позицию спавна
        Debug.Log($"Checkpoint {checkpointID} activated. Spawn position: {transform.position + Vector3.up * spawnHeightOffset}");
    }

    void UpdateVisualState()
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = isTriggered ? activeColor : inactiveColor;
        }

        if (checkpointLight != null)
        {
            checkpointLight.color = isTriggered ? activeColor : inactiveColor;
            checkpointLight.enabled = isTriggered;
        }
    }

    void DeactivatePreviousCheckpoints()
    {
        Checkpoint[] allCheckpoints = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint cp in allCheckpoints)
        {
            if (cp != this && cp.isActive)
            {
                cp.isActive = false;
                cp.UpdateVisualState();
            }
        }
    }

    void OnDrawGizmos()
    {
        // Отображаем зону триггера чекпоинта
        Gizmos.color = isTriggered ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>()?.bounds.size ?? Vector3.one);

        // Визуализируем точку спавна (с учётом смещения) жёлтым кубом
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.5f);

        // Зоны слышимости
        if (Application.isPlaying && audioSource != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, audioSource.minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, audioSource.maxDistance);
        }
    }
}