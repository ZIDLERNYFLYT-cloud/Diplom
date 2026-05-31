using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectInteractor : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionUI;

    [Header("Задержка перед активацией")]
    [SerializeField] private float activationDelay = 0f; // Задержка в секундах
    [SerializeField] private bool showDelayProgress = true; // Показывать ли прогресс задержки

    [Header("Скрипты для активации (несколько)")]
    [SerializeField] private List<MonoBehaviour> scriptsToActivate = new List<MonoBehaviour>();

    [Header("GameObject для активации (опционально)")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("Настройки триггера")]
    [SerializeField] private float interactionRadius = 3f;

    private bool playerInRange = false;
    private PlayerSideController playerScript;
    private bool isActivating = false;
    private float activationTimer = 0f;

    // Опционально: UI для отображения прогресса задержки
    [Header("UI Прогресса (опционально)")]
    [SerializeField] private UnityEngine.UI.Image progressImage;
    [SerializeField] private GameObject progressPanel;

    private void Awake()
    {
        // Выключаем все целевые скрипты на старте
        if (scriptsToActivate != null)
        {
            foreach (var script in scriptsToActivate)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }

        // Выключаем все целевые объекты на старте
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    private void Start()
    {
        // Настраиваем коллайдер
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }

        col.isTrigger = true;
        col.radius = interactionRadius;

        // Скрываем UI при старте
        playerInRange = false;
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }

        // Скрываем прогресс бар при старте
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Проверяем нажатие только если игрок в зоне и не идет активация
        if (playerInRange && Input.GetKeyDown(interactionKey) && !isActivating)
        {
            if (activationDelay > 0)
            {
                // Начинаем задержку
                StartCoroutine(ActivationWithDelay());
            }
            else
            {
                // Активируем мгновенно
                Interact();
            }
        }

        // Обновляем прогресс бар если он есть и идет активация
        if (isActivating && showDelayProgress && progressImage != null && activationDelay > 0)
        {
            activationTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(activationTimer / activationDelay);
            progressImage.fillAmount = progress;
        }
    }

    private IEnumerator ActivationWithDelay()
    {
        isActivating = true;
        activationTimer = 0f;

        // Показываем прогресс
        if (showDelayProgress && progressPanel != null)
        {
            progressPanel.SetActive(true);
            if (progressImage != null)
            {
                progressImage.fillAmount = 0f;
            }
        }

        // Ждем указанную задержку
        yield return new WaitForSeconds(activationDelay);

        // Скрываем прогресс
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }

        // Активируем
        Interact();
        isActivating = false;
    }

    private void Interact()
    {
        // Активируем все скрипты
        if (scriptsToActivate != null)
        {
            foreach (var script in scriptsToActivate)
            {
                if (script != null)
                {
                    script.enabled = true;
                    Debug.Log($"[УСПЕХ] Скрипт {script.GetType().Name} запущен на объекте {script.gameObject.name}!");
                }
            }
        }

        // Активируем все объекты
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"[УСПЕХ] Объект {obj.name} активирован!");
                }
            }
        }

        // Разблокируем прыжок если есть
        if (playerScript != null)
        {
            playerScript.UnlockJump();
        }

        // Выключаем интерфейс навсегда после активации
        if (interactionUI != null) interactionUI.SetActive(false);

        // Полностью деактивируем логику взаимодействия
        playerInRange = false;
        this.enabled = false;

        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null) col.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем строго тег Игрока
        if (other.CompareTag("Player"))
        {
            playerScript = other.GetComponent<PlayerSideController>();
            playerInRange = true;

            if (interactionUI != null) interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerScript = null;

            if (interactionUI != null) interactionUI.SetActive(false);

            // Отменяем активацию если игрок вышел во время задержки
            if (isActivating)
            {
                StopAllCoroutines();
                isActivating = false;
                if (progressPanel != null) progressPanel.SetActive(false);
                Debug.Log("[ОТМЕНА] Активация отменена - игрок покинул зону");
            }
        }
    }
}