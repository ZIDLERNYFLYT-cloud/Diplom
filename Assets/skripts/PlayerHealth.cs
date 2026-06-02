using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Характеристики")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Ссылки")]
    public Animator anim;
    public AudioSource audioSource;
    public AudioClip hurtSound;

    [Header("UI - Image полоска здоровья")]
    public Image healthBarImage; // Image для полоски здоровья (Type = Filled)
    public float smoothHealthChange = 5f; // Плавное изменение полоски
    public bool useSmoothHealth = true; // Использовать плавное изменение

    [Header("UI - Экран смерти")]
    public GameObject deathScreen;
    public float deathScreenDelay = 1f;

    [Header("Возрождение")]
    public RespawnSystem respawnSystem;
    public bool respawnInsteadOfRestart = true;

    private bool isDead = false;
    private float targetFillAmount; // Целевое значение заполнения
    private PlayerSideController moveScript;

    void Start()
    {
        currentHealth = maxHealth;
        moveScript = GetComponent<PlayerSideController>();

        if (anim == null) anim = GetComponent<Animator>();

        if (respawnSystem == null && respawnInsteadOfRestart)
        {
            respawnSystem = FindObjectOfType<RespawnSystem>();
        }

        // Настройка Image полоски здоровья
        if (healthBarImage != null)
        {
            // Убеждаемся что Image Type = Filled
            if (healthBarImage.type != Image.Type.Filled)
            {
                Debug.LogWarning("Health bar Image type is not set to Filled! Please set Image Type to 'Filled' in the inspector.");
                healthBarImage.type = Image.Type.Filled;
            }

            healthBarImage.fillMethod = Image.FillMethod.Horizontal;
            healthBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            targetFillAmount = 1f;
            healthBarImage.fillAmount = 1f;
            Debug.Log($"Health bar initialized: fillAmount = {healthBarImage.fillAmount}");
        }
        else
        {
            Debug.LogError("Health Bar Image is not assigned in PlayerHealth!");
        }

        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Плавное обновление полоски здоровья
        if (useSmoothHealth && healthBarImage != null && !isDead)
        {
            if (Mathf.Abs(healthBarImage.fillAmount - targetFillAmount) > 0.01f)
            {
                healthBarImage.fillAmount = Mathf.Lerp(healthBarImage.fillAmount, targetFillAmount, smoothHealthChange * Time.deltaTime);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! HP: {currentHealth}/{maxHealth}");

        // Обновление полоски здоровья
        UpdateHealthBar();

        if (audioSource && hurtSound)
            audioSource.PlayOneShot(hurtSound);

        if (currentHealth <= 0) Die();
        else PlayHurtAnimation();
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Player healed {amount}! HP: {currentHealth}/{maxHealth}");

        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            targetFillAmount = healthPercentage;

            if (!useSmoothHealth)
            {
                healthBarImage.fillAmount = healthPercentage;
            }

            Debug.Log($"Health bar updated: {healthPercentage * 100}% (fillAmount: {targetFillAmount})");
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        UpdateHealthBar();

        if (anim != null)
        {
            anim.Rebind();
            anim.ResetTrigger("die");
            anim.ResetTrigger("getHit");
        }

        if (moveScript != null)
        {
            moveScript.enabled = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
        }

        Debug.Log($"Player health reset to {currentHealth}/{maxHealth}");
    }

    public void RespawnAtLastCheckpoint()
    {
        Debug.Log("RespawnAtLastCheckpoint called!");

        if (respawnSystem != null)
        {
            if (deathScreen != null)
            {
                deathScreen.SetActive(false);
            }

            Time.timeScale = 1f;
            respawnSystem.RespawnPlayer(this);
        }
        else
        {
            Debug.LogError("RespawnSystem not found! Using restart instead.");
            RestartGame();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player died!");

        if (moveScript != null) moveScript.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.Move(Vector3.zero);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (anim != null)
        {
            anim.SetInteger("deathType", Random.Range(1, 3));
            anim.ResetTrigger("getHit");
            anim.SetTrigger("die");
        }

        if (deathScreen != null)
        {
            Invoke("ShowDeathScreen", deathScreenDelay);
        }
        else
        {
            Debug.LogWarning("Death screen not assigned!");
        }
    }

    void ShowDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);

            Button respawnButton = deathScreen.GetComponentInChildren<Button>();
            if (respawnButton != null)
            {
                respawnButton.onClick.RemoveAllListeners();
                respawnButton.onClick.AddListener(RespawnAtLastCheckpoint);
                Debug.Log("Respawn button connected!");
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void PlayHurtAnimation()
    {
        if (anim != null)
        {
            int randomHurt = Random.Range(1, 4);
            anim.SetInteger("hurtType", randomHurt);
            anim.SetTrigger("getHit");
        }
    }
}