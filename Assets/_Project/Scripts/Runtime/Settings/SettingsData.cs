using System;

namespace SayHeyDash.Settings
{
    /// <summary>
    /// The player's options plus the best score (no personal data - GDD Version 1).
    /// Volumes are stored as steps (0..<see cref="VolumeSteps"/>) so the menu can cycle them
    /// with one button and the saved values stay small integers.
    /// </summary>
    [Serializable]
    public struct SettingsData
    {
        public const int VolumeSteps = 5;

        public bool Muted;
        public int MusicLevel;
        public int EffectsLevel;
        public bool ReducedMotion;
        public bool AssistedMode;
        public int BestScore;

        /// <summary>First visit: sound on, full volume, reduced motion follows the OS/browser preference.</summary>
        public static SettingsData Defaults(bool reducedMotion) => new SettingsData
        {
            Muted = false,
            MusicLevel = VolumeSteps,
            EffectsLevel = VolumeSteps,
            ReducedMotion = reducedMotion,
            AssistedMode = false,
            BestScore = 0,
        };

        public static float LevelToVolume(int level) => ClampLevel(level) / (float)VolumeSteps;

        /// <summary>One press of the menu button: 100% -> 80% ... 0% -> back to 100%.</summary>
        public static int NextLevelDown(int level) => level <= 0 ? VolumeSteps : ClampLevel(level) - 1;

        public static int ClampLevel(int level) => level < 0 ? 0 : (level > VolumeSteps ? VolumeSteps : level);

        public static int Percent(int level) => ClampLevel(level) * 100 / VolumeSteps;
    }
}
