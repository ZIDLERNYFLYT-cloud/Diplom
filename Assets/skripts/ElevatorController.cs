using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private GameObject interactionUI;

    [Header("Настройки лифта")]
    [SerializeField] private float liftDistance = 5f;
    [SerializeField] private float doorDistance = 3f;      // На сколько двигаются двери
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float doorSpeed = 1.5f;       // Скорость дверей (отдельно)

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource1Dveri;
    [SerializeField] private AudioSource audioSource2Message;

    [Header("Двери")]
    [SerializeField] private Transform child1;
    [SerializeField] private Transform child2;

    private bool isActivated = false;
    private bool isMovingUp = true; // Направление движения: вверх или вниз

    private Vector3 startPosition;  // Начальная позиция лифта
    private Vector3 endPosition;    // Конечная позиция лифта

    private void Awake()
    {
        if (interactionUI != null) interactionUI.SetActive(false);

        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = interactionRadius;
        }

        // Запоминаем начальную и конечную позиции
        startPosition = transform.position;
        endPosition = startPosition + Vector3.up * liftDistance;
    }

    public IEnumerator ActivateElevator()
    {
        if (isActivated) yield break;
        isActivated = true;

        if (interactionUI != null) interactionUI.SetActive(false);

        // --- ФАЗА 1: ЗАКРЫТИЕ ДВЕРЕЙ ---
        if (audioSource1Dveri != null) audioSource1Dveri.Play();

        Vector3 c1Start = child1.localPosition;
        Vector3 c1Closed = c1Start + Vector3.down * doorDistance;
        Vector3 c2Start = child2.localPosition;
        Vector3 c2Closed = c2Start + Vector3.down * doorDistance;

        float elapsed = 0;
        float doorDuration = doorDistance / doorSpeed;

        while (elapsed < doorDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / doorDuration;
            if (child1 != null) child1.localPosition = Vector3.Lerp(c1Start, c1Closed, t);
            if (child2 != null) child2.localPosition = Vector3.Lerp(c2Start, c2Closed, t);
            yield return new WaitForFixedUpdate();
        }

        // --- ФАЗА 2: СООБЩЕНИЕ И ПАУЗА ---
        if (audioSource2Message != null)
        {
            audioSource2Message.Play();
        }
        yield return new WaitForSeconds(0.5f); // Короткая пауза перед рывком лифта

        // --- ФАЗА 3: ДВИЖЕНИЕ ЛИФТА (в зависимости от направления) ---
        Vector3 liftStart = transform.position;
        Vector3 liftTarget = isMovingUp ? endPosition : startPosition;

        elapsed = 0;
        float liftDuration = liftDistance / moveSpeed;

        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / liftDuration;
            transform.position = Vector3.Lerp(liftStart, liftTarget, t);
            yield return new WaitForFixedUpdate();
        }
        transform.position = liftTarget;

        // --- ФАЗА 4: ОСТАНОВКА ПЕРЕД ОТКРЫТИЕМ ---
        yield return new WaitForSeconds(2.0f); // Лифт стоит несколько секунд

        // --- ФАЗА 5: ОТКРЫТИЕ ДВЕРЕЙ ---
        if (audioSource1Dveri != null) audioSource1Dveri.Play(); // Звук открытия

        elapsed = 0;
        // Двигаем обратно из Closed в Start
        while (elapsed < doorDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / doorDuration;
            if (child1 != null) child1.localPosition = Vector3.Lerp(c1Closed, c1Start, t);
            if (child2 != null) child2.localPosition = Vector3.Lerp(c2Closed, c2Start, t);
            yield return new WaitForFixedUpdate();
        }

        // Финальная фиксация
        if (child1 != null) child1.localPosition = c1Start;
        if (child2 != null) child2.localPosition = c2Start;

        // Меняем направление для следующего использования
        isMovingUp = !isMovingUp;

        isActivated = false; // Лифт можно использовать снова
    }
}