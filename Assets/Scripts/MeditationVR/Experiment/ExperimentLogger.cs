using UnityEngine;

namespace MeditationVR.Experiment
{
    public static class ExperimentLogger
    {
        private const string Prefix = "[Experiment]";

        public static void LogConfigSet(ExperimentConfig config)
        {
            Debug.Log($"{Prefix} Config set: {config.conditionId}, SessionMinutes={config.sessionMinutes}");
        }

        public static void LogRuntimeComposition(string conditionId)
        {
            Debug.Log($"{Prefix} Runtime composed for condition: {conditionId}");
        }

        public static void LogSessionStart(ExperimentConfig config)
        {
            Debug.Log($"{Prefix} Session started: {config.conditionId}, Duration={config.sessionMinutes} minute(s)");
        }

        public static void LogSessionEnd(ExperimentConfig config)
        {
            Debug.Log($"{Prefix} Session ended: {config.conditionId}");
        }
    }
}
