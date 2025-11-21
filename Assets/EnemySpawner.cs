using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    [Tooltip("Optional set of prefab variants. When populated, the spawner randomly selects one of these per spawn. Entries left empty are ignored.")]
    public GameObject[] enemyPrefabs;
    public int maxEnemies = 10;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 7f;

    [System.Serializable]
    public class LifecycleSettings
    {
        [Tooltip("Seconds to wait before destroying or respawning after a kill.")]
        public float postKillDelay = 5f;
        [Tooltip("Seconds to ignore kill collisions after spawning or respawning.")]
        public float killGraceTime = 0.2f;
        [Tooltip("Destroy killed enemies instead of leaving them in the scene.")]
        public bool destroyOnKill = true;
        [Tooltip("Respawn enemies instead of destroying them after the post-kill delay.")]
        public bool respawnOnKill = false;
    }

    [System.Serializable]
    public struct WaveSettings
    {
        [Tooltip("Total enemies spawned during this wave.")]
        public int enemyCount;
        [Tooltip("Minimum seconds between spawns for this wave.")]
        public float spawnIntervalMin;
        [Tooltip("Maximum seconds between spawns for this wave.")]
        public float spawnIntervalMax;
        [Tooltip("Canvas or GameObject to enable while this wave is active.")]
        public GameObject waveCanvas;
        [Tooltip("Seconds to keep the wave canvas visible before fading out. Leave 0 to use the default display duration.")]
        public float displayDuration;
    }

    [Header("Lifecycle Settings")]
    public LifecycleSettings lifecycle = new LifecycleSettings();

    [Header("Wave Settings")]
    public WaveSettings[] waves = new WaveSettings[5];
    [Tooltip("Seconds to wait after showing the first wave canvas before spawning begins.")]
    public float initialWaveDelay = 0f;
    [Tooltip("Seconds to wait after showing the next wave canvas before its spawns begin.")]
    public float betweenWaveDelay = 2f;
    [Header("Wave UI Settings")]
    [Tooltip("Seconds to blend the wave canvas alpha in from 0 to 1.")]
    public float waveCanvasFadeInDuration = 0.5f;
    [Tooltip("Default time to keep the wave canvas visible before fading out.")]
    public float defaultWaveCanvasDisplayDuration = 2f;
    [Tooltip("Seconds to blend the wave canvas alpha from 1 back down to 0.")]
    public float waveCanvasFadeOutDuration = 0.5f;
    [Header("Activation")]
    [Tooltip("If enabled, spawning begins automatically in Start(). Otherwise call StartSpawning() manually (e.g., from a UI Button).")]
    [SerializeField] private bool autoStart = true;

    [Header("Audio")]
    [Tooltip("Optional audio source to play when a wave begins.")]
    [SerializeField] private AudioSource roundStartAudioSource;
    [Tooltip("Clip used for the round start audio. Leave empty to use the audio source's assigned clip.")]
    [SerializeField] private AudioClip roundStartClip;

    public event Action<int> OnWaveCleared;

    private readonly Dictionary<EnemyChase, EnemyState> enemyStates = new Dictionary<EnemyChase, EnemyState>();
    private Coroutine mainRoutine;
    private int currentEnemies;
    private bool useWaveMode;
    private int currentWaveIndex = -1;
    private int spawnedThisWave;
    private int defeatedThisWave;
    private int currentWaveTargetCount;
    private GameObject activeWaveCanvas;
    private CanvasGroup activeWaveCanvasGroup;
    private Coroutine activeCanvasRoutine;
    private bool initializationAttempted;
    private bool initializationSucceeded;
    private NetworkRunner cachedRunner;
    private bool warnedMissingRunner;

    private class EnemyState
    {
        public Transform spawnPoint;
        public Coroutine postKillRoutine;
    }

    private void Awake()
    {
        HideAllWaveCanvasesAtStartup();
    }

    private void Start()
    {
        if (autoStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} cannot start spawning while disabled.", this);
            return;
        }

        if (!EnsureInitialized())
        {
            return;
        }

        if (mainRoutine == null)
        {
            mainRoutine = StartCoroutine(useWaveMode ? RunWaveMode() : SpawnLoopMode());
        }
    }

    private bool HasWaveConfiguration()
    {
        if (waves == null || waves.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].enemyCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        if (mainRoutine != null)
        {
            StopCoroutine(mainRoutine);
            mainRoutine = null;
        }

        foreach (var entry in enemyStates)
        {
            if (entry.Value.postKillRoutine != null)
            {
                StopCoroutine(entry.Value.postKillRoutine);
            }

            entry.Key.AssignSpawner(null);
        }

        enemyStates.Clear();
        currentEnemies = 0;
        currentWaveIndex = -1;
        spawnedThisWave = 0;
        defeatedThisWave = 0;
        currentWaveTargetCount = 0;
        StopActiveCanvasRoutine();
        HideActiveWaveCanvasImmediate();
        initializationAttempted = false;
        initializationSucceeded = false;
        useWaveMode = false;
    }

    private void HideAllWaveCanvasesAtStartup()
    {
        if (waves == null || waves.Length == 0)
        {
            return;
        }

        for (int i = 0; i < waves.Length; i++)
        {
            HideCanvasImmediate(waves[i].waveCanvas);
        }
    }

    private IEnumerator SpawnLoopMode()
    {
        while (true)
        {
            float waitTime = GetNextSpawnDelay();
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                yield return null;
            }

            if (!isActiveAndEnabled)
            {
                yield break;
            }

            if (!CanSpawnEnemies())
            {
                continue;
            }

            if (currentEnemies >= maxEnemies)
            {
                continue;
            }

            TrySpawnEnemy();
        }
    }

    private IEnumerator RunWaveMode()
    {
        for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            WaveSettings wave = waves[waveIndex];
            if (wave.enemyCount <= 0)
            {
                continue;
            }

            currentWaveIndex = waveIndex;
            currentWaveTargetCount = Mathf.Max(0, wave.enemyCount);
            spawnedThisWave = 0;
            defeatedThisWave = 0;

            float preSpawnDelay = waveIndex == 0 ? Mathf.Max(0f, initialWaveDelay) : Mathf.Max(0f, betweenWaveDelay);
            if (preSpawnDelay > 0f)
            {
                yield return new WaitForSeconds(preSpawnDelay);
            }

            StopActiveCanvasRoutine();
            HideActiveWaveCanvasImmediate();
            if (wave.waveCanvas != null)
            {
                activeCanvasRoutine = StartCoroutine(ShowWaveCanvas(wave.waveCanvas, wave.displayDuration));
                if (activeCanvasRoutine != null)
                {
                    yield return activeCanvasRoutine;
                    activeCanvasRoutine = null;
                }
            }

            PlayRoundStartAudio();

            while (defeatedThisWave < currentWaveTargetCount)
            {
                if (!CanSpawnEnemies())
                {
                    yield return null;
                    continue;
                }

                if (spawnedThisWave < currentWaveTargetCount && currentEnemies < maxEnemies)
                {
                    if (TrySpawnEnemy())
                    {
                        spawnedThisWave++;
                        float wait = GetWaveSpawnDelay(wave);
                        if (wait > 0f)
                        {
                            yield return new WaitForSeconds(wait);
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{nameof(EnemySpawner)} on {name} failed to spawn an enemy for wave {waveIndex + 1}.", this);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
                else
                {
                    yield return null;
                }
            }

            OnWaveCleared?.Invoke(waveIndex);
            HideActiveWaveCanvasImmediate();
        }

        currentWaveIndex = -1;
    }

    private IEnumerator ShowWaveCanvas(GameObject canvasObject, float overrideDisplayDuration)
    {
        if (canvasObject == null)
        {
            yield break;
        }

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        }

        activeWaveCanvas = canvasObject;
        activeWaveCanvasGroup = canvasGroup;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;

        canvasObject.SetActive(true);

        if (waveCanvasFadeInDuration > 0f)
        {
            yield return FadeCanvas(canvasGroup, 0f, 1f, waveCanvasFadeInDuration);
        }
        else
        {
            canvasGroup.alpha = 1f;
        }

        float displayDuration = ResolveWaveDisplayDuration(overrideDisplayDuration);
        if (displayDuration > 0f)
        {
            yield return new WaitForSeconds(displayDuration);
        }

        if (waveCanvasFadeOutDuration > 0f)
        {
            yield return FadeCanvas(canvasGroup, 1f, 0f, waveCanvasFadeOutDuration);
        }
        else
        {
            canvasGroup.alpha = 0f;
        }

        canvasObject.SetActive(false);
        activeWaveCanvas = null;
        activeWaveCanvasGroup = null;
    }

    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = endAlpha;
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    private float ResolveWaveDisplayDuration(float overrideDisplayDuration)
    {
        return overrideDisplayDuration > 0f
            ? overrideDisplayDuration
            : Mathf.Max(0f, defaultWaveCanvasDisplayDuration);
    }

    private void HideActiveWaveCanvasImmediate()
    {
        if (activeWaveCanvas == null)
        {
            return;
        }

        if (activeWaveCanvasGroup != null)
        {
            activeWaveCanvasGroup.alpha = 0f;
        }

        HideCanvasImmediate(activeWaveCanvas);
        activeWaveCanvas = null;
        activeWaveCanvasGroup = null;
    }

    private void StopActiveCanvasRoutine()
    {
        if (activeCanvasRoutine != null)
        {
            StopCoroutine(activeCanvasRoutine);
            activeCanvasRoutine = null;
        }
    }

    private static void HideCanvasImmediate(GameObject canvasObject)
    {
        if (canvasObject == null)
        {
            return;
        }

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasObject.SetActive(false);
    }

    private bool TrySpawnEnemy()
    {
        Transform spawnPoint = GetSpawnPoint();
        if (spawnPoint == null)
        {
            return false;
        }

        GameObject prefab = GetEnemyPrefabForSpawn();
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} skipped spawning because all configured prefabs are missing.", this);
            return false;
        }

        GameObject newEnemy = SpawnEnemyInstance(prefab, spawnPoint.position, spawnPoint.rotation);
        if (newEnemy == null)
        {
            return false;
        }

        currentEnemies++;
        RegisterSpawnedEnemy(newEnemy, spawnPoint);
        return true;
    }

    private GameObject SpawnEnemyInstance(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        NetworkObject networkPrefab = prefab.GetComponent<NetworkObject>();
        if (networkPrefab != null)
        {
            NetworkRunner runner = GetActiveRunner();
            if (runner == null)
            {
                if (!warnedMissingRunner)
                {
                    Debug.LogWarning($"{nameof(EnemySpawner)} on {name} is configured with network enemies but no active {nameof(NetworkRunner)} was found. Waiting until a runner starts before spawning.", this);
                    warnedMissingRunner = true;
                }
                return null;
            }

            if (!HasSpawnAuthority(runner))
            {
                return null;
            }

            warnedMissingRunner = false;
            NetworkObject spawned = runner.Spawn(networkPrefab, position, rotation);
            return spawned != null ? spawned.gameObject : null;
        }

        return Instantiate(prefab, position, rotation);
    }

    private void RegisterSpawnedEnemy(GameObject newEnemy, Transform spawnPoint)
    {
        EnemyChase chase = newEnemy.GetComponent<EnemyChase>();
        if (chase != null)
        {
            chase.AssignSpawner(this);
            chase.ResetForSpawn(spawnPoint.position, spawnPoint.rotation, lifecycle.killGraceTime);

            var state = new EnemyState
            {
                spawnPoint = spawnPoint,
                postKillRoutine = null
            };
            enemyStates[chase] = state;
        }
        else
        {
            var tracker = newEnemy.GetComponent<SpawnedEnemyTracker>();
            if (tracker == null)
            {
                tracker = newEnemy.AddComponent<SpawnedEnemyTracker>();
            }
            tracker.Init(this);
        }
    }

    private NetworkRunner GetActiveRunner()
    {
        if (cachedRunner != null && cachedRunner.IsRunning)
        {
            return cachedRunner;
        }

        NetworkRunner runnerInScene = NetworkRunner.GetRunnerForScene(gameObject.scene);
        if (runnerInScene != null && runnerInScene.IsRunning)
        {
            cachedRunner = runnerInScene;
            return cachedRunner;
        }

        var found = FindObjectOfType<NetworkRunner>();
        if (found != null && found.IsRunning)
        {
            cachedRunner = found;
            return cachedRunner;
        }

        return null;
    }

    private bool HasSpawnAuthority(NetworkRunner runner)
    {
        return runner != null && (runner.IsServer || runner.IsSharedModeMasterClient);
    }

    private bool CanSpawnEnemies()
    {
        NetworkRunner runner = GetActiveRunner();
        if (runner == null)
        {
            // If any configured prefab is networked, wait for a runner before spawning.
            return !HasNetworkedEnemyPrefabs();
        }

        return HasSpawnAuthority(runner);
    }

    private bool HasNetworkedEnemyPrefabs()
    {
        if (enemyPrefab != null && enemyPrefab.GetComponent<NetworkObject>() != null)
        {
            return true;
        }

        if (enemyPrefabs != null)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                var candidate = enemyPrefabs[i];
                if (candidate != null && candidate.GetComponent<NetworkObject>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private float GetNextSpawnDelay()
    {
        float min = Mathf.Min(spawnIntervalMin, spawnIntervalMax);
        float max = Mathf.Max(spawnIntervalMin, spawnIntervalMax);
        return Random.Range(min, max);
    }

    private bool HasAnyEnemyPrefab()
    {
        if (enemyPrefab != null)
        {
            return true;
        }

        if (enemyPrefabs != null)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private GameObject GetEnemyPrefabForSpawn()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            // Try a few random attempts first to increase variety.
            int attempts = Mathf.Min(enemyPrefabs.Length, 5);
            for (int i = 0; i < attempts; i++)
            {
                int index = Random.Range(0, enemyPrefabs.Length);
                GameObject variant = enemyPrefabs[index];
                if (variant != null)
                {
                    return variant;
                }
            }

            // Fallback to the first non-null entry if the random attempts hit empty slots.
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    return enemyPrefabs[i];
                }
            }
        }

        return enemyPrefab;
    }

    private void PlayRoundStartAudio()
    {
        if (roundStartAudioSource == null && roundStartClip == null)
        {
            return;
        }

        AudioSource source = roundStartAudioSource;
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        if (source == null)
        {
            return;
        }

        AudioClip clip = roundStartClip != null ? roundStartClip : source.clip;
        if (clip == null)
        {
            return;
        }

        source.PlayOneShot(clip);
    }

    private float GetWaveSpawnDelay(WaveSettings wave)
    {
        float min = Mathf.Min(wave.spawnIntervalMin, wave.spawnIntervalMax);
        float max = Mathf.Max(wave.spawnIntervalMin, wave.spawnIntervalMax);
        return Random.Range(min, max);
    }

    private bool EnsureInitialized()
    {
        if (initializationAttempted)
        {
            return initializationSucceeded;
        }

        initializationAttempted = true;

        if (!HasAnyEnemyPrefab())
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has no enemy prefabs assigned.", this);
            initializationSucceeded = false;
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has no spawn points configured.", this);
            initializationSucceeded = false;
            return false;
        }

        useWaveMode = HasWaveConfiguration();

        if (useWaveMode && lifecycle.respawnOnKill)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} is running in wave mode, which ignores respawnOnKill. Disabling respawns for waves.", this);
            lifecycle.respawnOnKill = false;
        }

        initializationSucceeded = true;
        return true;
    }

    internal void HandleEnemyKilled(EnemyChase enemy)
    {
        if (!enemyStates.TryGetValue(enemy, out var state))
        {
            return;
        }

        if (state.postKillRoutine != null)
        {
            return;
        }

        state.postKillRoutine = StartCoroutine(HandlePostKill(enemy, state));
    }

    private IEnumerator HandlePostKill(EnemyChase enemy, EnemyState state)
    {
        float wait = Mathf.Max(0f, lifecycle.postKillDelay);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }

        state.postKillRoutine = null;

        if (useWaveMode)
        {
            bool removed = RemoveEnemy(enemy, countForWave: true);
            if (removed && lifecycle.destroyOnKill)
            {
                DespawnOrDestroy(enemy);
            }
        }
        else
        {
            if (lifecycle.respawnOnKill)
            {
                Transform spawnPoint = state.spawnPoint != null ? state.spawnPoint : GetSpawnPoint();
                if (spawnPoint == null)
                {
                    spawnPoint = transform;
                }

                state.spawnPoint = spawnPoint;
                enemy.ResetForSpawn(spawnPoint.position, spawnPoint.rotation, lifecycle.killGraceTime);
            }
            else
            {
                bool removed = RemoveEnemy(enemy, countForWave: false);
                if (removed && lifecycle.destroyOnKill)
                {
                    DespawnOrDestroy(enemy);
                }
            }
        }
    }

    internal void NotifyEnemyDestroyed(EnemyChase enemy)
    {
        RemoveEnemy(enemy, countForWave: useWaveMode);
    }

    private bool RemoveEnemy(EnemyChase enemy, bool countForWave)
    {
        if (!enemyStates.TryGetValue(enemy, out var state))
        {
            return false;
        }

        Coroutine routine = state.postKillRoutine;
        enemyStates.Remove(enemy);

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        currentEnemies = Mathf.Max(0, currentEnemies - 1);
        enemy.AssignSpawner(null);

        if (countForWave && useWaveMode)
        {
            defeatedThisWave = Mathf.Min(defeatedThisWave + 1, currentWaveTargetCount);
        }

        return true;
    }

    private void HandleUntrackedEnemyDestroyed()
    {
        currentEnemies = Mathf.Max(0, currentEnemies - 1);

        if (useWaveMode)
        {
            defeatedThisWave = Mathf.Min(defeatedThisWave + 1, currentWaveTargetCount);
        }
    }

    private void DespawnOrDestroy(EnemyChase enemy)
    {
        if (enemy == null)
        {
            return;
        }

        GameObject target = enemy.gameObject;
        NetworkObject networkObject = target.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            NetworkRunner runner = networkObject.Runner != null && networkObject.Runner.IsRunning
                ? networkObject.Runner
                : GetActiveRunner();
            if (runner != null && HasSpawnAuthority(runner))
            {
                runner.Despawn(networkObject);
                return;
            }

            // Do not locally destroy a networked object if we do not own it.
            return;
        }

        Destroy(target);
    }

    private class SpawnedEnemyTracker : MonoBehaviour
    {
        private EnemySpawner owner;

        public void Init(EnemySpawner spawner)
        {
            owner = spawner;
        }

        private void OnDestroy()
        {
            if (owner != null)
            {
                owner.HandleUntrackedEnemyDestroyed();
            }
        }
    }
}
