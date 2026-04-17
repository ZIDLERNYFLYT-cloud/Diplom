using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float lifeTime = 3f; // Сколько секунд живет пуля
    [SerializeField] private int damage = 20;     // Урон пули
    [SerializeField] private GameObject hitEffect; // Префаб эффекта попадания (искры/кровь)

    private void Start()
    {
        // Уничтожаем объект через заданное время, чтобы пули не летели вечно
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Проверяем, есть ли у объекта, в который попали, компонент здоровья или скрипт врага
        // Допустим, у ваших врагов есть метод TakeDamage
        if (collision.gameObject.TryGetComponent(out EnemyAI enemy))
        {
            enemy.TakeDamage(damage);
        }

        // 2. Создаем эффект попадания, если он назначен
        if (hitEffect != null)
        {
            // Спавним искры в точке контакта и разворачиваем их в сторону нормали (от поверхности)
            ContactPoint contact = collision.contacts[0];
            GameObject effect = Instantiate(hitEffect, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(effect, 1f); // Удаляем эффект через секунду
        }

        // 3. Уничтожаем саму пулю при столкновении
        Destroy(gameObject);
    }
}