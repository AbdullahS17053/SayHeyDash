namespace SayHeyDash.Gameplay
{
    /// <summary>Outcome of Stage C. <see cref="Quality"/> is 1 at the zone centre, 0 at its edge.</summary>
    public readonly struct ThrowResult
    {
        public readonly bool Success;
        public readonly float Quality;
        /// <summary>Needle position when pressed (0..1), or -1 if the time limit ran out.</summary>
        public readonly float NeedlePosition;

        public ThrowResult(bool success, float quality, float needlePosition)
        {
            Success = success;
            Quality = quality;
            NeedlePosition = needlePosition;
        }

        public bool TimedOut => NeedlePosition < 0f;

        public override string ToString() =>
            TimedOut ? "Throw timed out" : $"{(Success ? "Hit" : "Miss")} at {NeedlePosition:0.000} (quality {Quality:0.00})";
    }
}
