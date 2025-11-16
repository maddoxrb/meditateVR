using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class EnemyChase : MonoBehaviour
{
    public float speed = 3.5f;
    [Tooltip("Distance at which the enemy switches from chase to attack animation.")]
    public float attackDistance = 1.5f;
    [Tooltip("Collision impulse magnitude that will kill the enemy.")]
    public float killImpulseThreshold = 5f;
    [Tooltip("Seconds to ignore kill collisions after spawning when no spawner overrides this value.")]
    [FormerlySerializedAs("spawnKillGraceTime")]
    [SerializeField] private float defaultKillGraceTime = 0.2f;
    [Tooltip("If enabled, only collisions from the specified GameObject can trigger a kill.")]
    public bool requireSpecificKiller = false;
    [Tooltip("GameObject required to trigger a kill when 'requireSpecificKiller' is true.")]                                public GameObject specificKiller;
    [Tooltip("Maximum distance to search above/below the player for a valid NavMesh point when the player is airborne.")]
    [SerializeField] private float navSampleDistance = 4f;
    [Tooltip("Optional layers considered ground when raycasting beneath the player as a fallback.")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Bullet Kill Settings")]
    [Tooltip("If enabled, the enemy dies after a set number of qualifying bullet collisions instead of by collision impulse.")]
    [SerializeField] private bool useBulletHitCount = false;
    [Tooltip("Number of qualifying bullet collisions required to kill the enemy.")]
    [Min(1)]
    [SerializeField] private int requiredBulletHits = 3;
    [Tooltip("Only these bullet prefabs count toward the hit count. Leave empty to allow any (subject to tags).")]
    [SerializeField] private GameObject[] qualifyingBulletPrefabs;
    [Tooltip("Additional tags that identify qualifying bullets. Leave empty to ignore tags.")]
    [SerializeField] private string[] qualifyingBulletTags;
    [Tooltip("If disabled, the same bullet instance will only ever count once even if it collides multiple times.")]
    [SerializeField] private bool countMultipleHitsFromSameInstance = true;

    private Transform player;
    private NavMeshAgent agent;
    [SerializeField] private Animator animator; // assign in inspector if animator is on a child
    private static readonly int IsChasingHash = Animator.StringToHash("isChasing");
    private static readonly int IsAttackingHash1 = Animator.StringToHash("isAttacking1");
    private static readonly int IsAttackingHash2 = Animator.StringToHash("isAttacking2");
    private static readonly int IsKilledHash = Animator.StringToHash("isKilled");
    private bool isKilled;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 lastDestination;
    private int currentAttackVariant = -1;
    private EnemySpawner spawner;
    private float killProtectionEndTime;
    private bool spawnStateInitialized;
    private bool initialized;
    [SerializeField] private AudioSource audioSource; // optional, falls back to local AudioSource
    [SerializeField] private AudioClip[] attackAudioClips;
    [SerializeField] private float attackAudioInterval = 1.5f;
    private bool isInAttackState;
    private float nextAttackAudioTime;
    private int bulletHitCount;
    private HashSet<int> countedBulletInstanceIds;

    void Start()
    {
        InitializeIfNeeded();

        if (!spawnStateInitialized)
        {
            ResetForSpawn(transform.position, transform.rotation, defaultKillGraceTime);
        }
    }

    void Update()
    {
        if (isKilled)
        {
            return;
        }

        if (player != null && agent != null)
        {
            Vector3 chaseTarget;
            if (TryGetChasePosition(player.position, out chaseTarget))
            {
                lastDestination = chaseTarget;
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                }
                agent.SetDestination(chaseTarget);
            }
            else if (lastDestination != Vector3.zero && !agent.hasPath)
            {
                agent.SetDestination(lastDestination);
            }

            if (animator != null)
            {
                Vector3 enemyFlat = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 playerFlat = new Vector3(player.position.x, 0f, player.position.z);
                float distanceToPlayer = Vector3.Distance(enemyFlat, playerFlat);
                Debug.Log("Distance to player: " + distanceToPlayer);
                bool shouldAttack = distanceToPlayer <= attackDistance;
                Debug.Log("Should Attack: " + shouldAttack);

                if (shouldAttack)
                {
                    Debug.Log("Chasing: " + animator.GetBool("isChasing"));
                    animator.SetBool(IsChasingHash, false);
                    animator.SetBool(IsAttackingHash1, true);
                    Debug.Log(animator.GetBool("isAttacking1"));
                    animator.SetBool(IsAttackingHash2, false);
                    if (!agent.isStopped)
                    {
                        agent.isStopped = true;
                    }
                    if (!isInAttackState)
                    {
                        isInAttackState = true;
                        nextAttackAudioTime = Time.time;
                    }

                    if (Time.time >= nextAttackAudioTime)
                    {
                        PlayAttackAudio();
                        float interval = Mathf.Max(attackAudioInterval, 0f);
                        nextAttackAudioTime = Time.time + interval;
                    }
                }
                else
                {
                    animator.SetBool(IsChasingHash, true);
                    animator.SetBool(IsAttackingHash1, false);
                    animator.SetBool(IsAttackingHash2, false);
                    if (agent.isStopped)
                    {
                        agent.isStopped = false;
                    }
                    currentAttackVariant = -1;
                    isInAttackState = false;
                    nextAttackAudioTime = 0f;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isKilled || animator == null)
        {
            return;
        }

        if (Time.time < killProtectionEndTime)
        {
            return;
        }

        if (useBulletHitCount)
        {
            if (!IsQualifyingBullet(collision.gameObject))
            {
                return;
            }

            // Optionally avoid counting the same bullet instance multiple times
            if (!countMultipleHitsFromSameInstance)
            {
                if (countedBulletInstanceIds == null)
                {
                    countedBulletInstanceIds = new HashSet<int>();
                }
                var root = collision.transform != null && collision.transform.root != null
                    ? collision.transform.root.gameObject
                    : collision.gameObject;
                int id = root.GetInstanceID();
                if (!countedBulletInstanceIds.Add(id))
                {
                    return;
                }
            }

            bulletHitCount++;
            if (bulletHitCount >= Mathf.Max(1, requiredBulletHits))
            {
                HandleKilled();
            }
            return;
        }

        if (requireSpecificKiller)
        {
            if (specificKiller == null || collision.gameObject != specificKiller)
            {
                return;
            }
        }

        float impulse = collision.impulse.magnitude;
        if (impulse >= killImpulseThreshold)
        {
            HandleKilled();
        }
    }

    private bool TryGetChasePosition(Vector3 playerPosition, out Vector3 chasePosition)
    {
        chasePosition = Vector3.zero;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(playerPosition, out navHit, navSampleDistance, NavMesh.AllAreas))
        {
            chasePosition = navHit.position;
            return true;
        }

        RaycastHit groundHit;
        Vector3 rayStart = playerPosition + Vector3.up * navSampleDistance;
        if (Physics.Raycast(rayStart, Vector3.down, out groundHit, navSampleDistance * 2f, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (NavMesh.SamplePosition(groundHit.point, out navHit, navSampleDistance, NavMesh.AllAreas))
            {
                chasePosition = navHit.position;
                return true;
            }
        }

        Vector3 horizontalTarget = new Vector3(playerPosition.x, transform.position.y, playerPosition.z);
        if (NavMesh.SamplePosition(horizontalTarget, out navHit, navSampleDistance, NavMesh.AllAreas))
        {
            chasePosition = navHit.position;
            return true;
        }

        return false;
    }

    public void AssignSpawner(EnemySpawner owner)
    {
        spawner = owner;
    }

    public void ResetForSpawn(Vector3 position, Quaternion rotation, float killGraceDuration)
    {
        InitializeIfNeeded();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        spawnPosition = position;
        spawnRotation = rotation;
        transform.SetPositionAndRotation(position, rotation);

        if (agent != null)
        {
            agent.Warp(position);
            agent.speed = speed;
            agent.stoppingDistance = attackDistance;
            agent.isStopped = false;
            agent.ResetPath();
        }

        isKilled = false;
        lastDestination = spawnPosition;
        currentAttackVariant = -1;
        killProtectionEndTime = Time.time + Mathf.Max(0f, killGraceDuration);
        spawnStateInitialized = true;
        isInAttackState = false;
        nextAttackAudioTime = 0f;
        bulletHitCount = 0;
        countedBulletInstanceIds?.Clear();

        if (animator != null)
        {
            animator.SetBool(IsKilledHash, false);
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsAttackingHash1, false);
            animator.SetBool(IsAttackingHash2, false);
        }
    }

    public bool IsKilled => isKilled;

    private void HandleKilled()
    {
        if (isKilled)
        {
            return;
        }

        isKilled = true;

        if (animator != null)
        {
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsAttackingHash1, false);
            animator.SetBool(IsAttackingHash2, false);
            animator.SetBool(IsKilledHash, true);
        }

        currentAttackVariant = -1;
        isInAttackState = false;
        nextAttackAudioTime = 0f;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        spawner?.HandleEnemyKilled(this);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = attackDistance;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        initialized = true;
    }

    private void OnDestroy()
    {
        spawner?.NotifyEnemyDestroyed(this);
    }

    private void PlayAttackAudio()
    {
        if (attackAudioClips == null || attackAudioClips.Length == 0)
        {
            return;
        }

        AudioSource source = audioSource;
        if (source == null)
        {
            source = GetComponent<AudioSource>();
            audioSource = source;
        }

        if (source != null)
        {
            int clipIndex = Random.Range(0, attackAudioClips.Length);
            AudioClip clip = attackAudioClips[clipIndex];
            if (clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }

    private bool IsQualifyingBullet(GameObject other)
    {
        if (other == null)
        {
            return false;
        }

        // Tag filter (optional)
        if (qualifyingBulletTags != null && qualifyingBulletTags.Length > 0)
        {
            for (int i = 0; i < qualifyingBulletTags.Length; i++)
            {
                var tag = qualifyingBulletTags[i];
                if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                {
                    return true;
                }
            }
        }

        // Prefab-name filter (optional)
        if (qualifyingBulletPrefabs != null && qualifyingBulletPrefabs.Length > 0)
        {
            string otherName = other.name;
            string rootName = other.transform != null && other.transform.root != null ? other.transform.root.name : null;

            for (int i = 0; i < qualifyingBulletPrefabs.Length; i++)
            {
                var prefab = qualifyingBulletPrefabs[i];
                if (prefab == null) continue;
                string prefabName = prefab.name;
                if (NamesMatch(otherName, prefabName) || (!string.IsNullOrEmpty(rootName) && NamesMatch(rootName, prefabName)))
                {
                    return true;
                }
            }
            return false;
        }

        // If no filters specified, treat any collision as qualifying in bullet-hit mode
        return true;
    }

    private static bool NamesMatch(string instanceName, string prefabName)
    {
        if (string.IsNullOrEmpty(instanceName) || string.IsNullOrEmpty(prefabName))
        {
            return false;
        }
        if (instanceName == prefabName)
        {
            return true;
        }
        // Common case for instantiated prefabs: "Name(Clone)"
        if (instanceName.StartsWith(prefabName) && instanceName.Contains("(Clone)"))
        {
            return true;
        }
        return false;
    }
}
