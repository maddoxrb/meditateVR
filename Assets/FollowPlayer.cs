using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform target;       // assign at runtime if null
    [SerializeField] private float repathRate = 0.1f; // seconds between path updates
    private NavMeshAgent agent;
    private float t;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    void Update()
    {
        if (!target || !agent.isOnNavMesh) return;

        t += Time.deltaTime;
        if (t >= repathRate)
        {
            t = 0f;
            agent.SetDestination(target.position);
        }
    }
}