using System;
using UnityEngine;

namespace MeditationVR.Experiment
{
    public enum GuidanceMode
    {
        Guided = 0,
        Nonguided = 1
    }

    public enum EnvironmentType
    {
        Nature = 0,
        NonNature = 1
    }

    public enum PaletteMode
    {
        Cool = 0,
        Warm = 1
    }

    [Serializable]
    public struct ExperimentConfig : IEquatable<ExperimentConfig>
    {
        public const int MinSessionMinutes = 1;
        public const int MaxSessionMinutes = 60;
        public const int DefaultSessionMinutes = 5;

        public GuidanceMode guidance;
        public EnvironmentType environment;
        public PaletteMode palette;

        [Range(MinSessionMinutes, MaxSessionMinutes)]
        public int sessionMinutes;

        public string conditionId => BuildConditionId(guidance, environment, palette);

        public ExperimentConfig(
            GuidanceMode guidance,
            EnvironmentType environment,
            PaletteMode palette,
            int sessionMinutes = DefaultSessionMinutes)
        {
            this.guidance = guidance;
            this.environment = environment;
            this.palette = palette;
            this.sessionMinutes = Mathf.Clamp(sessionMinutes, MinSessionMinutes, MaxSessionMinutes);
        }

        public static ExperimentConfig CreateDefault()
        {
            return new ExperimentConfig(
                GuidanceMode.Guided,
                EnvironmentType.Nature,
                PaletteMode.Cool,
                DefaultSessionMinutes);
        }

        public static string BuildConditionId(
            GuidanceMode guidance,
            EnvironmentType environment,
            PaletteMode palette)
        {
            return $"G_{guidance}-Env_{environment}-P_{palette}";
        }

        public ExperimentConfig Sanitized()
        {
            guidance = Enum.IsDefined(typeof(GuidanceMode), guidance) ? guidance : GuidanceMode.Guided;
            environment = Enum.IsDefined(typeof(EnvironmentType), environment) ? environment : EnvironmentType.Nature;
            palette = Enum.IsDefined(typeof(PaletteMode), palette) ? palette : PaletteMode.Cool;

            int safeMinutes = sessionMinutes <= 0 ? DefaultSessionMinutes : sessionMinutes;
            sessionMinutes = Mathf.Clamp(safeMinutes, MinSessionMinutes, MaxSessionMinutes);

            return this;
        }

        public bool Equals(ExperimentConfig other)
        {
            return guidance == other.guidance
                   && environment == other.environment
                   && palette == other.palette
                   && sessionMinutes == other.sessionMinutes;
        }

        public override bool Equals(object obj)
        {
            return obj is ExperimentConfig other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)guidance;
                hash = (hash * 397) ^ (int)environment;
                hash = (hash * 397) ^ (int)palette;
                hash = (hash * 397) ^ sessionMinutes;
                return hash;
            }
        }

        public static bool operator ==(ExperimentConfig left, ExperimentConfig right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExperimentConfig left, ExperimentConfig right)
        {
            return !left.Equals(right);
        }
    }
}
