namespace SayHeyDash.Scoring
{
    /// <summary>Result of a completed mission: points per category, total and rank.</summary>
    public readonly struct ScoreBreakdown
    {
        public readonly int Positioning;
        public readonly int Timing;
        public readonly int Throw;
        public readonly int Total;
        public readonly Rank Rank;

        public ScoreBreakdown(int positioning, int timing, int throwPoints, int total, Rank rank)
        {
            Positioning = positioning;
            Timing = timing;
            Throw = throwPoints;
            Total = total;
            Rank = rank;
        }

        public override string ToString() =>
            $"Positioning {Positioning} + Timing {Timing} + Throw {Throw} = {Total} ({Rank})";
    }
}
