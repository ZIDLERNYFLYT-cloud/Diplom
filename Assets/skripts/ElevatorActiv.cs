using Unity.VisualScripting;
using UnityEngine;

public class ElevatorActiv : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionUI; // Ссылка на объект с текстом (Canvas или Sprite)
    [SerializeField] private GameObject elevator;

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
            var script = elevator.GetComponent<ElevatorController>();
            if (script != null)
            {
                // ПРАВИЛЬНЫЙ ЗАПУСК КОРУТИНЫ
                StartCoroutine(script.ActivateElevator());

            }
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
            other.transform.SetParent(this.elevator.transform);
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
            other.transform.SetParent(null);
        }
    }
}