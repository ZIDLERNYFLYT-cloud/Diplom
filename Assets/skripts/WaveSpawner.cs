using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("=== Настройки волн ===")]
    public List<Wave> waves = new List<Wave>();
    public float delayBetweenWaves = 2f;

    [Header("=== Движение объекта после волн ===")]
    [Tooltip("Точки пути (waypoints)")]
    public Transform[] waypoints;

    [Tooltip("Скорость движения")]
    public float moveSpeed = 5f;

    [Tooltip("Двигаться по кругу?")]
    public bool loopMovement = true;

    [Tooltip("Начинать движение сразу при старте? (лучше выключить)")]
    public bool startMovementOnAwake = false;

    // Внутренние переменные
    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;
    private bool isSpawning = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool isStarted = false;

    private int currentWaypointIndex = 0;
    private bool isMoving = false;

    void Start()
    {
        if (startMovementOnAwake)
            StartMovement();
    }

    void Update()
    {
        if (isMoving)
            MoveTowardsTarget();
    }

    // ====================== ДВИЖЕНИЕ ======================
    private void MoveTowardsTarget()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 target = waypoints[currentWaypointIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            SetNextWaypoint();
        }
    }

    private void SetNextWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            if (loopMovement)
                currentWaypointIndex = 0;
            else
                StopMovement();
        }
    }

    public void StartMovement()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        isMoving = true;
        Debug.Log("WaveSpawner: Движение началось");
    }

    public void StopMovement()
    {
        isMoving = false;
        Debug.Log("WaveSpawner: Движение остановлено");
    }

    // ====================== ВОЛНЫ ======================
    public void StartWaves()
    {
        if (isStarted || waves.Count == 0) return;

        isStarted = true;
        StartCoroutine(SpawnWavesCoroutine());
    }

    IEnumerator SpawnWavesCoroutine()
    {
        isSpawning = true;

        for (int i = 0; i < waves.Count; i++)
        {
            currentWaveIndex = i;
            Debug.Log($"Начинается волна {i + 1}: {waves[i].waveName}");

            yield return StartCoroutine(SpawnWave(waves[i]));
            yield return StartCoroutine(WaitForAllEnemiesToDie());

            Debug.Log($"Волна {i + 1} завершена!");

            if (i < waves.Count - 1)
                yield return new WaitForSeconds(delayBetweenWaves);
        }

        isSpawning = false;
        Debug.Log("=== Все волны завершены! ===");

        // Запускаем движение объекта после всех волн
        StartMovement();
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (wave.spawnPoints == null || wave.spawnPoints.Count == 0)
        {
            yield break;
        }

        foreach (SpawnPoint sp in wave.spawnPoints)
        {
            if (sp.enemyToSpawn != null && sp.position != null)
            {
                GameObject enemy = Instantiate(sp.enemyToSpawn, sp.position.position, sp.position.rotation);
                spawnedEnemies.Add(enemy);
                enemiesRemaining++;

                // Подписка на смерть врага
                HealthEnemy health = enemy.GetComponent<HealthEnemy>();
                if (health != null)
                    health.OnDeath += OnEnemyDeath;
                else
                    enemiesRemaining--; // если нет компонента
            }
            yield return new WaitForSeconds(wave.delayBetweenSpawns);
        }
    }

    IEnumerator WaitForAllEnemiesToDie()
    {
        while (enemiesRemaining > 0)
        {
            spawnedEnemies.RemoveAll(e => e == null);
            yield return new WaitForSeconds(0.5f);
        }
    }

    void OnEnemyDeath()
    {
        enemiesRemaining--;
    }

    // ====================== Вспомогательные методы ======================
    public void ResetWaves()
    {
        StopAllCoroutines();
        foreach (var enemy in spawnedEnemies)
            if (enemy != null) Destroy(enemy);

        spawnedEnemies.Clear();
        enemiesRemaining = 0;
        isSpawning = false;
        isStarted = false;
        StopMovement();
    }
}

// ====================== Классы для инспектора ======================
[System.Serializable]
public class Wave
{
    public string waveName = "Волна";
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public float delayBetweenSpawns = 0.5f;
}

[System.Serializable]
public class SpawnPoint
{
    public Transform position;
    public GameObject enemyToSpawn;
}