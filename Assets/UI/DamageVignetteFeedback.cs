using UnityEngine;
using UnityEngine.UI;

public class DamageVignetteFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image vignetteImage;
    [Tooltip("Enemy spawner used to detect when a wave is cleared so death UI can hide and health can reset.")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Flash Settings")]
    [Tooltip("Base alpha added for the first hit; subsequent hits are scaled exponentially.")]
    [SerializeField] private float baseAlphaIncrease = 0.1f;
    [Tooltip("Multiplier applied per hit to make alpha ramp faster ( > 1 for growth, 1 for linear ).")]
    [SerializeField] private float alphaGrowthFactor = 1.4f;
    [Tooltip("Maximum alpha the vignette can reach.")]
    [SerializeField] private float maxAlpha = 0.85f;

    [Header("Canvas Switching")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Canvas deathCanvas;

    [Header("Wave Clear Detection")]
    [Tooltip("If true, will poll the scene for remaining enemies when the player is dead. This is useful on non-host clients where the spawner may not issue wave events.")]
    [SerializeField] private bool useLocalEnemyPollFallback = true;
    [Tooltip("Seconds between enemy polls while dead.")]
    [SerializeField] private float enemyPollInterval = 0.35f;

    private float currentAlpha;
    private bool waitingForWaveClear;
    private float nextEnemyPollTime;

    private void Awake()
    {
        if (mainCanvas == null)
        {
            mainCanvas = GetComponentInParent<Canvas>();
        }

        if (deathCanvas != null)
        {
            deathCanvas.gameObject.SetActive(false);
        }

        if (vignetteImage != null)
        {
            currentAlpha = vignetteImage.color.a;
            SetImageAlpha(currentAlpha);
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged += HandleDamaged;
            playerHealth.OnDeath += HandleDeath;
        }

        if (enemySpawner != null)
        {
            enemySpawner.OnWaveCleared += HandleWaveCleared;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDeath -= HandleDeath;
        }

        if (enemySpawner != null)
        {
            enemySpawner.OnWaveCleared -= HandleWaveCleared;
        }
    }

    private void HandleDamaged(int remainingHearts, int heartsLostThisHit)
    {
        if (vignetteImage == null || playerHealth == null)
            return;

        int hitsTaken = Mathf.Max(0, playerHealth.MaxHearts - remainingHearts);
        int heartsLost = Mathf.Max(1, heartsLostThisHit);
        float addedAlpha = 0f;

        int firstHitIndex = Mathf.Max(1, hitsTaken - heartsLost + 1);
        int lastHitIndex = hitsTaken;

        for (int hitNumber = firstHitIndex; hitNumber <= lastHitIndex; hitNumber++)
        {
            float growthPower = Mathf.Pow(alphaGrowthFactor, Mathf.Max(0, hitNumber - 1));
            addedAlpha += baseAlphaIncrease * growthPower;
        }

        currentAlpha = Mathf.Min(maxAlpha, currentAlpha + addedAlpha);
        SetImageAlpha(currentAlpha);
    }

    private void HandleDeath()
    {
        currentAlpha = maxAlpha;
        SetImageAlpha(currentAlpha);
        waitingForWaveClear = true;
        nextEnemyPollTime = Time.time + Mathf.Max(0f, enemyPollInterval * 0.5f);

        if (deathCanvas != null)
        {
            deathCanvas.gameObject.SetActive(true);
            deathCanvas.enabled = true;
        }

        if (mainCanvas != null && deathCanvas != mainCanvas)
        {
            mainCanvas.enabled = false;
            mainCanvas.gameObject.SetActive(false);
        }
    }

    private void HandleWaveCleared(int waveIndex)
    {
        TryHandleWaveClear();
    }

    private void Update()
    {
        if (!waitingForWaveClear || !useLocalEnemyPollFallback)
        {
            return;
        }

        if (Time.time < nextEnemyPollTime)
        {
            return;
        }

        nextEnemyPollTime = Time.time + Mathf.Max(0.05f, enemyPollInterval);
        if (!AnyAliveEnemies())
        {
            TryHandleWaveClear();
        }
    }

    private void TryHandleWaveClear()
    {
        if (!waitingForWaveClear)
        {
            return;
        }

        waitingForWaveClear = false;

        if (deathCanvas != null)
        {
            deathCanvas.enabled = false;
            deathCanvas.gameObject.SetActive(false);
        }

        if (mainCanvas != null)
        {
            mainCanvas.enabled = true;
            mainCanvas.gameObject.SetActive(true);
        }

        if (playerHealth != null)
        {
            playerHealth.ResetHealthToFull();
        }

        currentAlpha = 0f;
        SetImageAlpha(currentAlpha);
    }

    private void SetImageAlpha(float alpha)
    {
        if (vignetteImage == null)
            return;

        Color color = vignetteImage.color;
        color.a = Mathf.Clamp01(alpha);
        vignetteImage.color = color;
    }

    private bool AnyAliveEnemies()
    {
        var enemies = FindObjectsOfType<EnemyChase>();
        if (enemies == null || enemies.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyChase enemy = enemies[i];
            if (enemy != null && enemy.isActiveAndEnabled && !enemy.IsKilled)
            {
                return true;
            }
        }

        return false;
    }
}
