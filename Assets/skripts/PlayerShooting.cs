using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Настройки выстрела")]
    public GameObject bulletPrefab;
    public Transform firePoint; // Пустой объект у дула оружия
    public float bulletSpeed = 20f;

    // Эта функция вызывается событием из анимации
    public void ShootEvent()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // Создаем пулю
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Если у пули есть Rigidbody, даем ей импульс вперед
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Используем firePoint.forward, чтобы пуля летела туда, куда смотрит ствол
            rb.velocity = firePoint.forward * bulletSpeed;
        }
    }
}