using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class GameFinale : MonoBehaviour
{
    [Header("Персонаж")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField, Tooltip("Имя триггера в Animator Controller")]
    private string kneelTriggerName = "StartKneel";

    [SerializeField, Tooltip("Длительность анимации вставания на колени (в секундах)")]
    private float kneelAnimationDuration = 2.5f;

    [Header("Целевой объект")]
    [SerializeField] private GameObject targetObject;

    [Header("Скрипты")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private MonoBehaviour[] scriptsToEnable;

    [Header("Финальный объект")]
    [SerializeField] private GameObject objectToActivate;

    [Header("Объекты для отключения")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Видео и аудио")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource1;
    [SerializeField] private AudioSource audioSource2;

    [Header("Настройки")]
    [SerializeField] private string playerTag = "Player";

    [Header("Паузы")]
    [SerializeField] private float delayBeforeTargetObject = 0.5f;
    [SerializeField] private float delayBeforeVideo = 0.5f;
    [SerializeField] private float delayBeforeAudio1 = 0f;
    [SerializeField] private float delayBeforeAudio2 = 0f;
    [SerializeField] private float delayBeforeFinalObject = 0f;

    private bool isFinaleStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isFinaleStarted || !other.CompareTag(playerTag))
            return;

        isFinaleStarted = true;

        // Получаем компоненты
        PlayerSideController playerController = other.GetComponent<PlayerSideController>();
        Rigidbody rb = other.GetComponent<Rigidbody>();

        // Отключаем управление игроком
        if (playerController != null)
            playerController.enabled = false;

        // Полная остановка физики
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(StartFinaleSequence(other.transform));
    }

    private IEnumerator StartFinaleSequence(Transform playerTransform)
    {
        // 1. Отключаем лишние объекты
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null) obj.SetActive(false);
        }

        // 2. Запускаем анимацию (самая частая причина проблем — исправлено)
        if (playerAnimator != null)
        {
            // Принудительно переводим в Idle перед триггером (очень помогает)
            playerAnimator.ResetTrigger(kneelTriggerName);
            playerAnimator.Play("Idle", 0, 0f);           // ← Важно!

            yield return null; // Даём один кадр на переход в Idle

            playerAnimator.SetTrigger(kneelTriggerName);
            Debug.Log("Триггер анимации запущен: " + kneelTriggerName);
        }
        else
        {
            Debug.LogError("PlayerAnimator не назначен в GameFinale!");
        }

        // Ждём завершения анимации
        yield return new WaitForSeconds(kneelAnimationDuration);

        yield return new WaitForSeconds(delayBeforeTargetObject);

        // 3. Активация/деактивация объектов и скриптов
        if (targetObject != null)
            targetObject.SetActive(true);

        foreach (MonoBehaviour script in scriptsToDisable)
            if (script != null) script.enabled = false;

        foreach (MonoBehaviour script in scriptsToEnable)
            if (script != null) script.enabled = true;

        yield return new WaitForSeconds(delayBeforeVideo);

        // 4. Видео
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            yield return new WaitWhile(() => videoPlayer.isPlaying);
        }

        yield return new WaitForSeconds(delayBeforeAudio1);

        // 5. Аудио 1
        if (audioSource1 != null && audioSource1.clip != null)
        {
            audioSource1.Play();
            yield return new WaitWhile(() => audioSource1.isPlaying);
        }

        yield return new WaitForSeconds(delayBeforeAudio2);

        // 6. Аудио 2
        if (audioSource2 != null && audioSource2.clip != null)
        {
            audioSource2.Play();
            yield return new WaitWhile(() => audioSource2.isPlaying);
        }

        yield return new WaitForSeconds(delayBeforeFinalObject);

        // 7. Финальный объект
        if (objectToActivate != null)
            objectToActivate.SetActive(true);
    }
}