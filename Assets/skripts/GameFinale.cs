using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class GameFinale : MonoBehaviour
{
    [Header("Персонаж")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string kneelingAnimName = "Kneeling"; // анимация вставания на колени
    [SerializeField] private string kneelingIdleAnimName = "KneelingIdle"; // idle анимация стоя на коленях

    [Header("Объекты для активации")]
    [SerializeField] private GameObject targetObject; // объект, скрипт которого нужно запустить
    [SerializeField] private GameObject objectToActivate; // объект, который включится в конце

    [Header("Видео и аудио")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource1;
    [SerializeField] private AudioSource audioSource2;

    [Header("Настройки")]
    [SerializeField] private string playerTag = "Player";

    [Header("Таймеры (паузы ПЕРЕД каждым действием)")]
    [SerializeField, Tooltip("Пауза после анимации перед запуском targetObject")]
    private float delayBeforeTargetObject = 0.5f;

    [SerializeField, Tooltip("Пауза после targetObject перед включением видео")]
    private float delayBeforeVideo = 0.5f;

    [SerializeField, Tooltip("Пауза после окончания видео перед первым аудио")]
    private float delayBeforeAudio1 = 0f;

    [SerializeField, Tooltip("Пауза после первого аудио перед вторым")]
    private float delayBeforeAudio2 = 0f;

    [SerializeField, Tooltip("Пауза после второго аудио перед последним объектом")]
    private float delayBeforeFinalObject = 0f;

    private bool isFinaleStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isFinaleStarted && other.CompareTag(playerTag))
        {
            isFinaleStarted = true;
            StartCoroutine(StartFinaleSequence());
        }
    }

    private IEnumerator StartFinaleSequence()
    {
        // 1. Запускаем анимацию вставания на колени (в обратном порядке)
        playerAnimator.speed = -1f; // проигрывание наоборот
        playerAnimator.Play(kneelingAnimName, 0, 1f); // начинаем с конца

        // Ждем окончания обратной анимации
        yield return new WaitForSeconds(GetAnimationLength(kneelingAnimName));

        // 2. Останавливаем обратную анимацию и запускаем зацикленный idle
        playerAnimator.speed = 1f; // сбрасываем скорость
        playerAnimator.Play(kneelingIdleAnimName, 0, 0f);
        playerAnimator.SetBool(kneelingIdleAnimName, true);

        // --- ТАЙМЕР 1 ---
        yield return new WaitForSeconds(delayBeforeTargetObject);

        // 3. Запускаем скрипт с целевого объекта
        if (targetObject != null)
        {
            targetObject.SetActive(true);
            // Если нужно вызвать конкретный метод:
            var scripts = targetObject.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                script.Invoke("StartScript", 0f); // или любой другой метод
            }
        }

        // --- ТАЙМЕР 2 ---
        yield return new WaitForSeconds(delayBeforeVideo);

        // 4. Включаем видео
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            // Ждем окончания видео
            yield return new WaitWhile(() => videoPlayer.isPlaying);
        }

        // --- ТАЙМЕР 3 ---
        yield return new WaitForSeconds(delayBeforeAudio1);

        // 5. Воспроизводим первое аудио и ждем его окончания
        if (audioSource1 != null && audioSource1.clip != null)
        {
            audioSource1.Play();
            yield return new WaitWhile(() => audioSource1.isPlaying);
        }

        // --- ТАЙМЕР 4 ---
        yield return new WaitForSeconds(delayBeforeAudio2);

        // 6. Воспроизводим второе аудио и ждем его окончания
        if (audioSource2 != null && audioSource2.clip != null)
        {
            audioSource2.Play();
            yield return new WaitWhile(() => audioSource2.isPlaying);
        }

        // --- ТАЙМЕР 5 ---
        yield return new WaitForSeconds(delayBeforeFinalObject);

        // 7. Включаем последний объект
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }
    }

    // Вспомогательный метод для получения длины анимации
    private float GetAnimationLength(string animName)
    {
        if (playerAnimator == null) return 1f;

        RuntimeAnimatorController ac = playerAnimator.runtimeAnimatorController;
        if (ac == null) return 1f;

        AnimationClip[] clips = ac.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animName)
            {
                return clip.length;
            }
        }

        Debug.LogWarning($"Анимация '{animName}' не найдена!");
        return 1f;
    }
}