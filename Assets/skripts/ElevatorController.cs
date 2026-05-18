using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private float interactionRadius = 3f;

    [Header("Настройки лифта")]
    [SerializeField] private float liftDistance = 5f;      // На сколько поднимется лифт
    [SerializeField] private float counterweightDistance = 3f; // На сколько опустятся противовесы
    [SerializeField] private float moveSpeed = 2f;         // Скорость движения
    [SerializeField] private AudioSource audioSource;      // Ссылка на звук

    [Header("Дочерние объекты (Противовесы)")]
    [SerializeField] private Transform child1;
    [SerializeField] private Transform child2;

    private bool playerInRange = false;
    private bool isActivated = false; // Чтобы нельзя было нажать дважды

    private void Awake()
    {
        if (interactionUI != null) interactionUI.SetActive(false);

        // Настройка триггера программно
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = interactionRadius;
        }
    }

    private void Update()
    {
        if (playerInRange && !isActivated && Input.GetKeyDown(interactionKey))
        {
            StartCoroutine(ActivateElevator());
        }
    }

    private IEnumerator ActivateElevator()
    {
        isActivated = true;

        // Скрываем подсказку сразу после нажатия
        if (interactionUI != null) interactionUI.SetActive(false);

        // 1. Проигрываем звук
        if (audioSource != null)
        {
            audioSource.Play();
            // Ждем завершения звука (или уберите yield, если лифт должен ехать сразу со звуком)
            yield return new WaitForSeconds(audioSource.clip.length);
        }

        // 2. Движение
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * liftDistance;

        Vector3 child1Start = child1.localPosition;
        Vector3 child1Target = child1Start + Vector3.down * counterweightDistance;

        Vector3 child2Start = child2.localPosition;
        Vector3 child2Target = child2Start + Vector3.down * counterweightDistance;

        float elapsed = 0;
        float duration = liftDistance / moveSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Двигаем сам лифт
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Двигаем дочерние объекты локально вниз
            if (child1 != null) child1.localPosition = Vector3.Lerp(child1Start, child1Target, t);
            if (child2 != null) child2.localPosition = Vector3.Lerp(child2Start, child2Target, t);

            yield return null;
        }

        // Фиксируем финальные позиции
        transform.position = targetPos;
        if (child1 != null) child1.localPosition = child1Target;
        if (child2 != null) child2.localPosition = child2Target;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            playerInRange = true;
            if (interactionUI != null) interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionUI != null) interactionUI.SetActive(false);
        }
    }
}