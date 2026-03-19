using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeditationVR.Experiment
{
    public sealed class MeditationSceneOrchestrator : MonoBehaviour
    {
        public const string RuntimeSceneName = "MeditationRuntime";
        public const string HubSceneName = "ConditionSelect";

        [Header("Anchors")]
        [SerializeField]
        private Transform environmentAnchor;

        [SerializeField]
        private Transform guidanceAnchor;

        [Header("Environment Prefabs")]
        [SerializeField]
        private GameObject natureEnvironmentPrefab;

        [SerializeField]
        private GameObject nonNatureEnvironmentPrefab;

        [Header("Guidance Prefabs")]
        [SerializeField]
        private GameObject guidedGuidancePrefab;

        [SerializeField]
        private GameObject nonguidedGuidancePrefab;

        [Header("Visual Profiles")]
        [SerializeField]
        private VisualProfile coolProfile;

        [SerializeField]
        private VisualProfile warmProfile;

        [SerializeField]
        private VisualProfileApplier visualProfileApplier;

        [Header("Session")]
        [SerializeField]
        private bool startSessionOnStart = true;

        private Coroutine sessionRoutine;
        private ExperimentConfig activeConfig;
        private GameObject spawnedEnvironmentInstance;
        private GameObject spawnedGuidanceInstance;

        private void Start()
        {
            ComposeFromCurrentConfig();

            if (startSessionOnStart)
            {
                StartSession();
            }
        }

        public void ComposeFromCurrentConfig()
        {
            activeConfig = ResolveConfig();

            EnsureAnchors();
            CleanupSpawnedContent();

            spawnedEnvironmentInstance = SpawnEnvironment(activeConfig.environment);
            spawnedGuidanceInstance = SpawnGuidance(activeConfig.guidance);
            ApplyPalette(activeConfig.palette);

            ExperimentLogger.LogRuntimeComposition(activeConfig.conditionId);
        }

        public void StartSession()
        {
            if (activeConfig.sessionMinutes <= 0)
            {
                activeConfig = ResolveConfig();
            }

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
            }

            ExperimentLogger.LogSessionStart(activeConfig);
            int safeMinutes = Mathf.Clamp(
                activeConfig.sessionMinutes,
                ExperimentConfig.MinSessionMinutes,
                ExperimentConfig.MaxSessionMinutes);
            sessionRoutine = StartCoroutine(SessionTimerRoutine(safeMinutes));
        }

        public void EndSession()
        {
            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
                sessionRoutine = null;
            }

            ExperimentLogger.LogSessionEnd(activeConfig);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            SceneManager.LoadScene(HubSceneName);
#endif
        }

        private IEnumerator SessionTimerRoutine(int minutes)
        {
            yield return new WaitForSeconds(minutes * 60f);
            sessionRoutine = null;
            EndSession();
        }

        private ExperimentConfig ResolveConfig()
        {
            if (ConfigManager.TryGetExisting(out ConfigManager manager))
            {
                return manager.GetConfig();
            }

            return ExperimentConfig.CreateDefault();
        }

        private void EnsureAnchors()
        {
            if (environmentAnchor == null)
            {
                environmentAnchor = CreateAnchor("EnvironmentAnchor");
            }

            if (guidanceAnchor == null)
            {
                guidanceAnchor = CreateAnchor("GuidanceAnchor");
            }
        }

        private Transform CreateAnchor(string anchorName)
        {
            Transform existing = transform.Find(anchorName);
            if (existing != null)
            {
                return existing;
            }

            GameObject anchorObject = new GameObject(anchorName);
            anchorObject.transform.SetParent(transform, false);
            return anchorObject.transform;
        }

        private void CleanupSpawnedContent()
        {
            if (environmentAnchor != null)
            {
                ClearChildren(environmentAnchor);
            }

            if (guidanceAnchor != null)
            {
                ClearChildren(guidanceAnchor);
            }

            spawnedEnvironmentInstance = null;
            spawnedGuidanceInstance = null;
        }

        private GameObject SpawnEnvironment(EnvironmentType environmentType)
        {
            GameObject prefab = environmentType == EnvironmentType.Nature
                ? natureEnvironmentPrefab
                : nonNatureEnvironmentPrefab;
            return SpawnUnderAnchor(prefab, environmentAnchor, "environment");
        }

        private GameObject SpawnGuidance(GuidanceMode guidanceMode)
        {
            GameObject prefab = guidanceMode == GuidanceMode.Guided
                ? guidedGuidancePrefab
                : nonguidedGuidancePrefab;
            return SpawnUnderAnchor(prefab, guidanceAnchor, "guidance");
        }

        private GameObject SpawnUnderAnchor(GameObject prefab, Transform anchor, string contentType)
        {
            if (anchor == null)
            {
                Debug.LogWarning($"[Experiment] Missing {contentType} anchor.");
                return null;
            }

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[Experiment] Missing {contentType} prefab for condition {activeConfig.conditionId}.");
                return null;
            }

            return Instantiate(prefab, anchor, false);
        }

        private void ApplyPalette(PaletteMode paletteMode)
        {
            VisualProfile profile = paletteMode == PaletteMode.Cool ? coolProfile : warmProfile;
            if (profile == null)
            {
                Debug.LogWarning($"[Experiment] Missing visual profile for palette {paletteMode}.");
                return;
            }

            if (visualProfileApplier == null)
            {
                visualProfileApplier = GetComponent<VisualProfileApplier>();
            }

            if (visualProfileApplier == null)
            {
                visualProfileApplier = FindObjectOfType<VisualProfileApplier>();
            }

            if (visualProfileApplier == null)
            {
                GameObject applierObject = new GameObject(nameof(VisualProfileApplier));
                visualProfileApplier = applierObject.AddComponent<VisualProfileApplier>();
            }

            visualProfileApplier.Apply(profile);
        }

        private static void ClearChildren(Transform anchor)
        {
            for (int i = anchor.childCount - 1; i >= 0; i--)
            {
                Transform child = anchor.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
