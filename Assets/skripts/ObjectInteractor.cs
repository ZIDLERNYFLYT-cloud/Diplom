using UnityEngine;

public class ObjectInteractor : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionUI;

    [Header("Скрипт для активации")]
    [SerializeField] private MonoBehaviour scriptToActivate;

    [Header("Настройки триггера")]
    [SerializeField] private float interactionRadius = 3f;

    private bool playerInRange = false;
    private PlayerSideController playerScript;

    private void Awake()
    {
        // Выключаем целевой скрипт на старте
        if (scriptToActivate != null)
        {
            scriptToActivate.enabled = false;
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

        // ЖЕСТКОЕ СБРОС СТАРТА: Гарантируем, что при запуске надпись ВЫКЛЮЧЕНА
        playerInRange = false;
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Проверяем нажатие только если игрок РЕАЛЬНО в зоне
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            Interact();
        }
    }

    private void Interact()
    {
        if (scriptToActivate != null)
        {
            scriptToActivate.enabled = true;
            Debug.Log($"[УСПЕХ] Скрипт {scriptToActivate.GetType().Name} запущен!");
        }

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
        }
    }
}
