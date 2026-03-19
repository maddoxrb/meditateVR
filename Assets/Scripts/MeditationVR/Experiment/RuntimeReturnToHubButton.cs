using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeditationVR.Experiment
{
    /// <summary>
    /// Attach to a runtime UI button and wire OnClick -> OnReturnToHubPressed.
    /// </summary>
    public sealed class RuntimeReturnToHubButton : MonoBehaviour
    {
        [SerializeField]
        private MeditationSceneOrchestrator orchestrator;

        [SerializeField]
        private bool verboseLogging = true;

        public void OnReturnToHubPressed()
        {
            if (orchestrator == null)
            {
                orchestrator = FindObjectOfType<MeditationSceneOrchestrator>();
            }

            if (orchestrator != null)
            {
                if (verboseLogging)
                {
                    Debug.Log("[Experiment][RuntimeReturnToHubButton] Returning to hub via orchestrator.", this);
                }

                orchestrator.ReturnToHubNow();
                return;
            }

            if (verboseLogging)
            {
                Debug.LogWarning(
                    "[Experiment][RuntimeReturnToHubButton] Orchestrator not found, loading hub scene directly.",
                    this);
            }

            SceneManager.LoadScene(MeditationSceneOrchestrator.HubSceneName);
        }
    }
}
