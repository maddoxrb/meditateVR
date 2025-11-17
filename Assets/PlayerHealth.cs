using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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
    [Tooltip("Optional transform to use as the center of the damage radius. Leave empty to use this component's transform. Useful for XR rigs where the playable rig root moves.")]
    [SerializeField] private Transform detectionCenter;
    [Tooltip("Optional tag used to identify enemies when the layer mask is empty.")]
    [SerializeField] private string enemyTag;
    [Tooltip("Fallback name fragment if layers/explicit objects are not set. Case-insensitive.")]
    [SerializeField] private string enemyNameContains = "Martian";
    [Tooltip("If no detection center is specified, automatically try to use the first child camera (or the main camera) so the radius follows the moving rig.")]
    [SerializeField] private bool autoAssignCenterFromCamera = true;
    [SerializeField] private float autoCenterSearchIntervalSeconds = 1f;

    [Header("Damage Scaling")]
    [Tooltip("If enabled, damage per tick scales with the number of enemies within radius.")]
    [SerializeField] private bool scaleDamageByEnemyCount = true;

    [Header("Audio")]
    [Tooltip("Optional AudioSource to play damage audio from. Falls back to a local AudioSource on this GameObject if not set.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sound to play whenever the player loses health.")]
    [SerializeField] private AudioClip damageClip;
    [Range(0f,1f)]
    [SerializeField] private float damageClipVolume = 1f;

    [Header("Haptics")]
    [Tooltip("Amplitude of the damage haptic pulse (0-1).")]
    [SerializeField, Range(0f, 1f)] private float damageHapticAmplitude = 0.8f;
    [Tooltip("Frequency of the damage haptic pulse (0-1).")]
    [SerializeField, Range(0f, 1f)] private float damageHapticFrequency = 0.4f;
    [Tooltip("Duration of each pulse in seconds.")]
    [SerializeField, Min(0f)] private float damageHapticPulseDuration = 0.12f;
    [Tooltip("Delay between pulses in the double burst (seconds).")]
    [SerializeField, Min(0f)] private float damageHapticPulseGap = 0.05f;

    public event Action<int, int> OnDamaged;
    public event Action OnDeath;

    public int CurrentHearts => currentHearts;
    public int MaxHearts => maxHearts;

    private readonly Collider[] overlapBuffer = new Collider[32];
    private HashSet<Transform> nearbyEnemyRoots;

    private int currentHearts;
    private float timeNearEnemy;
    private bool isNearEnemy;
    private Transform autoDetectedCenter;
    private float nextAutoCenterSearchTime;
    private Coroutine damageHapticsRoutine;

    private void Awake()
    {
        currentHearts = Mathf.Max(1, maxHearts);
        timeNearEnemy = 0f;
        isNearEnemy = false;
        nearbyEnemyRoots = new HashSet<Transform>();
        autoDetectedCenter = null;
        nextAutoCenterSearchTime = 0f;
    }

    private void OnDisable()
    {
        StopDamageHaptics();
    }

    private void OnValidate()
    {
        maxHearts = Mathf.Max(1, maxHearts);
        damageRadius = Mathf.Max(0.05f, damageRadius);
        damageIntervalSeconds = Mathf.Max(0.05f, damageIntervalSeconds);
        damageClipVolume = Mathf.Clamp01(damageClipVolume);
        autoCenterSearchIntervalSeconds = Mathf.Max(0.1f, autoCenterSearchIntervalSeconds);
    }

    private void Update()
    {
        if (currentHearts <= 0)
            return;

        int nearbyCount = CountEnemiesWithinRadius();

        if (nearbyCount <= 0)
        {
            isNearEnemy = false;
            timeNearEnemy = 0f;
            return;
        }

        if (!isNearEnemy)
        {
            isNearEnemy = true;
            timeNearEnemy = 0f;
            ApplyDamage(scaleDamageByEnemyCount ? nearbyCount : 1);
            return;
        }

        timeNearEnemy += Time.deltaTime;
        if (timeNearEnemy >= damageIntervalSeconds)
        {
            timeNearEnemy -= damageIntervalSeconds;
            ApplyDamage(scaleDamageByEnemyCount ? nearbyCount : 1);
        }
    }

    private int CountEnemiesWithinRadius()
    {
        float sqrRadius = damageRadius * damageRadius;
        Transform center = GetDetectionCenterRuntime();
        nearbyEnemyRoots.Clear();

        // 1) Explicit enemies (optional)
        if (explicitEnemyObjects != null)
        {
            foreach (var enemy in explicitEnemyObjects)
            {
                if (enemy == null)
                    continue;
                Vector3 offset = enemy.transform.position - center.position;
                if (offset.sqrMagnitude <= sqrRadius)
                {
                    nearbyEnemyRoots.Add(enemy.transform.root);
                }
            }
        }

        // 2) Physics overlap for any other enemies
        int mask = enemyLayers.value == 0 ? ~0 : enemyLayers.value;
        int count = Physics.OverlapSphereNonAlloc(
            center.position,
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
            {
                nearbyEnemyRoots.Add(other.root);
            }
        }

        return nearbyEnemyRoots.Count;
    }

    private bool IsEnemyCollider(Collider other)
    {
        if (other == null)
            return false;

        if (explicitEnemyObjects != null)
        {
            foreach (var enemy in explicitEnemyObjects)
            {
                if (enemy == null)
                    continue;

                if (other.transform == enemy.transform || other.transform.IsChildOf(enemy.transform))
                    return true;
            }
        }

        if (enemyLayers.value != 0)
        {
            if (((1 << other.gameObject.layer) & enemyLayers.value) != 0)
                return true;
        }
        else
        {
            // Layer mask is empty: allow tag-based identification if provided
            if (!string.IsNullOrEmpty(enemyTag))
            {
                var tr = other.transform;
                if (tr.CompareTag(enemyTag) || tr.root.CompareTag(enemyTag))
                {
                    return true;
                }
            }
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
            PlayDamageAudio();
            TriggerDamageHaptics();

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
        Transform center = detectionCenter != null ? detectionCenter : FindCameraCenter(includeInactiveChildren: true) ?? transform;
        Gizmos.DrawSphere(center.position, Mathf.Max(0.05f, damageRadius));
    }

    private void PlayDamageAudio()
    {
        if (damageClip == null)
        {
            return;
        }

        var src = audioSource != null ? audioSource : GetComponent<AudioSource>();
        if (src == null)
        {
            return;
        }

        src.PlayOneShot(damageClip, damageClipVolume);
    }

    private void TriggerDamageHaptics()
    {
        if (damageHapticPulseDuration <= 0f)
            return;

        StopDamageHaptics();
        damageHapticsRoutine = StartCoroutine(DamageHapticRoutine());
    }

    private IEnumerator DamageHapticRoutine()
    {
        float pulseDuration = Mathf.Max(0f, damageHapticPulseDuration);
        float gapDuration = Mathf.Max(0f, damageHapticPulseGap);

        yield return PlayHapticBurst();
        if (gapDuration > 0f)
        {
            yield return new WaitForSeconds(gapDuration);
        }
        yield return PlayHapticBurst();
        damageHapticsRoutine = null;
    }

    private IEnumerator PlayHapticBurst()
    {
        float amplitude = Mathf.Clamp01(damageHapticAmplitude);
        float frequency = Mathf.Clamp01(damageHapticFrequency);
        float duration = Mathf.Max(0f, damageHapticPulseDuration);

        OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }

    private void StopDamageHaptics()
    {
        if (damageHapticsRoutine != null)
        {
            StopCoroutine(damageHapticsRoutine);
            damageHapticsRoutine = null;
        }

        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }

    private Transform GetDetectionCenterRuntime()
    {
        if (detectionCenter != null)
            return detectionCenter;

        if (!autoAssignCenterFromCamera)
            return transform;

        if (autoDetectedCenter == null || !autoDetectedCenter.gameObject.activeInHierarchy)
        {
            if (!Application.isPlaying || Time.time >= nextAutoCenterSearchTime)
            {
                autoDetectedCenter = FindCameraCenter(includeInactiveChildren: false);
                if (autoDetectedCenter == null && Application.isPlaying)
                {
                    nextAutoCenterSearchTime = Time.time + autoCenterSearchIntervalSeconds;
                }
            }
        }

        return autoDetectedCenter != null ? autoDetectedCenter : transform;
    }

    private Transform FindCameraCenter(bool includeInactiveChildren)
    {
        Camera selectedCamera = null;
        var childCameras = GetComponentsInChildren<Camera>(includeInactiveChildren);
        if (childCameras != null && childCameras.Length > 0)
        {
            foreach (var cam in childCameras)
            {
                if (cam == null)
                    continue;

                if (cam.CompareTag("MainCamera"))
                {
                    selectedCamera = cam;
                    break;
                }

                if (selectedCamera == null)
                {
                    selectedCamera = cam;
                }
            }
        }

        if (selectedCamera == null)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null && (includeInactiveChildren || mainCamera.gameObject.activeInHierarchy))
            {
                selectedCamera = mainCamera;
            }
        }

        return selectedCamera != null ? selectedCamera.transform : null;
    }
}
