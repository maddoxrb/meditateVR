using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    public float speed = 3.5f;
    [Tooltip("Distance at which the enemy switches from chase to attack animation.")]
    public float attackDistance = 1.5f;

    private Transform player;
    private NavMeshAgent agent;
    [SerializeField] private Animator animator; // assign in inspector if animator is on a child
    private static readonly int IsChasingHash = Animator.StringToHash("isChasing");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");

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

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
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
}
