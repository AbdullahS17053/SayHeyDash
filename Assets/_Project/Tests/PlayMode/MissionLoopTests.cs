using System;
using System.Collections;
using NUnit.Framework;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Drives the real Main scene through complete missions with a scripted "player"
    /// (AC-04, AC-05, AC-07, AC-08). Any Debug.LogError / exception fails the test.
    /// Runs at 4x game speed; judging uses the same frame's values, so results are exact.
    /// </summary>
    public sealed class MissionLoopTests
    {
        private const float TimeScale = 4f;
        private const float StepTimeoutSeconds = 20f; // real seconds

        private GameFlowController _flow;
        private OutfielderController _outfielder;
        private BallFlight _ball;
        private CatchSystem _catch;
        private ThrowSystem _throw;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _flow = Object.FindFirstObjectByType<GameFlowController>();
            _outfielder = Object.FindFirstObjectByType<OutfielderController>();
            _ball = Object.FindFirstObjectByType<BallFlight>();
            _catch = Object.FindFirstObjectByType<CatchSystem>();
            _throw = Object.FindFirstObjectByType<ThrowSystem>();
            Assert.IsNotNull(_flow, "Main scene has no GameFlowController - run Say Hey Dash > Build Main Scene.");
            Assert.AreEqual(GameState.Title, _flow.State);
            Time.timeScale = TimeScale;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator TwentyFiveConsecutiveMissions_CompleteReplayAndReturnToTitle()
        {
            // Listeners must see a ready throw meter at the moment the Throw state is announced.
            int throwEntries = 0;
            _flow.StateChanged += (prev, next) =>
            {
                if (next != GameState.Throw) return;
                throwEntries++;
                Assert.IsTrue(_throw.IsActive && _throw.ZoneWidth > 0f, "throw meter not initialised on state entry");
            };

            yield return FromTitleToTrackBall();
            for (int i = 0; i < 25; i++)
            {
                yield return PlaySuccessfulAttempt();
                Assert.AreEqual(GameState.MissionComplete, _flow.State, $"attempt {i}");
                Assert.That(_flow.LastScore.Total, Is.InRange(500, 1000), $"attempt {i}");

                if (i % 5 == 4)
                {
                    _flow.ShowReplayMode();
                    Assert.AreEqual(GameState.Replay, _flow.State);
                    yield return null;
                    _flow.BackToTitle();
                    Assert.AreEqual(GameState.Title, _flow.State);
                    yield return null;
                    yield return FromTitleToTrackBall();
                }
                else
                {
                    _flow.ReplayMission();
                    Assert.AreEqual(GameState.TrackBall, _flow.State);
                    yield return null;
                }
            }
            Assert.AreEqual(25, _flow.CompletedCount);
            Assert.AreEqual(25, throwEntries);
        }

        [UnityTest]
        public IEnumerator Failures_AlwaysLeadToTryAgain_AndRetryCleanly()
        {
            yield return FromTitleToTrackBall();
            for (int i = 0; i < 12; i++)
            {
                switch (i % 4)
                {
                    case 0: // never press -> ball drops
                        yield return WaitUntil(() => _flow.State == GameState.MissionFailed);
                        Assert.AreEqual(FailReason.BallMissed, _flow.LastFailReason);
                        break;
                    case 1: // press from the start position as soon as presses count -> out of zone
                        yield return WaitUntil(() => _catch.Cue != CatchCue.None);
                        yield return null; // a press on the state-change frame is deliberately ignored
                        _flow.SimulateActionPress();
                        yield return WaitUntil(() => _flow.State == GameState.MissionFailed);
                        Assert.AreEqual(CatchOutcome.OutOfZone, _flow.LastCatchOutcome);
                        break;
                    case 2: // in position but too early
                        _outfielder.TeleportTo(_ball.Trajectory.Landing);
                        yield return WaitUntil(() => _catch.Cue == CatchCue.Ready);
                        yield return null;
                        Assert.AreEqual(CatchCue.Ready, _catch.Cue, "frame too long for this test");
                        _flow.SimulateActionPress();
                        yield return WaitUntil(() => _flow.State == GameState.MissionFailed);
                        Assert.AreEqual(CatchOutcome.TooEarly, _flow.LastCatchOutcome);
                        break;
                    case 3: // good catch, bad throw
                        yield return CatchCleanly();
                        yield return WaitUntil(() => _flow.State == GameState.Throw);
                        yield return WaitUntil(() =>
                            Mathf.Abs(_throw.Needle - _throw.ZoneCenter) > _throw.ZoneWidth * 2f && _throw.Elapsed > 0f);
                        _flow.SimulateActionPress();
                        yield return WaitUntil(() => _flow.State == GameState.MissionFailed);
                        Assert.AreEqual(FailReason.ThrowMissed, _flow.LastFailReason);
                        break;
                }
                _flow.Retry();
                Assert.AreEqual(GameState.TrackBall, _flow.State);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTheBall_AndRestartResetsTheAttempt()
        {
            yield return FromTitleToTrackBall();
            yield return WaitUntil(() => _ball.Elapsed > 0.5f);

            _flow.Pause();
            Assert.IsTrue(_flow.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            float frozen = _ball.Elapsed;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(frozen, _ball.Elapsed, "ball must not move while paused");

            _flow.SimulateActionPress(); // ignored while paused
            Assert.AreEqual(GameState.TrackBall, _flow.State);

            int attempts = _flow.AttemptCount;
            _flow.RestartAttempt();
            Time.timeScale = TimeScale;
            Assert.IsFalse(_flow.IsPaused);
            Assert.AreEqual(GameState.TrackBall, _flow.State);
            Assert.AreEqual(attempts + 1, _flow.AttemptCount);
            Assert.Less(_ball.Elapsed, 0.01f);

            yield return PlaySuccessfulAttempt();
            Assert.AreEqual(GameState.MissionComplete, _flow.State);
        }

        [UnityTest]
        public IEnumerator AssistedMode_WidensTheCatchWindow()
        {
            float defaultWindow;
            yield return FromTitleToTrackBall();
            defaultWindow = _catch.WindowSeconds;
            _flow.Pause();
            _flow.RestartAttempt();
            Time.timeScale = TimeScale;
            yield return PlaySuccessfulAttempt();
            _flow.ShowReplayMode();
            _flow.BackToTitle();
            yield return null;

            _flow.SetAssistedMode(true);
            Assert.IsTrue(_flow.AssistedMode);
            yield return FromTitleToTrackBall();
            Assert.Greater(_catch.WindowSeconds, defaultWindow);
            Assert.Greater(_catch.ZoneRadius, 0f);
        }

        // ------------------------------------------------------------------ scripted player

        private IEnumerator FromTitleToTrackBall()
        {
            Assert.AreEqual(GameState.Title, _flow.State);
            _flow.StartMission();
            Assert.AreEqual(GameState.MissionBriefing, _flow.State);
            yield return null;
            _flow.BeginMission();
            Assert.AreEqual(GameState.TrackBall, _flow.State);
            yield return null;
        }

        private IEnumerator CatchCleanly()
        {
            _outfielder.TeleportTo(_ball.Trajectory.Landing + new Vector2(0.1f, 0f));
            yield return WaitUntil(() => _catch.Cue == CatchCue.Now);
            yield return WaitUntil(() => _ball.Elapsed >= _catch.PerfectTime - 0.03f);
            _flow.SimulateActionPress();
            Assert.AreEqual(GameState.CatchSequence, _flow.State, $"catch failed: {_flow.LastCatchOutcome}");
            yield return null;
        }

        private IEnumerator PlaySuccessfulAttempt()
        {
            Assert.AreEqual(GameState.TrackBall, _flow.State);
            yield return CatchCleanly();
            _flow.SimulateActionPress(); // skip the catch sequence
            yield return WaitUntil(() => _flow.State == GameState.Throw);
            yield return null;
            yield return WaitUntil(() => Mathf.Abs(_throw.Needle - _throw.ZoneCenter) < _throw.ZoneWidth * 0.3f);
            _flow.SimulateActionPress();
            yield return WaitUntil(() => _flow.State == GameState.MissionComplete);
        }

        private static IEnumerator WaitUntil(Func<bool> condition)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > StepTimeoutSeconds)
                    Assert.Fail("Timed out waiting for condition.");
                yield return null;
            }
        }
    }
}
