using UnityEngine;

public class CharacterFootsteps : MonoBehaviour
{
    [Header("Настройки звука")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepSounds; // Массив звуков (шаг левой, шаг правой)
    [SerializeField] private AudioClip jumpSound;

    [Range(0, 1)][SerializeField] private float volume = 0.5f;
    [SerializeField] private float pitchRange = 0.2f; // Разброс высоты звука для естественности
    [Range(0, 1)][SerializeField] private float volumejump = 0.5f;

    // Этот метод будет вызываться из анимации
    public void PlayFootstepSound()
    {
        if (footstepSounds.Length == 0 || audioSource == null)
        {
            Debug.LogWarning("Звуки не назначены или AudioSource отсутствует!");
            return;
        }

        // Выбираем случайный звук из массива
        int index = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[index];

        // Немного меняем Pitch, чтобы шаги не звучали идентично
        audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);

        // Воспроизводим один раз
        audioSource.PlayOneShot(clip, volume);
        Debug.Log("Звук");
    }

    public void jumpSoundPlay()
    {
        if (footstepSounds.Length == 0 || audioSource == null)
        {
            Debug.LogWarning("Звуки не назначены или AudioSource отсутствует!");
            return;
        }

        // Выбираем случайный звук из массива
        int index = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[index];

        // Немного меняем Pitch, чтобы шаги не звучали идентично
        audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);

        // Воспроизводим один раз
        audioSource.PlayOneShot(clip, volumejump);
        Debug.Log("Звук");
    }

}