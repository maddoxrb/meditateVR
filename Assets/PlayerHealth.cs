using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHearts = 5;

    [Header("Damage Window")]
    [Tooltip("How close an enemy needs to be before damage starts ticking.")]
    [SerializeField] private float damageRadius = 2.5f;
    [Tooltip("How often the player loses another heart while an enemy stays in range (seconds).")]
    [SerializeField] private float damageIntervalSeconds = 1f;

    [Header("Enemy Detection")]
    [Tooltip("Enemies on these layers will be considered damaging. Leave empty to match all layers.")]
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("Optional list of specific enemy objects that can hurt the player.")]
    [SerializeField] private GameObject[] explicitEnemyObjects;
    [Tooltip("Fallback name fragment if layers/explicit objects are not set. Case-insensitive.")]
    [SerializeField] private string enemyNameContains = "Martian";

    public event Action<int, int> OnDamaged;
    public event Action OnDeath;

    public int CurrentHearts => currentHearts;
    public int MaxHearts => maxHearts;

    private readonly Collider[] overlapBuffer = new Collider[32];

    private int currentHearts;
    private float timeNearEnemy;
    private bool isNearEnemy;

    private void Awake()
    {
        currentHearts = Mathf.Max(1, maxHearts);
        timeNearEnemy = 0f;
        isNearEnemy = false;
    }

    private void OnValidate()
    {
        maxHearts = Mathf.Max(1, maxHearts);
        damageRadius = Mathf.Max(0.05f, damageRadius);
        damageIntervalSeconds = Mathf.Max(0.05f, damageIntervalSeconds);
    }

    private void Update()
    {
        if (currentHearts <= 0)
            return;

        bool enemyNearby = IsEnemyWithinRadius();

        if (!enemyNearby)
        {
            isNearEnemy = false;
            timeNearEnemy = 0f;
            return;
        }

        if (!isNearEnemy)
        {
            isNearEnemy = true;
            timeNearEnemy = 0f;
            ApplyDamage(1);
            return;
        }

        timeNearEnemy += Time.deltaTime;
        if (timeNearEnemy >= damageIntervalSeconds)
        {
            timeNearEnemy -= damageIntervalSeconds;
            ApplyDamage(1);
        }
    }

    private bool IsEnemyWithinRadius()
    {
        float sqrRadius = damageRadius * damageRadius;

        foreach (var enemy in explicitEnemyObjects)
        {
            if (enemy == null)
                continue;

            Vector3 offset = enemy.transform.position - transform.position;
            if (offset.sqrMagnitude <= sqrRadius)
                return true;
        }

        int mask = enemyLayers.value == 0 ? ~0 : enemyLayers.value;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            damageRadius,
            overlapBuffer,
            mask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider candidate = overlapBuffer[i];
            if (candidate == null)
                continue;

            Transform other = candidate.transform;
            if (other == transform || other.IsChildOf(transform))
                continue;

            if (IsEnemyCollider(candidate))
                return true;
        }

        return false;
    }

    private bool IsEnemyCollider(Collider other)
    {
        if (other == null)
            return false;

        foreach (var enemy in explicitEnemyObjects)
        {
            if (enemy == null)
                continue;

            if (other.transform == enemy.transform || other.transform.IsChildOf(enemy.transform))
                return true;
        }

        if (enemyLayers.value != 0)
        {
            if (((1 << other.gameObject.layer) & enemyLayers.value) != 0)
                return true;
        }

        if (!string.IsNullOrEmpty(enemyNameContains))
        {
            if (other.name.IndexOf(enemyNameContains, StringComparison.OrdinalIgnoreCase) >= 0 ||
                other.transform.root.name.IndexOf(enemyNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyDamage(int amount)
    {
        if (amount <= 0 || currentHearts <= 0)
            return;

        int previousHearts = currentHearts;
        currentHearts = Mathf.Max(0, currentHearts - amount);

        if (currentHearts < previousHearts)
        {
            int heartsLostThisHit = previousHearts - currentHearts;
            OnDamaged?.Invoke(currentHearts, heartsLostThisHit);

            if (currentHearts == 0)
            {
                Debug.Log("Player died");
                OnDeath?.Invoke();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, Mathf.Max(0.05f, damageRadius));
    }
}
