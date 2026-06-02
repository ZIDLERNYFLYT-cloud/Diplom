using UnityEngine;
using System;

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

    // События для других компонентов
    public event Action<int, int> OnDamageTaken; // (currentHealth, damage)
    public event Action OnDeath;
    public event Action<float> OnHealthPercentChanged; // (percent)

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " получил урон: " + damage + ". Осталось здоровья: " + currentHealth);

        // Звук получения урона
        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        // Анимация получения урона
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

        // Звук смерти
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        // Анимация смерти
        if (animator != null)
        {
            animator.SetTrigger("death");
            // Сбрасываем другие параметры анимации
            animator.SetBool("walk", false);
            animator.SetBool("run", false);
            animator.SetBool("idle1", false);
            animator.SetBool("idle2", false);
        }

        OnDeath?.Invoke();
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