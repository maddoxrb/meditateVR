using System;
using UnityEngine;
using UnityEngine.Events;

namespace MeditationVR.Experiment
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ConfigManager : MonoBehaviour
    {
        [Serializable]
        public sealed class ExperimentConfigUnityEvent : UnityEvent<ExperimentConfig>
        {
        }

        private static ConfigManager instance;

        [SerializeField]
        private ExperimentConfig config;

        [SerializeField]
        private ExperimentConfigUnityEvent configChangedUnityEvent = new ExperimentConfigUnityEvent();

        public static ConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ConfigManager>();
                    if (instance == null)
                    {
                        GameObject managerObject = new GameObject(nameof(ConfigManager));
                        instance = managerObject.AddComponent<ConfigManager>();
                    }
                }

                return instance;
            }
        }

        public static bool TryGetExisting(out ConfigManager manager)
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ConfigManager>();
            }

            manager = instance;
            return manager != null;
        }

        public event Action<ExperimentConfig> ConfigChanged;

        public ExperimentConfigUnityEvent ConfigChangedUnityEvent => configChangedUnityEvent;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            config = config.Sanitized();
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnValidate()
        {
            config = config.Sanitized();
        }

        public void SetGuidanceMode(GuidanceMode guidanceMode)
        {
            ExperimentConfig next = config;
            next.guidance = guidanceMode;
            SetConfigInternal(next);
        }

        public void SetEnvironmentType(EnvironmentType environmentType)
        {
            ExperimentConfig next = config;
            next.environment = environmentType;
            SetConfigInternal(next);
        }

        public void SetPaletteMode(PaletteMode paletteMode)
        {
            ExperimentConfig next = config;
            next.palette = paletteMode;
            SetConfigInternal(next);
        }

        public void SetSessionMinutes(int minutes)
        {
            ExperimentConfig next = config;
            next.sessionMinutes = Mathf.Clamp(
                minutes,
                ExperimentConfig.MinSessionMinutes,
                ExperimentConfig.MaxSessionMinutes);
            SetConfigInternal(next);
        }

        public ExperimentConfig GetConfig()
        {
            config = config.Sanitized();
            return config;
        }

        public void ResetToDefaults()
        {
            SetConfigInternal(ExperimentConfig.CreateDefault(), true);
        }

        private void SetConfigInternal(ExperimentConfig newConfig, bool forceNotify = false)
        {
            ExperimentConfig sanitized = newConfig.Sanitized();
            if (!forceNotify && sanitized == config)
            {
                return;
            }

            config = sanitized;
            ExperimentLogger.LogConfigSet(config);
            ConfigChanged?.Invoke(config);
            configChangedUnityEvent?.Invoke(config);
        }
    }
}
