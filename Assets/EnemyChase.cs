using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    public float speed = 3.5f;
    [Tooltip("Distance at which the enemy switches from chase to attack animation.")]
    public float attackDistance = 1.5f;
    [Tooltip("Collision impulse magnitude that will kill the enemy.")]
    public float killImpulseThreshold = 5f;
    [Tooltip("Destroy the enemy GameObject when killed.")]
    public bool destroyOnKill = true;
    [Tooltip("Respawn the enemy after the post-kill delay instead of leaving it dead.")]
    public bool respawnOnKill = false;
    [Tooltip("Seconds to wait before destroying or respawning after a kill.")]
    public float postKillDelay = 5f;
    [Tooltip("Seconds to ignore kill collisions after spawning or respawning.")]
    public float spawnKillGraceTime = 0.2f;
    [Tooltip("If enabled, only collisions from the specified GameObject can trigger a kill.")]
    public bool requireSpecificKiller = false;
    [Tooltip("GameObject required to trigger a kill when 'requireSpecificKiller' is true.")]
    public GameObject specificKiller;
    [Tooltip("Maximum distance to search above/below the player for a valid NavMesh point when the player is airborne.")]
    [SerializeField] private float navSampleDistance = 4f;
    [Tooltip("Optional layers considered ground when raycasting beneath the player as a fallback.")]
    [SerializeField] private LayerMask groundLayers = ~0;

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
    private Coroutine postKillRoutine;
    private float spawnTime;
    private Vector3 lastDestination;
    private int currentAttackVariant = -1;

    void Start()
    {
        // Find player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Get NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = attackDistance;
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        spawnTime = Time.time;
        lastDestination = spawnPosition;
        currentAttackVariant = -1;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (animator != null)
        {
            animator.SetBool(IsKilledHash, false);
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsAttackingHash1, false);
            animator.SetBool(IsAttackingHash2, false);
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
                bool shouldAttack = distanceToPlayer <= attackDistance;

                animator.SetBool(IsChasingHash, !shouldAttack);
                if (shouldAttack)
                {
                    if (currentAttackVariant == -1)
                    {
                        currentAttackVariant = Random.value > 0.5f ? 1 : 2;
                        animator.SetBool(IsAttackingHash1, currentAttackVariant == 1);
                        animator.SetBool(IsAttackingHash2, currentAttackVariant == 2);
                    }
                }
                else
                {
                    animator.SetBool(IsAttackingHash1, false);
                    animator.SetBool(IsAttackingHash2, false);
                    currentAttackVariant = -1;
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

        float timeSinceSpawn = Time.time - spawnTime;
        if (timeSinceSpawn < spawnKillGraceTime)
        {
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
            isKilled = true;
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsAttackingHash1, false);
            animator.SetBool(IsAttackingHash2, false);
            animator.SetBool(IsKilledHash, true);
            currentAttackVariant = -1;

            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (postKillRoutine != null)
            {
                StopCoroutine(postKillRoutine);
            }
            postKillRoutine = StartCoroutine(HandlePostKill());
        }
    }

    private System.Collections.IEnumerator HandlePostKill()
    {
        float wait = Mathf.Max(0f, postKillDelay);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }

        if (respawnOnKill)
        {
            Respawn();
        }
        else if (destroyOnKill)
        {
            Destroy(gameObject);
        }
    }

    private void Respawn()
    {
        isKilled = false;

        if (animator != null)
        {
            animator.SetBool(IsKilledHash, false);
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsAttackingHash1, false);
            animator.SetBool(IsAttackingHash2, false);
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (agent != null)
        {
            agent.Warp(spawnPosition);
            agent.isStopped = false;
        }

        spawnTime = Time.time;
        lastDestination = spawnPosition;
        currentAttackVariant = -1;
        postKillRoutine = null;
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
}
