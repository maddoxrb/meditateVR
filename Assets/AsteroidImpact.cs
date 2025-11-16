using UnityEngine;

public class AsteroidImpact : MonoBehaviour
{
    [SerializeField] private float fallbackSpeed = 45f;
    [SerializeField] private float maxLifetime = 10f;
    [SerializeField] private float raycastPadding = 0.5f;
    [SerializeField] private ParticleSystem fallbackImpactEffect;
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [Header("Audio")]
    [SerializeField] private AudioSource impactAudioSourcePrefab;

    private Vector3 travelDirection = Vector3.down;
    private float travelSpeed;
    private ParticleSystem impactEffectPrefab;
    private float lifetimeTimer;
    private bool initialized;
    private bool active;
    private AsteroidSpawner owner;

    private void Awake()
    {
        lifetimeTimer = maxLifetime;
        travelSpeed = fallbackSpeed;
        impactEffectPrefab = fallbackImpactEffect;
    }

    public void Initialize(Vector3 direction, float speed, ParticleSystem impactEffect, LayerMask groundMask, float lifetime)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            travelDirection = direction.normalized;
        }

        travelSpeed = Mathf.Max(0.01f, speed);
        impactEffectPrefab = impactEffect != null ? impactEffect : fallbackImpactEffect;
        groundLayers = groundMask;
        maxLifetime = lifetime > 0f ? lifetime : maxLifetime;
        lifetimeTimer = maxLifetime;
        transform.rotation = Quaternion.LookRotation(travelDirection);
        initialized = true;
        active = true;
        enabled = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            Initialize(travelDirection, travelSpeed, impactEffectPrefab, groundLayers, maxLifetime);
        }

        float stepDistance = travelSpeed * Time.deltaTime;
        if (stepDistance <= 0f)
        {
            return;
        }

        if (Physics.Raycast(transform.position, travelDirection, out RaycastHit hit, stepDistance + raycastPadding, groundLayers, QueryTriggerInteraction.Ignore))
        {
            HandleImpact(hit.point, hit.normal);
            return;
        }

        transform.position += travelDirection * stepDistance;
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            HandleImpact(transform.position, -travelDirection);
        }
    }

    private void HandleImpact(Vector3 point, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            ParticleSystem spawned = Instantiate(impactEffectPrefab, point, rotation);
            var main = spawned.main;
            float duration = main.duration;
            if (main.loop)
            {
                Destroy(spawned.gameObject, duration);
            }
            else
            {
                float lifetime = duration;
                if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    lifetime += main.startLifetime.constantMax;
                }
                else
                {
                    lifetime += main.startLifetime.constant;
                }
                Destroy(spawned.gameObject, lifetime);
            }
        }

        PlayImpactAudio(point);

        active = false;
        enabled = false;
        owner?.NotifyAsteroidAvailable(this);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        initialized = false;
    }

    public bool IsActive => active;

    public void AssignOwner(AsteroidSpawner spawner)
    {
        owner = spawner;
    }

    private void PlayImpactAudio(Vector3 point)
    {
        if (impactAudioSourcePrefab == null)
        {
            return;
        }

        AudioSource instance = Instantiate(impactAudioSourcePrefab, point, Quaternion.identity);
        instance.transform.position = point;
        instance.Play();

        float clipDuration = 0f;
        if (instance.clip != null)
        {
            float pitch = Mathf.Approximately(instance.pitch, 0f) ? 1f : instance.pitch;
            clipDuration = instance.clip.length / Mathf.Abs(pitch);
        }

        float lifetime = clipDuration > 0f ? clipDuration : 1f;
        Destroy(instance.gameObject, lifetime);
    }
}
