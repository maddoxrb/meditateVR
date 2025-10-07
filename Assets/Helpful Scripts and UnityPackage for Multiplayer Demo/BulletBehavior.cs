using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class BulletBehavior : NetworkBehaviour
{
    [Header("Lifetime in seconds")]
    public float lifeTime = 5f;

    private float spawnTime;

    public override void Spawned()
    {
        spawnTime = Runner.SimulationTime;
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.SimulationTime - spawnTime >= lifeTime)
        {
            Runner.Despawn(Object);
        }
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     // Optional: despawn immediately on hit
    //     if (Object != null && Object.HasStateAuthority)
    //     {
    //         Runner.Despawn(Object);
    //     }
    // }
}
