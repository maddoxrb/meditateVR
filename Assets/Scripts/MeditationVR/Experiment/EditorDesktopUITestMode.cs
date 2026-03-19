using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MeditationVR.Experiment
{
    [AddComponentMenu("Meditation VR/Experiment/Editor Desktop UI Test Mode")]
    public sealed class EditorDesktopUITestMode : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField]
        private bool enableInEditorPlayMode = true;

        [Header("XR UI Modules")]
        [SerializeField]
        private bool disableOVROverlayCanvas = true;

        [SerializeField]
        private bool disablePointableCanvasModule = true;

        [Header("Desktop Input")]
        [SerializeField]
        private bool ensureDesktopInputModule = true;

        [SerializeField]
        private bool verboseLogging = true;

        private void Awake()
        {
            if (!ShouldApply())
            {
                return;
            }

            ApplyDesktopTestMode();
        }

        private bool ShouldApply()
        {
            return enableInEditorPlayMode && Application.isEditor && Application.isPlaying;
        }

        private void ApplyDesktopTestMode()
        {
            int disabledOverlayCount = 0;
            int disabledPointableModuleCount = 0;

            if (disableOVROverlayCanvas)
            {
                OVROverlayCanvas[] overlays = FindObjectsOfType<OVROverlayCanvas>(true);
                foreach (OVROverlayCanvas overlay in overlays)
                {
                    if (overlay != null && overlay.enabled)
                    {
                        overlay.enabled = false;
                        disabledOverlayCount++;
                    }
                }
            }

            if (disablePointableCanvasModule)
            {
                PointableCanvasModule[] modules = FindObjectsOfType<PointableCanvasModule>(true);
                foreach (PointableCanvasModule module in modules)
                {
                    if (module != null && module.enabled)
                    {
                        module.enabled = false;
                        disabledPointableModuleCount++;
                    }
                }
            }

            if (ensureDesktopInputModule)
            {
                EnsureDesktopInput();
            }

            if (verboseLogging)
            {
                Debug.Log(
                    $"[Experiment] EditorDesktopUITestMode applied. " +
                    $"Disabled OVROverlayCanvas={disabledOverlayCount}, " +
                    $"Disabled PointableCanvasModule={disabledPointableModuleCount}");
            }
        }

        private static void EnsureDesktopInput()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = FindObjectOfType<EventSystem>();
            }

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
#else
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            inputModule.enabled = true;
#endif
        }
    }
}
