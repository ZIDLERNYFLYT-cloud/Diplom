using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 20;
    [SerializeField] private GameObject hitEffect;

    private bool hasHit = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // CharacterController обычно реагирует на OnCollisionEnter, 
    // но если пуля очень быстрая, лучше использовать OnTriggerEnter
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Игнорируем игрока
        if (other.CompareTag("Player")) return;

        // 1. Ищем скрипт врага (в самом объекте или у родителя)
        EnemyAI enemy = other.GetComponent<EnemyAI>() ?? other.GetComponentInParent<EnemyAI>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            hasHit = true;
            SpawnHitEffect(transform.position, -transform.forward);
            Destroy(gameObject);
        }
        // 2. Если попали в стену или другой статический объект
        else if (!other.isTrigger)
        {
            SpawnHitEffect(transform.position, -transform.forward);
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, point, Quaternion.LookRotation(normal));
            Destroy(effect, 1f);
        }
    }
}