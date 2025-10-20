using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;           // Your Martian enemy prefab
    public int maxEnemies = 10;              // Max enemies allowed at once

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;          // Assign in Inspector
    public float spawnIntervalMin = 3f;      // Minimum time between spawns
    public float spawnIntervalMax = 7f;      // Maximum time between spawns

    private int currentEnemies = 0;          // Track active enemies

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        // Keep spawning indefinitely
        while (true)
        {
            // Wait a random time between spawns
            float waitTime = Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(waitTime);

            // Only spawn if under the limit
            if (currentEnemies < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    void SpawnEnemy()
    {
        // Pick a random spawn point
        int index = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[index];

        // Instantiate the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        // Track enemy count
        currentEnemies++;

        // Decrease count when enemy is destroyed
        newEnemy.AddComponent<EnemyTracker>().Init(this);
    }

    public void OnEnemyDestroyed()
    {
        currentEnemies--;
    }
}

// Helper component for tracking when enemies die
public class EnemyTracker : MonoBehaviour
{
    private EnemySpawner spawner;

    public void Init(EnemySpawner spawner)
    {
        this.spawner = spawner;
    }

    void OnDestroy()
    {
        if (spawner != null)
            spawner.OnEnemyDestroyed();
    }
}