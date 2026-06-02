using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveSpawner : MonoBehaviour
{
    [Header("Настройки волн")]
    public List<Wave> waves = new List<Wave>();
    public float delayBetweenWaves = 2f;

    [Header("События")]
    public UnityEvent onAllWavesComplete;
    public UnityEvent<int> onWaveStart;
    public UnityEvent<int> onWaveComplete;

    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;
    private bool isSpawning = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool isStarted = false;

    void Start()
    {
        // НИЧЕГО НЕ ДЕЛАЕМ ПРИ СТАРТЕ
        Debug.Log($"WaveSpawner на {gameObject.name}: Ожидание активации через WaveTrigger или вызов StartWaves()");
    }

    public void StartWaves()
    {
        if (isStarted)
        {
            Debug.LogWarning("WaveSpawner: Волны уже были запущены!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("WaveSpawner: Волны уже запущены!");
            return;
        }

        if (waves == null || waves.Count == 0)
        {
            Debug.LogError("WaveSpawner: Нет настроенных волн! Добавьте волны в инспекторе.");
            return;
        }

        Debug.Log("WaveSpawner: ВОЛНЫ АКТИВИРОВАНЫ!");
        isStarted = true;
        StartCoroutine(SpawnWavesCoroutine());
    }

    IEnumerator SpawnWavesCoroutine()
    {
        isSpawning = true;

        for (int i = 0; i < waves.Count; i++)
        {
            currentWaveIndex = i;
            onWaveStart?.Invoke(i);

            Debug.Log($"Начинается волна {i + 1}: {waves[i].waveName}");
            Debug.Log($"Количество точек спавна в волне: {waves[i].spawnPoints.Count}");

            // Спавним врагов волны
            yield return StartCoroutine(SpawnWave(waves[i]));

            // Ждем пока все враги не будут убиты
            yield return StartCoroutine(WaitForAllEnemiesToDie());

            onWaveComplete?.Invoke(i);
            Debug.Log($"Волна {i + 1} завершена!");

            // Задержка между волнами (кроме последней)
            if (i < waves.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }

        isSpawning = false;
        Debug.Log("Все волны завершены!");
        onAllWavesComplete?.Invoke();
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (wave.spawnPoints == null || wave.spawnPoints.Count == 0)
        {
            Debug.LogWarning($"Нет точек спавна для волны {wave.waveName}");
            yield break;
        }

        // Спавним врагов на каждой точке
        foreach (SpawnPoint spawnPoint in wave.spawnPoints)
        {
            if (spawnPoint.enemyToSpawn != null && spawnPoint.position != null)
            {
                GameObject enemy = Instantiate(spawnPoint.enemyToSpawn, spawnPoint.position.position, spawnPoint.position.rotation);
                spawnedEnemies.Add(enemy);
                enemiesRemaining++;

                Debug.Log($"Спавн [{wave.waveName}] {enemy.name} на точке {spawnPoint.position.name}");

                // Подписываемся на смерть врага
                HealthEnemy health = enemy.GetComponent<HealthEnemy>();
                if (health != null)
                {
                    health.OnDeath += OnEnemyDeath;
                }
                else
                {
                    EnemyAI3D enemyAI = enemy.GetComponent<EnemyAI3D>();
                    if (enemyAI != null)
                    {
                        StartCoroutine(TrackEnemyDeath(enemy));
                    }
                    else
                    {
                        enemiesRemaining--;
                        Debug.LogWarning($"У врага {enemy.name} нет компонента HealthEnemy!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Точка спавна {spawnPoint.position?.name} не настроена!");
            }

            yield return new WaitForSeconds(wave.delayBetweenSpawns);
        }

        Debug.Log($"Все враги волны {wave.waveName} заспавнены. Всего врагов: {enemiesRemaining}");
    }

    IEnumerator TrackEnemyDeath(GameObject enemy)
    {
        yield return new WaitUntil(() => enemy == null);
        OnEnemyDeath();
    }

    void OnEnemyDeath()
    {
        enemiesRemaining--;
        Debug.Log($"Враг убит. Осталось врагов: {enemiesRemaining}");
    }

    IEnumerator WaitForAllEnemiesToDie()
    {
        spawnedEnemies.RemoveAll(e => e == null);

        while (enemiesRemaining > 0 || spawnedEnemies.Count > 0)
        {
            spawnedEnemies.RemoveAll(e => e == null);

            if (enemiesRemaining > 0 || spawnedEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("Все враги волны уничтожены!");
    }

    public void SkipToNextWave()
    {
        if (!isSpawning) return;

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
        enemiesRemaining = 0;
    }

    public void ResetWaves()
    {
        StopAllCoroutines();

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
        enemiesRemaining = 0;
        isSpawning = false;
        currentWaveIndex = 0;
        isStarted = false;
    }

    public void AddWave()
    {
        waves.Add(new Wave());
    }

    public void RemoveWave(int index)
    {
        if (index >= 0 && index < waves.Count)
            waves.RemoveAt(index);
    }

    public bool IsSpawning()
    {
        return isSpawning;
    }

    public bool IsStarted()
    {
        return isStarted;
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }

    public int GetEnemiesRemaining()
    {
        return enemiesRemaining;
    }
}

[System.Serializable]
public class Wave
{
    [Header("Название волны")]
    public string waveName = "Новая волна";

    [Header("Точки спавна")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Настройки волны")]
    public float delayBetweenSpawns = 0.5f;

    public void AddSpawnPoint()
    {
        spawnPoints.Add(new SpawnPoint());
    }

    public void RemoveSpawnPoint(int index)
    {
        if (index >= 0 && index < spawnPoints.Count)
            spawnPoints.RemoveAt(index);
    }
}

[System.Serializable]
public class SpawnPoint
{
    [Header("Позиция спавна")]
    public Transform position;

    [Header("Объект для спавна")]
    public GameObject enemyToSpawn;
}