using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Настройки чекпоинта")]
    public int checkpointID = 0;
    public bool isActive = false;

    [Header("Визуальные эффекты")]
    public GameObject checkpointEffect;
    public Light checkpointLight;

    [Header("Аудио")]
    public AudioClip checkpointSound;
    [Range(0, 2)] public float soundVolume = 1f;
    public float soundMaxDistance = 30f; // Максимальная дистанция слышимости
    public float soundMinDistance = 1f;  // Минимальная дистанция (громкость 100%)

    [Header("Цвета индикации")]
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.green;

    private Renderer checkpointRenderer;
    private bool isTriggered = false;
    private AudioSource audioSource; // Добавляем свой AudioSource

    void Start()
    {
        checkpointRenderer = GetComponent<Renderer>();
        SetupAudioSource();
        UpdateVisualState();
    }

    void SetupAudioSource()
    {
        // Создаем отдельный AudioSource для чекпоинта
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Полностью 3D звук
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
            respawnSystem.SetRespawnPoint(transform.position, checkpointID);
        }

        // ПРОИГРЫВАЕМ ЗВУК через AudioSource
        if (checkpointSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(checkpointSound, soundVolume);
        }
        else if (checkpointSound != null)
        {
            // Запасной вариант с увеличенной громкостью
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

        Debug.Log($"Checkpoint {checkpointID} activated at position: {transform.position}");
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
        Gizmos.color = isTriggered ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>()?.bounds.size ?? Vector3.one);

        // Визуализируем зону слышимости
        if (Application.isPlaying && audioSource != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, audioSource.minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, audioSource.maxDistance);
        }
    }
}