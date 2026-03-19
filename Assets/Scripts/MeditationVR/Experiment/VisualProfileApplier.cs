using UnityEngine;
using UnityEngine.Rendering;

namespace MeditationVR.Experiment
{
    public sealed class VisualProfileApplier : MonoBehaviour
    {
        [SerializeField]
        private Light mainDirectionalLight;

        [SerializeField]
        private Volume globalVolume;

        public void Apply(VisualProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[Experiment] VisualProfileApplier.Apply called with a null profile.");
                return;
            }

            RenderSettings.ambientLight = profile.ambientLight;

            if (profile.skyboxMaterial != null)
            {
                RenderSettings.skybox = profile.skyboxMaterial;
            }

            RenderSettings.fog = profile.fogEnabled;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogDensity = Mathf.Max(0f, profile.fogDensity);

            if (mainDirectionalLight == null)
            {
                mainDirectionalLight = RenderSettings.sun;
            }

            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = profile.directionalLightColor;
                mainDirectionalLight.intensity = profile.directionalLightIntensity;
            }

            if (globalVolume != null && profile.postProcessingProfile != null)
            {
                globalVolume.sharedProfile = profile.postProcessingProfile;
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}
