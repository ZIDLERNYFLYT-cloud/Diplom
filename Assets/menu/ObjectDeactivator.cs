using System.Collections;
using UnityEngine;

public class ObjectDeactivator : MonoBehaviour
{
    [Header("Настройки Первого Объекта")]
    [Tooltip("Ссылка на первый объект")]
    public GameObject firstObject;
    [Tooltip("Через сколько секунд ВЫКЛЮЧИТЬ первый объект")]
    public float delayBeforeFirstDisable = 2f;

    [Header("Настройки Второго Объекта")]
    [Tooltip("Ссылка на второй объект")]
    public GameObject secondObject;
    [Tooltip("Через сколько секунд ПОСЛЕ ПЕРВОГО выключить второй объект")]
    public float delayBeforeSecondDisable = 3f;

    void Start()
    {
        // Запускаем таймер выключения
        StartCoroutine(DeactivateObjectsRoutine());
    }

    IEnumerator DeactivateObjectsRoutine()
    {
        // Ждем и выключаем первый объект
        yield return new WaitForSeconds(delayBeforeFirstDisable);
        if (firstObject != null)
        {
            firstObject.SetActive(false);
        }

        // Ждем и выключаем второй объект (отсчет идет после выключения первого)
        yield return new WaitForSeconds(delayBeforeSecondDisable);
        if (secondObject != null)
        {
            secondObject.SetActive(false);
        }
    }
}
