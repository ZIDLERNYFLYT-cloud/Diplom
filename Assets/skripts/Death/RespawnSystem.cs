using UnityEngine;
using System.Collections;

public class RespawnSystem : MonoBehaviour
{
    [Header("Настройки возрождения")]
    public Transform customRespawnPoint;
    public Vector3 defaultRespawnPoint = new Vector3(0, 1, 0);
    public float respawnOffsetY = 0.5f;
    public Vector3 customOffset = Vector3.zero;
    public bool useCustomOffset = false;

    [Header("Настройки оси Z")]
    public bool autoGetZFromPlayer = true; // Автоматически брать Z из позиции персонажа
    public float manualFixedZ = 0f; // Ручная установка Z, если autoGetZFromPlayer = false

    [Header("Задержки")]
    public float respawnDelay = 0.5f;
    public float teleportDelay = 0.1f;

    [Header("Эффекты")]
    public GameObject respawnEffect;
    public AudioClip respawnSound;

    private Vector3 currentRespawnPoint;
    private int currentCheckpointID = -1;
    private float playerFixedZ = 0f; // Сохраняем Z из позиции персонажа

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (customRespawnPoint != null)
        {
            currentRespawnPoint = GetRespawnPosition(customRespawnPoint.position);
            defaultRespawnPoint = currentRespawnPoint;
        }
        else
        {
            currentRespawnPoint = GetRespawnPosition(defaultRespawnPoint);
        }
    }

    void Start()
    {
        // Получаем Z координату из стартовой позиции персонажа
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerFixedZ = player.transform.position.z;
            Debug.Log($"Player's Z position captured: {playerFixedZ}");
        }
        else
        {
            Debug.LogWarning("Player not found! Using manual Z value if set.");
            playerFixedZ = manualFixedZ;
        }
    }

    private Vector3 GetRespawnPosition(Vector3 originalPosition)
    {
        Vector3 newPosition = originalPosition;

        // Применяем отступ
        if (useCustomOffset)
        {
            newPosition += customOffset;
        }
        else
        {
            newPosition += new Vector3(0, respawnOffsetY, 0);
        }

        // Применяем Z из позиции персонажа
        if (autoGetZFromPlayer)
        {
            newPosition.z = playerFixedZ;
        }
        else
        {
            newPosition.z = manualFixedZ;
        }

        return newPosition;
    }

    public void SetRespawnPoint(Vector3 newPosition, int checkpointID = -1)
    {
        currentRespawnPoint = GetRespawnPosition(newPosition);
        currentCheckpointID = checkpointID;
        Debug.Log($"Respawn point set to: {currentRespawnPoint} (Player Z: {playerFixedZ})");
    }

    public void SetRespawnPoint(Transform newRespawnTransform, int checkpointID = -1)
    {
        if (newRespawnTransform != null)
        {
            SetRespawnPoint(newRespawnTransform.position, checkpointID);
        }
    }

    public Vector3 GetCurrentRespawnPoint()
    {
        return currentRespawnPoint;
    }

    // Обновить Z из текущей позиции игрока (можно вызвать при необходимости)
    public void UpdatePlayerZFromCurrentPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerFixedZ = player.transform.position.z;
            Debug.Log($"Player Z updated to: {playerFixedZ}");

            // Обновляем текущую точку возрождения с новой Z
            if (currentCheckpointID != -1)
            {
                Vector3 oldPoint = currentRespawnPoint;
                currentRespawnPoint.z = playerFixedZ;
                Debug.Log($"Respawn point Z updated from {oldPoint.z} to {currentRespawnPoint.z}");
            }
        }
    }

    public void RespawnPlayer(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
        {
            Debug.LogError("Cannot respawn: PlayerHealth is null!");
            return;
        }

        StartCoroutine(RespawnCoroutine(playerHealth));
    }

    IEnumerator RespawnCoroutine(PlayerHealth playerHealth)
    {
        GameObject player = playerHealth.gameObject;

        // Получаем все компоненты
        CharacterController controller = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        PlayerSideController moveScript = player.GetComponent<PlayerSideController>();

        // 1. Отключаем управление
        if (moveScript != null)
        {
            moveScript.enabled = false;
        }

        // 2. Останавливаем физику
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        // 3. Отключаем CharacterController
        if (controller != null)
        {
            controller.enabled = false;
        }

        yield return new WaitForSeconds(teleportDelay);

        // 4. Телепортируем с учетом Z из позиции персонажа
        Vector3 respawnPos = GetRespawnPosition(currentRespawnPoint);

        // Дополнительная проверка: если Z все еще не совпадает, принудительно устанавливаем
        if (autoGetZFromPlayer && Mathf.Abs(respawnPos.z - playerFixedZ) > 0.01f)
        {
            respawnPos.z = playerFixedZ;
            Debug.Log($"Forced Z correction to: {playerFixedZ}");
        }

        player.transform.position = respawnPos;

        // Сбрасываем вращение
        Vector3 currentRot = player.transform.eulerAngles;
        player.transform.eulerAngles = new Vector3(currentRot.x, currentRot.y, 0);

        Debug.Log($"Player teleported to: {respawnPos} (Z from player: {respawnPos.z})");

        // 5. Сбрасываем скорость
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(teleportDelay);

        // 6. Включаем CharacterController
        if (controller != null)
        {
            controller.enabled = true;
            controller.Move(Vector3.zero);
        }

        // 7. Восстанавливаем здоровье
        playerHealth.ResetHealth();

        // 8. Сбрасываем анимации
        if (playerHealth.anim != null)
        {
            playerHealth.anim.Rebind();
            playerHealth.anim.Play("Idle");
        }

        // 9. Включаем управление
        if (moveScript != null)
        {
            yield return null;
            moveScript.enabled = true;
        }

        // 10. Размораживаем физику
        if (rb != null)
        {
            rb.WakeUp();
        }

        // 11. Эффекты
        if (respawnEffect != null)
        {
            Instantiate(respawnEffect, respawnPos, Quaternion.identity);
        }

        if (respawnSound != null)
        {
            AudioSource.PlayClipAtPoint(respawnSound, respawnPos);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 12. Финальная проверка Z координаты
        Vector3 finalPos = player.transform.position;
        if (Mathf.Abs(finalPos.z - playerFixedZ) > 0.01f)
        {
            finalPos.z = playerFixedZ;
            player.transform.position = finalPos;
            Debug.Log($"Final Z correction applied: {finalPos.z}");
        }

        Debug.Log($"Player respawned successfully at {respawnPos}");
    }

    public void ResetToDefaultRespawn()
    {
        currentRespawnPoint = defaultRespawnPoint;
        currentCheckpointID = -1;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 respawnPos = Application.isPlaying ? currentRespawnPoint : GetRespawnPosition(defaultRespawnPoint);
        Gizmos.DrawWireSphere(respawnPos, 0.5f);

        Gizmos.color = Color.yellow;
        Vector3 originalPos = Application.isPlaying ? GetOriginalRespawnPoint() : defaultRespawnPoint;
        Gizmos.DrawLine(originalPos, respawnPos);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(originalPos, Vector3.one);

        // Рисуем линию до фиксированной Z
        Gizmos.color = Color.blue;
        Vector3 zLineStart = new Vector3(respawnPos.x - 1, respawnPos.y, respawnPos.z);
        Vector3 zLineEnd = new Vector3(respawnPos.x + 1, respawnPos.y, respawnPos.z);
        Gizmos.DrawLine(zLineStart, zLineEnd);
    }

    private Vector3 GetOriginalRespawnPoint()
    {
        Vector3 original = currentRespawnPoint;

        // Убираем отступ для получения оригинальной позиции
        if (useCustomOffset)
        {
            original -= customOffset;
        }
        else
        {
            original -= new Vector3(0, respawnOffsetY, 0);
        }

        return original;
    }
}