using UnityEngine;

/// <summary>
/// Spawns a hit particle effect when this object receives a sufficiently strong collision.
/// </summary>
public class HitMarker : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Particle system prefab to spawn at the collision contact point.")]
    private ParticleSystem hitEffectPrefab;

    [SerializeField]
    [Tooltip("Minimum collision impulse required before the effect is spawned.")]
    private float impulseThreshold = 5f;

    [SerializeField]
    [Tooltip("If true the particle effect will be rotated to face away from the surface normal.")]
    private bool alignToSurfaceNormal = true;

    [Header("Audio")]
    [SerializeField]
    [Tooltip("Enable to play the assigned audio source whenever the hit effect is triggered.")]
    private bool playAudioOnHit = false;

    [SerializeField]
    [Tooltip("Optional audio source to play when a hit is registered.")]
    private AudioSource hitAudioSource;

    private void OnCollisionEnter(Collision collision)
    {
        bool shouldSpawnEffect = hitEffectPrefab != null;
        bool shouldPlayAudio = playAudioOnHit && hitAudioSource != null;

        if (!shouldSpawnEffect && !shouldPlayAudio)
        {
            return;
        }

        // collision.impulse is the total impulse applied to resolve the collision.
        if (collision.impulse.magnitude < impulseThreshold)
        {
            return;
        }

        if (shouldPlayAudio)
        {
            PlayHitAudio();
        }

        if (!shouldSpawnEffect)
        {
            return;
        }

        // Play the effect at every contact point so multi-point hits are handled out of the box.
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            SpawnEffect(contact.point, contact.normal);
        }
    }

    private void OnValidate()
    {
        if (impulseThreshold < 0f)
        {
            impulseThreshold = 0f;
        }
    }

    private void SpawnEffect(Vector3 position, Vector3 normal)
    {
        Quaternion rotation = alignToSurfaceNormal
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        ParticleSystem instance = Instantiate(hitEffectPrefab, position, rotation);
        instance.Play();

        // Clean up the spawned particle system once it has finished playing.
        var main = instance.main;
        if (!main.loop)
        {
            float maxLifetime = main.duration;
            var lifetime = main.startLifetime;
            switch (lifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    maxLifetime += lifetime.constant;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    maxLifetime += lifetime.constantMax;
                    break;
                default:
                    maxLifetime += lifetime.constantMax; // Fallback for curves.
                    break;
            }

            Destroy(instance.gameObject, maxLifetime);
        }
    }

    private void PlayHitAudio()
    {
        if (hitAudioSource.isPlaying)
        {
            hitAudioSource.Stop();
        }

        hitAudioSource.Play();
    }
}
