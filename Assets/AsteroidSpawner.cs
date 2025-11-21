using System.Collections;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid")]
    [SerializeField] private AsteroidImpact asteroidPrefab;
    [SerializeField] private ParticleSystem impactEffectPrefab;
    [SerializeField] private float minAsteroidSpeed = 35f;
    [SerializeField] private float maxAsteroidSpeed = 60f;
    [SerializeField] private float asteroidLifetime = 12f;

    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnDelay = 3f;
    [SerializeField] private float maxSpawnDelay = 6f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float spawnHeight = 80f;

    [Header("Trajectory")]
    [SerializeField, Range(0f, 89f)] private float minAngleFromVertical = 10f;
    [SerializeField, Range(0f, 89f)] private float maxAngleFromVertical = 35f;
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

    [Header("Gizmos")]
    [SerializeField] private Color spawnAreaGizmoColor = new Color(1f, 0.5f, 0f, 0.15f);

    private Coroutine spawnRoutine;
    private readonly System.Collections.Generic.Queue<AsteroidImpact> availableAsteroids = new System.Collections.Generic.Queue<AsteroidImpact>();

    private void OnEnable()
    {
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            float waitTime = Random.Range(minSpawnDelay, maxSpawnDelay);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                yield return null;
            }

            SpawnAsteroid();
        }
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefab == null)
        {
            
            return;
        }

        Vector2 planarOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(planarOffset.x, spawnHeight, planarOffset.y);

        float angle = Random.Range(minAngleFromVertical, maxAngleFromVertical);
        float azimuth = Random.Range(0f, 360f);
        Vector3 direction = BuildDirection(angle, azimuth);

        float speed = Random.Range(minAsteroidSpeed, maxAsteroidSpeed);
        if (speed <= 0f)
        {
            speed = minAsteroidSpeed > 0f ? minAsteroidSpeed : 1f;
        }

        AsteroidImpact asteroidInstance = GetAsteroidInstance();
        asteroidInstance.transform.position = spawnPosition;
        asteroidInstance.gameObject.SetActive(true);
        asteroidInstance.Initialize(direction, speed, impactEffectPrefab, groundLayers, asteroidLifetime);
    }

    private AsteroidImpact GetAsteroidInstance()
    {
        AsteroidImpact instance;
        if (availableAsteroids.Count > 0)
        {
            instance = availableAsteroids.Dequeue();
        }
        else
        {
            instance = Instantiate(asteroidPrefab, transform.position, Quaternion.identity);
            instance.enabled = false;
            instance.gameObject.SetActive(false);
        }

        instance.AssignOwner(this);

        return instance;
    }

    public void NotifyAsteroidAvailable(AsteroidImpact asteroid)
    {
        if (asteroid == null)
            return;

        availableAsteroids.Enqueue(asteroid);
    }

    private static Vector3 BuildDirection(float angleFromVertical, float azimuthDegrees)
    {
        angleFromVertical = Mathf.Clamp(angleFromVertical, 0f, 89f);
        float angleRad = angleFromVertical * Mathf.Deg2Rad;
        float azimuthRad = azimuthDegrees * Mathf.Deg2Rad;

        float horizontalMagnitude = Mathf.Sin(angleRad);
        float y = -Mathf.Cos(angleRad);
        float x = Mathf.Cos(azimuthRad) * horizontalMagnitude;
        float z = Mathf.Sin(azimuthRad) * horizontalMagnitude;

        Vector3 direction = new Vector3(x, y, z);
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector3.down;
        }
        return direction.normalized;
    }

    private void OnValidate()
    {
        if (maxSpawnDelay < minSpawnDelay)
        {
            maxSpawnDelay = minSpawnDelay;
        }

        if (maxAsteroidSpeed < minAsteroidSpeed)
        {
            maxAsteroidSpeed = minAsteroidSpeed;
        }

        if (maxAngleFromVertical < minAngleFromVertical)
        {
            maxAngleFromVertical = minAngleFromVertical;
        }

        spawnHeight = Mathf.Max(1f, spawnHeight);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        minSpawnDelay = Mathf.Max(0f, minSpawnDelay);
        maxSpawnDelay = Mathf.Max(minSpawnDelay, maxSpawnDelay);
        minAsteroidSpeed = Mathf.Max(0.1f, minAsteroidSpeed);
        maxAsteroidSpeed = Mathf.Max(minAsteroidSpeed, maxAsteroidSpeed);
        asteroidLifetime = Mathf.Max(0.1f, asteroidLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = spawnAreaGizmoColor;
        Gizmos.DrawSphere(transform.position + Vector3.up * spawnHeight, 1f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
