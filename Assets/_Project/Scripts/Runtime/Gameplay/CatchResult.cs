namespace SayHeyDash.Gameplay
{
    /// <summary>Outcome of Stage B plus the quality values used for scoring (0 = barely, 1 = perfect).</summary>
    public readonly struct CatchResult
    {
        public readonly CatchOutcome Outcome;
        public readonly float PositioningQuality;
        public readonly float TimingQuality;
        /// <summary>Press time minus perfect time, in seconds (negative = early).</summary>
        public readonly float TimingError;
        /// <summary>Distance from the outfielder's feet to the landing point, in world units.</summary>
        public readonly float Distance;

        public CatchResult(CatchOutcome outcome, float positioningQuality, float timingQuality,
            float timingError, float distance)
        {
            Outcome = outcome;
            PositioningQuality = positioningQuality;
            TimingQuality = timingQuality;
            TimingError = timingError;
            Distance = distance;
        }

        public bool Success => Outcome == CatchOutcome.Caught;

        public override string ToString() =>
            $"{Outcome} (distance {Distance:0.00}, timing {TimingError:+0.000;-0.000}s, " +
            $"posQ {PositioningQuality:0.00}, timeQ {TimingQuality:0.00})";
    }
}
