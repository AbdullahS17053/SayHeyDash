using SayHeyDash.Scoring;
using UnityEngine;

namespace SayHeyDash.Data
{
    /// <summary>
    /// Score maxima and rank thresholds (GDD §5, proposed formula in Docs/01 §6 - pending Q6).
    /// </summary>
    [CreateAssetMenu(menuName = "Say Hey Dash/Scoring Config", fileName = "ScoringConfig")]
    public sealed class ScoringConfig : ScriptableObject
    {
        [Min(0)] public int maxPositioningPoints = 400;
        [Min(0)] public int maxCatchTimingPoints = 300;
        [Min(0)] public int maxThrowAccuracyPoints = 300;

        [Tooltip("Fraction of each category's maximum paid for a barely-successful action (0.5 => any success >= 500).")]
        [Range(0f, 1f)] public float minimumSuccessFraction = 0.5f;

        [Tooltip("Lowest total that earns ROOKIE (successful missions always reach this with the default formula).")]
        [Min(0)] public int rookieThreshold = 500;
        [Min(0)] public int allStarThreshold = 750;
        [Min(0)] public int legendaryThreshold = 900;

        /// <summary>Plain-data copy for the pure <see cref="ScoreCalculator"/>.</summary>
        public ScoringRules ToRules() => new ScoringRules(
            maxPositioningPoints, maxCatchTimingPoints, maxThrowAccuracyPoints, minimumSuccessFraction,
            rookieThreshold, allStarThreshold, legendaryThreshold);
    }
}
