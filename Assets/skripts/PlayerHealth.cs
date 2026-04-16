using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Характеристики")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Ссылки")]
    public Animator anim;
    public AudioSource audioSource;
    public AudioClip hurtSound;

    private bool isDead = false;
    // Используем конкретный тип твоего скрипта управления
    private PlayerSideController moveScript;

    void Start()
    {
        currentHealth = maxHealth;

        // 1. ПРИСВАИВАЕМ ССЫЛКУ (без этого управление не отключится)
        moveScript = GetComponent<PlayerSideController>();

        if (anim == null) anim = GetComponent<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (audioSource && hurtSound)
            audioSource.PlayOneShot(hurtSound);

        if (currentHealth <= 0) Die();
        else PlayHurtAnimation();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

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

        anim.SetInteger("deathType", Random.Range(1, 3));
        anim.ResetTrigger("getHit");
        anim.SetTrigger("die");
    }

    void PlayHurtAnimation()
    {
        int randomHurt = Random.Range(1, 4);
        anim.SetInteger("hurtType", randomHurt);
        anim.SetTrigger("getHit");
    }
}