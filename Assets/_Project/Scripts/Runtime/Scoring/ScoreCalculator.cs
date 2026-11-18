using System;

namespace SayHeyDash.Scoring
{
    /// <summary>
    /// Pure scoring function (Docs/01 §6, pending client sign-off Q6):
    /// <c>points = round(max × (f + (1 − f) × quality))</c> per category, where f is the
    /// minimum success fraction (0.5) and quality ∈ [0, 1]. Any success therefore scores
    /// at least 500 and a perfect run exactly 1,000.
    /// </summary>
    public static class ScoreCalculator
    {
        public static ScoreBreakdown Calculate(in ScoringRules rules,
            float positioningQuality, float timingQuality, float throwQuality)
        {
            int positioning = CategoryPoints(rules.MaxPositioning, rules.MinimumSuccessFraction, positioningQuality);
            int timing = CategoryPoints(rules.MaxTiming, rules.MinimumSuccessFraction, timingQuality);
            int throwPoints = CategoryPoints(rules.MaxThrow, rules.MinimumSuccessFraction, throwQuality);
            int total = positioning + timing + throwPoints;
            return new ScoreBreakdown(positioning, timing, throwPoints, total, RankFor(rules, total));
        }

        public static int CategoryPoints(int max, float minimumFraction, float quality)
        {
            float q = Clamp01(quality);
            float f = Clamp01(minimumFraction);
            return (int)Math.Round(max * (f + (1f - f) * q), MidpointRounding.AwayFromZero);
        }

        public static Rank RankFor(in ScoringRules rules, int total)
        {
            if (total >= rules.LegendaryThreshold) return Rank.Legendary;
            if (total >= rules.AllStarThreshold) return Rank.AllStar;
            if (total >= rules.RookieThreshold) return Rank.Rookie;
            return Rank.None;
        }

        private static float Clamp01(float v)
        {
            if (float.IsNaN(v)) return 0f;
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }
    }
}
