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

    private Transform player;
    private NavMeshAgent agent;
    [SerializeField] private Animator animator; // assign in inspector if animator is on a child
    private static readonly int IsChasingHash = Animator.StringToHash("isChasing");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int IsKilledHash = Animator.StringToHash("isKilled");
    private bool isKilled;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Coroutine postKillRoutine;
    private float spawnTime;

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
            animator.SetBool(IsAttackingHash, false);
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
            agent.SetDestination(player.position);

            if (animator != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                bool shouldAttack = distanceToPlayer <= attackDistance;

                animator.SetBool(IsChasingHash, !shouldAttack);
                animator.SetBool(IsAttackingHash, shouldAttack);
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
            animator.SetBool(IsAttackingHash, false);
            animator.SetBool(IsKilledHash, true);

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
            animator.SetBool(IsAttackingHash, false);
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (agent != null)
        {
            agent.Warp(spawnPosition);
            agent.isStopped = false;
        }

        spawnTime = Time.time;
        postKillRoutine = null;
    }
}
