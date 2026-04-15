using UnityEngine;

public class CharacterFootsteps : MonoBehaviour
{
    [Header("Настройки звука")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound; // Звук прыжка/приземления
    [SerializeField] private AudioClip landSound;       // Звук приземления
    [SerializeField] private AudioClip takeoffSound;    // Звук отрыва от земли (добавлено)

    [Range(0, 1)][SerializeField] private float volume = 0.5f;
    [SerializeField] private float pitchRange = 0.2f;
    [Range(0, 1)][SerializeField] private float volumeJump = 0.6f;
    [Range(0, 1)][SerializeField] private float volumeTakeoff = 0.5f; // Громкость звука отрыва
    
    // Этот метод вызывается из АНИМАЦИИ (Animation Events)
    public void PlayFootstepSound()
    {
        // Если скрипт на дочернем объекте, ищем контроллер в родителе
        var controller = GetComponentInParent<PlayerSideController>();

        // Если мы НЕ на земле — выходим, чтобы шаги не звучали в прыжке
        if (controller != null && !Physics.CheckSphere(controller.groundCheck.position, 0.25f, controller.groundLayer))
            return;

        if (footstepSounds.Length > 0 && audioSource != null)
        {
            int index = Random.Range(0, footstepSounds.Length);
            audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
            audioSource.PlayOneShot(footstepSounds[index], volume);
        }
    }

    // Этот метод вызывается ИЗ СКРИПТА передвижения
    public void PlayJumpOrLandSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
            audioSource.PlayOneShot(jumpSound, volumeJump);
        }
    }

    // НОВЫЙ МЕТОД: Звук отрыва от земли
    public void PlayTakeoffSound()
    {
        if (takeoffSound != null && audioSource != null)
        {
            audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
            audioSource.PlayOneShot(takeoffSound, volumeTakeoff);
        }
    }

    
}