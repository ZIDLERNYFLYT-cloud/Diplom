using UnityEngine;
using System;
using System.Collections;

public class HealthEnemy : MonoBehaviour
{
    [Header("Настройки здоровья")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Ссылки")]
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("Плавный уход в землю")]
    public float sinkSpeed = 1.5f;        // Скорость опускания (единиц в секунду)
    public float sinkDelay = 0.5f;        // Задержка перед началом опускания (сек)
    public bool destroyAfterSink = true;  // Уничтожить объект после опускания?
    public float destroyDepth = -5f;      // На какой глубине уничтожить (если не отключать коллайдер, можно просто ждать)

    // События для других компонентов
    public event Action<int, int> OnDamageTaken;
    public event Action OnDeath;
    public event Action<float> OnHealthPercentChanged;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " получил урон: " + damage + ". Осталось здоровья: " + currentHealth);

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (animator != null && currentHealth > 0)
            animator.SetTrigger("gethit");

        OnDamageTaken?.Invoke(currentHealth, damage);
        OnHealthPercentChanged?.Invoke(GetHealthPercent());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " УМИРАЕТ!");

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (animator != null)
        {
            animator.SetTrigger("death");
            animator.SetBool("walk", false);
            animator.SetBool("run", false);
            animator.SetBool("idle1", false);
            animator.SetBool("idle2", false);
        }

        OnDeath?.Invoke();

        // Запускаем плавное опускание в землю
        StartCoroutine(SinkIntoGround());
    }

    IEnumerator SinkIntoGround()
    {
        // Отключаем коллайдер, чтобы враг не мешал физике
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Если есть NavMeshAgent — отключаем его, чтобы он не дёргал позицию
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // Дополнительно можно отключить Rigidbody, если есть
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Чтобы не падал под действием гравитации
        }

        // Ждём задержку (чтобы анимация смерти успела начаться)
        yield return new WaitForSeconds(sinkDelay);

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        // Опускаемся, пока не уйдём достаточно глубоко (или бесконечно, если destroyAfterSink = false)
        while (destroyAfterSink == false || transform.position.y > destroyDepth)
        {
            elapsed += Time.deltaTime * sinkSpeed;
            // Смещаем позицию вниз по Y
            Vector3 newPos = transform.position;
            newPos.y = startPos.y - elapsed;
            transform.position = newPos;

            // Если ушли ниже destroyDepth и нужно уничтожить — выходим из цикла
            if (destroyAfterSink && transform.position.y <= destroyDepth)
                break;

            yield return null;
        }

        // Уничтожаем объект (или можно просто отключить, если нужно переиспользовать)
        if (destroyAfterSink)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}