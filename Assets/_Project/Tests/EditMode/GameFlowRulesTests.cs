using NUnit.Framework;
using SayHeyDash.Flow;

namespace SayHeyDash.Tests
{
    /// <summary>AC-07: the seven states connect exactly as in Docs/01 §5.</summary>
    public sealed class GameFlowRulesTests
    {
        [TestCase(GameState.None, GameState.Title)]
        [TestCase(GameState.Title, GameState.MissionBriefing)]
        [TestCase(GameState.MissionBriefing, GameState.TrackBall)]
        [TestCase(GameState.TrackBall, GameState.CatchWindow)]
        [TestCase(GameState.CatchWindow, GameState.CatchSequence)]
        [TestCase(GameState.CatchWindow, GameState.MissionFailed)]
        [TestCase(GameState.CatchSequence, GameState.Throw)]
        [TestCase(GameState.Throw, GameState.MissionComplete)]
        [TestCase(GameState.Throw, GameState.MissionFailed)]
        [TestCase(GameState.MissionFailed, GameState.TrackBall)]
        [TestCase(GameState.MissionComplete, GameState.TrackBall)]
        [TestCase(GameState.MissionComplete, GameState.Replay)]
        [TestCase(GameState.Replay, GameState.Title)]
        [TestCase(GameState.Throw, GameState.TrackBall)] // restart from pause
        [TestCase(GameState.CatchSequence, GameState.TrackBall)]
        public void DocumentedTransitions_AreAllowed(GameState from, GameState to)
        {
            Assert.IsTrue(GameFlowRules.IsAllowed(from, to));
        }

        [TestCase(GameState.Title, GameState.TrackBall)]
        [TestCase(GameState.TrackBall, GameState.MissionComplete)]
        [TestCase(GameState.CatchWindow, GameState.Throw)]
        [TestCase(GameState.MissionFailed, GameState.MissionComplete)]
        [TestCase(GameState.MissionFailed, GameState.Replay)]
        [TestCase(GameState.Replay, GameState.TrackBall)]
        [TestCase(GameState.CatchSequence, GameState.MissionFailed)]
        public void ShortcutsAndAlternativeHistories_AreRejected(GameState from, GameState to)
        {
            Assert.IsFalse(GameFlowRules.IsAllowed(from, to));
        }

        [Test]
        public void OnlyLiveStates_CanPause()
        {
            foreach (GameState s in System.Enum.GetValues(typeof(GameState)))
            {
                bool expected = s == GameState.TrackBall || s == GameState.CatchWindow ||
                                s == GameState.CatchSequence || s == GameState.Throw;
                Assert.AreEqual(expected, GameFlowRules.CanPause(s), s.ToString());
            }
        }
    }
}
