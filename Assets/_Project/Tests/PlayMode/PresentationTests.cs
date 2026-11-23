using System;
using System.Collections;
using NUnit.Framework;
using SayHeyDash.Audio;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using SayHeyDash.UI;
using SayHeyDash.Visuals;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Milestone 3 presentation in the real Main scene: exactly one screen per state (AC-07),
    /// the outfielder animation follows the play, the ball sits in the glove until the throw,
    /// and sound cues + mute work (AC-10). Any logged error fails the test.
    /// </summary>
    public sealed class PresentationTests
    {
        private const float TimeScale = 4f;
        private const float StepTimeoutSeconds = 20f;

        private GameFlowController _flow;
        private OutfielderController _outfielder;
        private OutfielderAnimator _animator;
        private BallFlight _ball;
        private CatchSystem _catch;
        private ThrowSystem _throw;
        private AudioService _audio;
        private ScreenView[] _views;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _flow = Object.FindFirstObjectByType<GameFlowController>();
            _outfielder = Object.FindFirstObjectByType<OutfielderController>();
            _animator = Object.FindFirstObjectByType<OutfielderAnimator>();
            _ball = Object.FindFirstObjectByType<BallFlight>();
            _catch = Object.FindFirstObjectByType<CatchSystem>();
            _throw = Object.FindFirstObjectByType<ThrowSystem>();
            _audio = Object.FindFirstObjectByType<AudioService>();
            _views = Object.FindObjectsByType<ScreenView>(FindObjectsSortMode.None);
            Assert.IsNotNull(_flow, "Main scene has no GameFlowController - run Say Hey Dash > Build Main Scene.");
            Assert.IsNotNull(_animator);
            Assert.IsNotNull(_audio);
            Assert.AreEqual(8, _views.Length, "Title, Briefing, HUD, Pause, Failed, Complete, Replay + Settings overlay");
            Time.timeScale = TimeScale;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator EachStateShowsExactlyItsScreen()
        {
            AssertShown(typeof(TitleScreenView));
            Assert.AreEqual(AudioCue.TitleMusic, _audio.CurrentMusic);

            _flow.StartMission();
            yield return null;
            AssertShown(typeof(BriefingScreenView));

            _flow.BeginMission();
            yield return null;
            AssertShown(typeof(HudView));

            _flow.Pause();
            yield return null;
            AssertShown(typeof(HudView), typeof(PauseMenuView));
            _flow.Resume();
            Time.timeScale = TimeScale;
            yield return null;
            AssertShown(typeof(HudView));

            yield return WaitUntil(() => _flow.State == GameState.MissionFailed); // no press -> ball drops
            yield return null;
            AssertShown(typeof(MissionFailedView));
            Assert.AreEqual(AudioCue.FailJingle, _audio.CurrentMusic);
            Text reason = FindText<MissionFailedView>("Reason");
            Assert.AreEqual("BALL MISSED", reason.text);

            _flow.Retry();
            yield return null;
            AssertShown(typeof(HudView));
            yield return PlaySuccessfulAttempt();
            yield return null;
            AssertShown(typeof(MissionCompleteView));
            var complete = Object.FindFirstObjectByType<MissionCompleteView>();
            yield return WaitUntil(() => complete.TallyFinished);
            Assert.AreEqual(_flow.LastScore.Total.ToString(), FindText<MissionCompleteView>("TotalValue").text);
            StringAssert.Contains("*", FindText<MissionCompleteView>("Rank").text, "rank shows stars, not colour alone");

            _flow.ShowReplayMode();
            yield return null;
            AssertShown(typeof(ReplayScreenView));
            _flow.BackToTitle();
            yield return null;
            AssertShown(typeof(TitleScreenView));
        }

        [UnityTest]
        public IEnumerator OutfielderAnimation_FollowsThePlay_AndBallStaysInTheGloveUntilTheThrow()
        {
            Assert.AreEqual("idle", _animator.CurrentClip.name);
            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;

            yield return CatchCleanly();
            Assert.AreEqual("catch", _animator.CurrentClip.name);
            Assert.IsFalse(BallRenderer().enabled, "ball is drawn in the glove art while held");

            yield return WaitUntil(() => _animator.CurrentClip.name == "spin" || _flow.State == GameState.Throw);
            yield return WaitUntil(() => _flow.State == GameState.Throw);
            yield return null;
            Assert.AreEqual("throwReady", _animator.CurrentClip.name);

            yield return WaitUntil(() => Mathf.Abs(_throw.Needle - _throw.ZoneCenter) < _throw.ZoneWidth * 0.3f);
            _flow.SimulateActionPress();
            yield return null;
            Assert.AreEqual("throwRelease", _animator.CurrentClip.name);
            Assert.IsTrue(BallRenderer().enabled, "thrown ball is visible");
            Assert.AreEqual(AudioCue.Throw, _audio.LastCue);
            Text feedback = FindText<HudView>("ThrowFeedback");
            Assert.IsNotEmpty(feedback.text, "throw result is shown in words");
        }

        [UnityTest]
        public IEnumerator Running_PlaysTheRunCycle_ThenIdlesWhenStopped()
        {
            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;
            Assert.AreEqual(AudioCue.BatCrack, _audio.LastCue, "crack of the bat on launch");
            Assert.AreEqual("idle", _animator.CurrentClip.name);

            _outfielder.MoveOverride = Vector2.left;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.Less(_outfielder.Velocity.x, 0f, "moving left");
            Assert.IsFalse(_outfielder.FacingRight);
            Assert.AreEqual("run", _animator.CurrentClip.name);

            _outfielder.MoveOverride = Vector2.zero;
            yield return WaitUntil(() => _outfielder.Velocity.sqrMagnitude < 0.0001f);
            yield return null;
            Assert.AreEqual("idle", _animator.CurrentClip.name);
            _outfielder.MoveOverride = null;
        }

        [UnityTest]
        public IEnumerator Mute_SilencesEverySource_AndUpdatesLabels()
        {
            var title = Object.FindFirstObjectByType<TitleScreenView>();
            PixelButton sound = FindButton(title, "SoundButton");
            Assert.AreEqual("SOUND: ON", sound.Label);

            title.OnSoundClicked();
            Assert.IsTrue(_audio.Muted);
            Assert.AreEqual("SOUND: OFF", sound.Label);
            foreach (AudioSource s in _audio.GetComponentsInChildren<AudioSource>())
                Assert.IsTrue(s.mute, $"{s.name} not muted");

            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;
            var hud = Object.FindFirstObjectByType<HudView>();
            Image icon = FindImage(hud, "SoundButton");
            Assert.AreEqual("icon_sound_off", icon.sprite.name);

            hud.OnSoundClicked();
            Assert.IsFalse(_audio.Muted);
            Assert.AreEqual("icon_sound_on", icon.sprite.name);
        }

        // ------------------------------------------------------------------ helpers

        private void AssertShown(params Type[] shown)
        {
            foreach (ScreenView v in _views)
            {
                bool expected = Array.IndexOf(shown, v.GetType()) >= 0;
                Assert.AreEqual(expected, v.IsShown, $"{v.GetType().Name} in {_flow.State} (paused: {_flow.IsPaused})");
            }
        }

        private SpriteRenderer BallRenderer()
        {
            foreach (SpriteRenderer r in _ball.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.name == "Ball") return r;
            Assert.Fail("Ball renderer not found");
            return null;
        }

        private static Text FindText<T>(string name) where T : ScreenView
        {
            var view = Object.FindFirstObjectByType<T>();
            foreach (Text t in view.GetComponentsInChildren<Text>(true))
                if (t.name == name) return t;
            Assert.Fail($"{typeof(T).Name} has no Text '{name}'");
            return null;
        }

        private static PixelButton FindButton(Component view, string name)
        {
            foreach (PixelButton b in view.GetComponentsInChildren<PixelButton>(true))
                if (b.name == name) return b;
            Assert.Fail($"{view.GetType().Name} has no button '{name}'");
            return null;
        }

        private static Image FindImage(Component view, string name)
        {
            foreach (Image i in view.GetComponentsInChildren<Image>(true))
                if (i.name == name) return i;
            Assert.Fail($"{view.GetType().Name} has no image '{name}'");
            return null;
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
