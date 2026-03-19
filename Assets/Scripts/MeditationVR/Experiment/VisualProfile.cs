using UnityEngine;
using UnityEngine.Rendering;

namespace MeditationVR.Experiment
{
    [CreateAssetMenu(
        fileName = "VisualProfile",
        menuName = "Meditation VR/Experiment/Visual Profile")]
    public sealed class VisualProfile : ScriptableObject
    {
        [Header("Lighting")]
        public Color ambientLight = Color.gray;
        public Material skyboxMaterial;
        public Color directionalLightColor = Color.white;
        public float directionalLightIntensity = 1f;

        [Header("Fog")]
        public Color fogColor = Color.gray;
        public bool fogEnabled;
        [Min(0f)]
        public float fogDensity = 0.01f;

        [Header("Post Processing (Optional)")]
        public VolumeProfile postProcessingProfile;
    }
}
