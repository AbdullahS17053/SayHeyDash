using UnityEngine;

namespace SayHeyDash.Data
{
    /// <summary>
    /// Multipliers applied on top of <see cref="GameplayTuning"/>. Version 1 ships two
    /// profiles: Default (all 1) and the clearly labelled Assisted mode (wider catch window).
    /// </summary>
    [CreateAssetMenu(menuName = "Say Hey Dash/Difficulty Profile", fileName = "DifficultyProfile")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Shown on screen, e.g. \"ASSISTED MODE\".")]
        public string displayName = "DEFAULT";

        [Tooltip("Multiplies GameplayTuning.catchWindowSeconds.")]
        [Min(0.1f)] public float catchWindowMultiplier = 1f;

        [Tooltip("Multiplies GameplayTuning.landingZoneRadius.")]
        [Min(0.1f)] public float landingZoneMultiplier = 1f;

        [Tooltip("Multiplies GameplayTuning.throwTargetZoneWidth.")]
        [Min(0.1f)] public float throwZoneMultiplier = 1f;
    }
}
