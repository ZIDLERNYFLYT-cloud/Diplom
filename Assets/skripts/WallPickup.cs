using UnityEngine;

public class WallPickup : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionUI; // Ссылка на объект с текстом (Canvas или Sprite)

    private bool playerInRange = false;
    private PlayerSideController playerScript;

    private void Awake()
    {
        // Скрываем подсказку при старте игры
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    [SerializeField] private float interactionRadius = 3f; // Желаемый радиус

    private void Start()
    {
        // Ищем коллайдер на этом объекте
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = interactionRadius; // Устанавливаем радиус из кода
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            PickUp();
        }
    }

    private void PickUp()
    {
        if (playerScript != null)
        {
            playerScript.UnlockWallGrab();

            // Здесь можно добавить эффект, например:
            // Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerScript = other.GetComponent<PlayerSideController>();
            playerInRange = true;

            // Показываем подсказку
            if (interactionUI != null) interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerScript = null;

            // Скрываем подсказку
            if (interactionUI != null) interactionUI.SetActive(false);
        }
    }
}