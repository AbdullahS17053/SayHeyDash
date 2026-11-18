using NUnit.Framework;
using SayHeyDash.Scoring;

namespace SayHeyDash.Tests
{
    /// <summary>AC-06: scores and ranks match the documented formula and boundaries.</summary>
    public sealed class ScoreCalculatorTests
    {
        private static readonly ScoringRules Rules = ScoringRules.Default;

        [Test]
        public void PerfectRun_Scores1000_Legendary()
        {
            ScoreBreakdown s = ScoreCalculator.Calculate(Rules, 1f, 1f, 1f);
            Assert.AreEqual(400, s.Positioning);
            Assert.AreEqual(300, s.Timing);
            Assert.AreEqual(300, s.Throw);
            Assert.AreEqual(1000, s.Total);
            Assert.AreEqual(Rank.Legendary, s.Rank);
        }

        [Test]
        public void BarelySuccessfulRun_ScoresExactly500_Rookie()
        {
            ScoreBreakdown s = ScoreCalculator.Calculate(Rules, 0f, 0f, 0f);
            Assert.AreEqual(500, s.Total);
            Assert.AreEqual(Rank.Rookie, s.Rank);
        }

        [Test]
        public void AnySuccess_IsAtLeast500_AndAtMost1000()
        {
            for (int a = 0; a <= 10; a++)
            for (int b = 0; b <= 10; b++)
            for (int c = 0; c <= 10; c++)
            {
                ScoreBreakdown s = ScoreCalculator.Calculate(Rules, a / 10f, b / 10f, c / 10f);
                Assert.That(s.Total, Is.InRange(500, 1000), $"q=({a},{b},{c})");
                Assert.AreNotEqual(Rank.None, s.Rank);
            }
        }

        [Test]
        public void HalfQuality_PaysThreeQuartersOfEachMaximum()
        {
            ScoreBreakdown s = ScoreCalculator.Calculate(Rules, 0.5f, 0.5f, 0.5f);
            Assert.AreEqual(300, s.Positioning);
            Assert.AreEqual(225, s.Timing);
            Assert.AreEqual(225, s.Throw);
            Assert.AreEqual(750, s.Total);
        }

        [TestCase(499, Rank.None)]
        [TestCase(500, Rank.Rookie)]
        [TestCase(749, Rank.Rookie)]
        [TestCase(750, Rank.AllStar)]
        [TestCase(899, Rank.AllStar)]
        [TestCase(900, Rank.Legendary)]
        [TestCase(1000, Rank.Legendary)]
        public void RankBoundaries_MatchGdd(int total, Rank expected)
        {
            Assert.AreEqual(expected, ScoreCalculator.RankFor(Rules, total));
        }

        [TestCase(-1f, 200)]
        [TestCase(2f, 400)]
        [TestCase(float.NaN, 200)]
        public void QualityOutsideRange_IsClamped(float quality, int expected)
        {
            Assert.AreEqual(expected, ScoreCalculator.CategoryPoints(400, 0.5f, quality));
        }

        [Test]
        public void Rounding_IsHalfAwayFromZero()
        {
            // 300 * (0.5 + 0.5 * 0.25) = 187.5 -> 188 (0.25 is exact in binary floating point)
            Assert.AreEqual(188, ScoreCalculator.CategoryPoints(300, 0.5f, 0.25f));
        }
    }
}
