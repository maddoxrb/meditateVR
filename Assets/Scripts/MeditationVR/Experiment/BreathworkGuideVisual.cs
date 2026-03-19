using UnityEngine;

namespace MeditationVR.Experiment
{
    /// <summary>
    /// Drop this on any GameObject to spawn a guided breath visual:
    /// ring grows on inhale, shrinks on exhale, and shows instruction text.
    /// </summary>
    public sealed class BreathworkGuideVisual : MonoBehaviour
    {
        private enum BreathPhase
        {
            Inhale,
            InhaleHold,
            Exhale,
            ExhaleHold
        }

        [Header("Auto Placement")]
        [SerializeField]
        private bool placeInFrontOfCameraOnStart = true;

        [SerializeField, Min(0.2f)]
        private float distanceFromCamera = 1.5f;

        [SerializeField]
        private Vector3 cameraRelativeOffset = new Vector3(0f, -0.05f, 0f);

        [SerializeField]
        private bool faceMainCamera = true;

        [Header("Breathing Timing (seconds)")]
        [SerializeField, Min(0.1f)]
        private float inhaleDuration = 4f;

        [SerializeField, Min(0f)]
        private float inhaleHoldDuration = 1f;

        [SerializeField, Min(0.1f)]
        private float exhaleDuration = 6f;

        [SerializeField, Min(0f)]
        private float exhaleHoldDuration = 1f;

        [SerializeField]
        private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Ring")]
        [SerializeField, Min(0.01f)]
        private float minRadius = 0.08f;

        [SerializeField, Min(0.02f)]
        private float maxRadius = 0.2f;

        [SerializeField, Min(16)]
        private int ringSegments = 128;

        [SerializeField, Min(0.0005f)]
        private float ringWidth = 0.008f;

        [SerializeField]
        private Color inhaleColor = new Color(0.35f, 0.82f, 1f, 1f);

        [SerializeField]
        private Color exhaleColor = new Color(0.30f, 0.45f, 1f, 1f);

        [SerializeField]
        private Color holdColor = new Color(0.55f, 0.75f, 1f, 1f);

        [Header("Instruction Text")]
        [SerializeField]
        private bool showInstructionText = true;

        [SerializeField]
        private string inhaleText = "Breathe In";

        [SerializeField]
        private string exhaleText = "Breathe Out";

        [SerializeField]
        private string holdText = "Hold";

        [SerializeField]
        private Color instructionColor = Color.white;

        [SerializeField]
        private float instructionHeight = 0.28f;

        [SerializeField]
        private int instructionFontSize = 64;

        [Header("Optional Audio Prompts")]
        [SerializeField]
        private bool playAudioPrompts = false;

        [SerializeField]
        private AudioSource promptAudioSource;

        [SerializeField]
        private AudioClip inhalePromptClip;

        [SerializeField]
        private AudioClip exhalePromptClip;

        [Header("Debug")]
        [SerializeField]
        private bool verboseLogging = false;

        private Transform visualRoot;
        private LineRenderer ringRenderer;
        private TextMesh instructionTextMesh;
        private Material ringMaterial;
        private BreathPhase phase = BreathPhase.Inhale;
        private float phaseTime;
        private bool initialized;

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnEnable()
        {
            EnsureSetup();
            StartCycle();
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
                ringMaterial = null;
            }
        }

        private void Start()
        {
            if (placeInFrontOfCameraOnStart)
            {
                PlaceInFrontOfCamera();
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (faceMainCamera)
            {
                FaceCamera();
            }

            TickPhase(Time.deltaTime);
            ApplyVisuals();
        }

        [ContextMenu("Restart Cycle")]
        public void RestartCycle()
        {
            StartCycle();
        }

        private void EnsureSetup()
        {
            if (initialized)
            {
                return;
            }

            visualRoot = new GameObject("BreathVisualRoot").transform;
            visualRoot.SetParent(transform, false);

            CreateRing();
            CreateInstructionText();
            EnsurePromptAudioSource();

            initialized = true;
            Log("Breathwork guide initialized.");
        }

        private void CreateRing()
        {
            GameObject ringObject = new GameObject("BreathRing");
            ringObject.transform.SetParent(visualRoot, false);

            ringRenderer = ringObject.AddComponent<LineRenderer>();
            ringRenderer.loop = true;
            ringRenderer.useWorldSpace = false;
            ringRenderer.positionCount = Mathf.Max(16, ringSegments);
            ringRenderer.startWidth = ringWidth;
            ringRenderer.endWidth = ringWidth;
            ringRenderer.textureMode = LineTextureMode.Stretch;
            ringRenderer.alignment = LineAlignment.View;
            ringRenderer.numCapVertices = 8;
            ringRenderer.numCornerVertices = 8;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");

            ringMaterial = new Material(shader);
            if (ringMaterial.HasProperty("_Surface"))
            {
                ringMaterial.SetFloat("_Surface", 1f);
            }

            if (ringMaterial.HasProperty("_Cull"))
            {
                ringMaterial.SetFloat("_Cull", 0f);
            }

            ringRenderer.material = ringMaterial;
        }

        private void CreateInstructionText()
        {
            if (!showInstructionText)
            {
                return;
            }

            GameObject textObject = new GameObject("BreathInstructionText");
            textObject.transform.SetParent(visualRoot, false);
            textObject.transform.localPosition = new Vector3(0f, instructionHeight, 0f);

            instructionTextMesh = textObject.AddComponent<TextMesh>();
            instructionTextMesh.anchor = TextAnchor.MiddleCenter;
            instructionTextMesh.alignment = TextAlignment.Center;
            instructionTextMesh.fontSize = Mathf.Max(16, instructionFontSize);
            instructionTextMesh.characterSize = 0.01f;
            instructionTextMesh.color = instructionColor;
            instructionTextMesh.text = inhaleText;

            Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtInFont == null)
            {
                builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (builtInFont != null)
            {
                instructionTextMesh.font = builtInFont;
            }
        }

        private void EnsurePromptAudioSource()
        {
            if (promptAudioSource != null)
            {
                return;
            }

            GameObject audioObject = new GameObject("BreathPromptAudio");
            audioObject.transform.SetParent(visualRoot, false);
            promptAudioSource = audioObject.AddComponent<AudioSource>();
            promptAudioSource.playOnAwake = false;
            promptAudioSource.loop = false;
            promptAudioSource.spatialBlend = 0f;
        }

        private void StartCycle()
        {
            phase = BreathPhase.Inhale;
            phaseTime = 0f;
            OnPhaseEntered();
            ApplyVisuals();
        }

        private void TickPhase(float dt)
        {
            phaseTime += Mathf.Max(0f, dt);

            float duration = GetPhaseDuration(phase);
            if (duration <= 0f || phaseTime < duration)
            {
                return;
            }

            phaseTime = 0f;
            phase = phase switch
            {
                BreathPhase.Inhale => BreathPhase.InhaleHold,
                BreathPhase.InhaleHold => BreathPhase.Exhale,
                BreathPhase.Exhale => BreathPhase.ExhaleHold,
                _ => BreathPhase.Inhale
            };

            OnPhaseEntered();
        }

        private void OnPhaseEntered()
        {
            if (phase == BreathPhase.Inhale)
            {
                TryPlayPrompt(inhalePromptClip);
            }
            else if (phase == BreathPhase.Exhale)
            {
                TryPlayPrompt(exhalePromptClip);
            }
        }

        private float GetPhaseDuration(BreathPhase p)
        {
            return p switch
            {
                BreathPhase.Inhale => inhaleDuration,
                BreathPhase.InhaleHold => inhaleHoldDuration,
                BreathPhase.Exhale => exhaleDuration,
                _ => exhaleHoldDuration
            };
        }

        private void ApplyVisuals()
        {
            if (ringRenderer == null)
            {
                return;
            }

            float radius;
            Color color;
            string instruction;

            float duration = Mathf.Max(0.0001f, GetPhaseDuration(phase));
            float t = Mathf.Clamp01(phaseTime / duration);
            float eased = ease != null ? ease.Evaluate(t) : t;

            switch (phase)
            {
                case BreathPhase.Inhale:
                    radius = Mathf.Lerp(minRadius, maxRadius, eased);
                    color = inhaleColor;
                    instruction = inhaleText;
                    break;
                case BreathPhase.InhaleHold:
                    radius = maxRadius;
                    color = holdColor;
                    instruction = holdText;
                    break;
                case BreathPhase.Exhale:
                    radius = Mathf.Lerp(maxRadius, minRadius, eased);
                    color = exhaleColor;
                    instruction = exhaleText;
                    break;
                default:
                    radius = minRadius;
                    color = holdColor;
                    instruction = holdText;
                    break;
            }

            DrawRing(radius, color);

            if (instructionTextMesh != null)
            {
                instructionTextMesh.text = instruction;
                instructionTextMesh.color = instructionColor;
            }
        }

        private void DrawRing(float radius, Color color)
        {
            int count = ringRenderer.positionCount;
            float angleStep = Mathf.PI * 2f / count;

            for (int i = 0; i < count; i++)
            {
                float a = i * angleStep;
                ringRenderer.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }

            ringRenderer.startColor = color;
            ringRenderer.endColor = color;
        }

        private void TryPlayPrompt(AudioClip clip)
        {
            if (!playAudioPrompts || clip == null || promptAudioSource == null)
            {
                return;
            }

            promptAudioSource.PlayOneShot(clip);
        }

        private void PlaceInFrontOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            transform.position = cam.transform.position
                                 + cam.transform.forward * distanceFromCamera
                                 + cam.transform.TransformVector(cameraRelativeOffset);
            FaceCamera();
        }

        private void FaceCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 toCam = transform.position - cam.transform.position;
            if (toCam.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }
        }

        private void Log(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[Experiment][BreathworkGuideVisual] {message}", this);
            }
        }
    }
}
