using UnityEngine;

namespace MeditationVR.Experiment
{
    public sealed class AmbientSphereColorCycle : MonoBehaviour
    {
        public enum VisualStyle
        {
            GradientCycle = 0,
            ProceduralFlow = 1
        }

        public enum FlowTheme
        {
            Cool = 0,
            Warm = 1
        }

        [Header("Target")]
        [SerializeField]
        private Renderer targetRenderer;

        [SerializeField]
        private string baseColorProperty = "_BaseColor";

        [Header("Style")]
        [SerializeField]
        private VisualStyle visualStyle = VisualStyle.ProceduralFlow;

        [SerializeField]
        private FlowTheme flowTheme = FlowTheme.Cool;

        [Header("Force Compatible Material")]
        [SerializeField]
        private bool forceCompatibleMaterialOnAwake = true;

        [SerializeField]
        private bool preferUnlitShader = true;

        [SerializeField]
        private bool renderInsideFaces = true;

        [SerializeField]
        private bool assignMaterialToSharedSlot = true;

        [Header("Gradient Cycle (Legacy)")]
        [SerializeField, Min(1f)]
        private float cycleDurationSeconds = 45f;

        [SerializeField]
        private Gradient colorGradient;

        [SerializeField]
        private bool randomStartOffset = true;

        [Header("Procedural Flow")]
        [SerializeField, Min(0.01f)]
        private float flowSpeed = 0.20f;

        [SerializeField, Min(0.1f)]
        private float noiseScale = 2.20f;

        [SerializeField, Min(1f)]
        private float streakScale = 14f;

        [SerializeField, Min(0f)]
        private float streakIntensity = 0.35f;

        [SerializeField, Min(0f)]
        private float flowEmissionIntensity = 0.70f;

        [Header("Emission (Gradient Mode)")]
        [SerializeField]
        private bool driveEmission = true;

        [SerializeField]
        private string emissionColorProperty = "_EmissionColor";

        [SerializeField, Min(0f)]
        private float emissionIntensity = 0.35f;

        [Header("Debug")]
        [SerializeField]
        private bool verboseLogging = false;

        private static readonly int ColorAId = Shader.PropertyToID("_ColorA");
        private static readonly int ColorBId = Shader.PropertyToID("_ColorB");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int StreakScaleId = Shader.PropertyToID("_StreakScale");
        private static readonly int StreakIntensityId = Shader.PropertyToID("_StreakIntensity");
        private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");

        private MaterialPropertyBlock propertyBlock;
        private float startTime;

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
            ApplyCoolPreset();
        }

        private void Awake()
        {
            EnsureRuntimeState();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                return;
            }

            if (forceCompatibleMaterialOnAwake)
            {
                ForceCompatibleMaterial();
            }

            RefreshColorPropertyNameFromMaterial();
        }

        private void OnEnable()
        {
            EnsureRuntimeState();
            startTime = Time.time + (randomStartOffset ? Random.Range(0f, cycleDurationSeconds) : 0f);
        }

        private void Update()
        {
            EnsureRuntimeState();

            if (targetRenderer == null)
            {
                return;
            }

            if (visualStyle == VisualStyle.ProceduralFlow)
            {
                ApplyFlowProperties();
                return;
            }

            if (cycleDurationSeconds <= 0f || colorGradient == null)
            {
                return;
            }

            float t = Mathf.Repeat((Time.time - startTime) / cycleDurationSeconds, 1f);
            Color color = colorGradient.Evaluate(t);
            ApplyGradientColor(color);
        }

        [ContextMenu("Apply Cool Preset")]
        public void ApplyCoolPreset()
        {
            flowTheme = FlowTheme.Cool;
            visualStyle = VisualStyle.ProceduralFlow;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.08f, 0.24f, 0.70f), 0f),
                    new GradientColorKey(new Color(0.12f, 0.63f, 0.85f), 0.33f),
                    new GradientColorKey(new Color(0.25f, 0.37f, 0.90f), 0.66f),
                    new GradientColorKey(new Color(0.08f, 0.24f, 0.70f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            colorGradient = gradient;
        }

        [ContextMenu("Apply Warm Preset")]
        public void ApplyWarmPreset()
        {
            flowTheme = FlowTheme.Warm;
            visualStyle = VisualStyle.ProceduralFlow;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.92f, 0.42f, 0.12f), 0f),
                    new GradientColorKey(new Color(0.98f, 0.65f, 0.12f), 0.33f),
                    new GradientColorKey(new Color(0.86f, 0.20f, 0.28f), 0.66f),
                    new GradientColorKey(new Color(0.92f, 0.42f, 0.12f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            colorGradient = gradient;
        }

        [ContextMenu("Force Compatible Material")]
        public void ForceCompatibleMaterial()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                return;
            }

            Shader shader = ResolveBestShader();
            if (shader == null)
            {
                Debug.LogWarning("[Experiment] No compatible shader found for AmbientSphereColorCycle.", this);
                return;
            }

            Material material = new Material(shader)
            {
                name = $"{shader.name}_AmbientSphereRuntime"
            };

            ConfigureMaterial(material);

            if (assignMaterialToSharedSlot)
            {
                targetRenderer.sharedMaterial = material;
            }
            else
            {
                targetRenderer.material = material;
            }

            RefreshColorPropertyNameFromMaterial();
            Log($"Forced compatible material using shader: {shader.name}");
        }

        private Shader ResolveBestShader()
        {
            if (visualStyle == VisualStyle.ProceduralFlow)
            {
                Shader flow = Shader.Find("MeditationVR/AmbientFlowSphere");
                if (flow != null)
                {
                    return flow;
                }
            }

            if (preferUnlitShader)
            {
                Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (urpUnlit != null)
                {
                    return urpUnlit;
                }
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                return urpLit;
            }

            Shader legacyUnlit = Shader.Find("Unlit/Color");
            if (legacyUnlit != null)
            {
                return legacyUnlit;
            }

            return Shader.Find("Standard");
        }

        private void ConfigureMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (renderInsideFaces && material.HasProperty("_Cull"))
            {
                // Cull Off for robustness: visible from inside even with flipped mesh winding.
                material.SetFloat("_Cull", 0f);
            }
            else if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 2f);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.black);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.black);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
        }

        private void RefreshColorPropertyNameFromMaterial()
        {
            Material material = targetRenderer != null ? targetRenderer.sharedMaterial : null;
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                baseColorProperty = "_BaseColor";
            }
            else if (material.HasProperty("_Color"))
            {
                baseColorProperty = "_Color";
            }
        }

        private void ApplyGradientColor(Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            EnsureRuntimeState();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(baseColorProperty, color);

            if (driveEmission)
            {
                propertyBlock.SetColor(emissionColorProperty, color * emissionIntensity);
            }

            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ApplyFlowProperties()
        {
            if (targetRenderer == null)
            {
                return;
            }

            EnsureRuntimeState();
            GetThemeColors(out Color colorA, out Color colorB, out Color accent);
            Material material = GetCurrentMaterial();
            if (!MaterialSupportsProceduralFlow(material))
            {
                ApplyFallbackFlowColor(colorA, colorB, accent);
                return;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorAId, colorA);
            propertyBlock.SetColor(ColorBId, colorB);
            propertyBlock.SetColor(AccentColorId, accent);
            propertyBlock.SetFloat(FlowSpeedId, flowSpeed);
            propertyBlock.SetFloat(NoiseScaleId, noiseScale);
            propertyBlock.SetFloat(StreakScaleId, streakScale);
            propertyBlock.SetFloat(StreakIntensityId, streakIntensity);
            propertyBlock.SetFloat(EmissionIntensityId, flowEmissionIntensity);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private Material GetCurrentMaterial()
        {
            if (targetRenderer == null)
            {
                return null;
            }

            Material material = targetRenderer.sharedMaterial;
            if (material != null)
            {
                return material;
            }

            return targetRenderer.material;
        }

        private static bool MaterialSupportsProceduralFlow(Material material)
        {
            return material != null
                   && material.HasProperty(ColorAId)
                   && material.HasProperty(ColorBId)
                   && material.HasProperty(AccentColorId)
                   && material.HasProperty(FlowSpeedId)
                   && material.HasProperty(NoiseScaleId);
        }

        private void ApplyFallbackFlowColor(Color colorA, Color colorB, Color accent)
        {
            targetRenderer.GetPropertyBlock(propertyBlock);

            Color fallbackColor = Color.Lerp(colorA, colorB, 0.5f);
            fallbackColor = Color.Lerp(fallbackColor, accent, 0.20f);
            propertyBlock.SetColor(baseColorProperty, fallbackColor);

            if (driveEmission)
            {
                propertyBlock.SetColor(
                    emissionColorProperty,
                    fallbackColor * Mathf.Max(emissionIntensity, flowEmissionIntensity));
            }

            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void GetThemeColors(out Color a, out Color b, out Color accent)
        {
            if (flowTheme == FlowTheme.Warm)
            {
                a = new Color(0.95f, 0.35f, 0.10f);
                b = new Color(0.82f, 0.18f, 0.28f);
                accent = new Color(0.95f, 0.55f, 0.18f);
                return;
            }

            a = new Color(0.08f, 0.24f, 0.70f);
            b = new Color(0.12f, 0.63f, 0.85f);
            accent = new Color(0.18f, 0.42f, 0.88f);
        }

        private void EnsureRuntimeState()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void Log(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Experiment][AmbientSphereColorCycle] {message}", this);
            }
        }
    }
}
