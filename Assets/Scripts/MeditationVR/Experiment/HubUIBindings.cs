using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeditationVR.Experiment
{
    public sealed class HubUIBindings : MonoBehaviour
    {
        public const string RuntimeSceneName = "MeditationRuntime";

        private ConfigManager manager;

        private void Awake()
        {
            manager = ConfigManager.Instance;
        }

        public void OnGuidanceDropdownChanged(int optionIndex)
        {
            EnsureManager();
            manager.SetGuidanceMode(ParseGuidanceMode(optionIndex));
        }

        public void OnEnvironmentDropdownChanged(int optionIndex)
        {
            EnsureManager();
            manager.SetEnvironmentType(ParseEnvironmentType(optionIndex));
        }

        public void OnPaletteDropdownChanged(int optionIndex)
        {
            EnsureManager();
            manager.SetPaletteMode(ParsePaletteMode(optionIndex));
        }

        public void OnSessionMinutesChanged(string text)
        {
            EnsureManager();

            if (!int.TryParse(text, out int minutes))
            {
                Debug.LogWarning($"[Experiment] Invalid session minutes input: \"{text}\"");
                return;
            }

            manager.SetSessionMinutes(Mathf.Clamp(
                minutes,
                ExperimentConfig.MinSessionMinutes,
                ExperimentConfig.MaxSessionMinutes));
        }

        public void OnStartPressed()
        {
            EnsureManager();
            SceneManager.LoadScene(RuntimeSceneName);
        }

        public void OnResetPressed()
        {
            EnsureManager();
            manager.ResetToDefaults();
        }

        private void EnsureManager()
        {
            if (manager == null)
            {
                manager = ConfigManager.Instance;
            }
        }

        private static GuidanceMode ParseGuidanceMode(int optionIndex)
        {
            if (Enum.IsDefined(typeof(GuidanceMode), optionIndex))
            {
                return (GuidanceMode)optionIndex;
            }

            Debug.LogWarning($"[Experiment] Guidance index {optionIndex} is out of range. Defaulting to Guided.");
            return GuidanceMode.Guided;
        }

        private static EnvironmentType ParseEnvironmentType(int optionIndex)
        {
            if (Enum.IsDefined(typeof(EnvironmentType), optionIndex))
            {
                return (EnvironmentType)optionIndex;
            }

            Debug.LogWarning($"[Experiment] Environment index {optionIndex} is out of range. Defaulting to Nature.");
            return EnvironmentType.Nature;
        }

        private static PaletteMode ParsePaletteMode(int optionIndex)
        {
            if (Enum.IsDefined(typeof(PaletteMode), optionIndex))
            {
                return (PaletteMode)optionIndex;
            }

            Debug.LogWarning($"[Experiment] Palette index {optionIndex} is out of range. Defaulting to Cool.");
            return PaletteMode.Cool;
        }
    }
}
