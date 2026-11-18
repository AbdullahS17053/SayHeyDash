namespace SayHeyDash.Scoring
{
    /// <summary>Immutable scoring parameters (plain data, so the calculator is testable without assets).</summary>
    public readonly struct ScoringRules
    {
        public readonly int MaxPositioning;
        public readonly int MaxTiming;
        public readonly int MaxThrow;
        public readonly float MinimumSuccessFraction;
        public readonly int RookieThreshold;
        public readonly int AllStarThreshold;
        public readonly int LegendaryThreshold;

        public ScoringRules(int maxPositioning, int maxTiming, int maxThrow, float minimumSuccessFraction,
            int rookieThreshold, int allStarThreshold, int legendaryThreshold)
        {
            MaxPositioning = maxPositioning;
            MaxTiming = maxTiming;
            MaxThrow = maxThrow;
            MinimumSuccessFraction = minimumSuccessFraction;
            RookieThreshold = rookieThreshold;
            AllStarThreshold = allStarThreshold;
            LegendaryThreshold = legendaryThreshold;
        }

        /// <summary>The GDD defaults: 400 / 300 / 300, 50 % floor, ranks 500 / 750 / 900.</summary>
        public static ScoringRules Default => new ScoringRules(400, 300, 300, 0.5f, 500, 750, 900);
    }
}
