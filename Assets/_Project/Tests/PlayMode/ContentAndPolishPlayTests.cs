using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SayHeyDash.Audio;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using SayHeyDash.Platform;
using SayHeyDash.Replay;
using SayHeyDash.Settings;
using SayHeyDash.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Milestone 4 in the real Main scene: settings menu + persistence, best score, the animated
    /// replay, touch controls, browser pause requests and the crowd ducking under jingles.
    /// Any logged error fails the test.
    /// </summary>
    public sealed class ContentAndPolishPlayTests
    {
        private const float TimeScale = 4f;
        private const float StepTimeoutSeconds = 20f;

        private GameFlowController _flow;
        private OutfielderController _outfielder;
        private BallFlight _ball;
        private CatchSystem _catch;
        private ThrowSystem _throw;
        private AudioService _audio;
        private SettingsService _settings;
        private SettingsMenuView _menu;
        private MemorySettingsStore _store;

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
            _audio = Object.FindFirstObjectByType<AudioService>();
            _settings = Object.FindFirstObjectByType<SettingsService>();
            _menu = Object.FindFirstObjectByType<SettingsMenuView>();
            Assert.IsNotNull(_settings, "Main scene has no SettingsService - run Say Hey Dash > Build Main Scene.");
            Assert.IsNotNull(_menu);
            _store = new MemorySettingsStore();
            _settings.UseStore(_store);
            Time.timeScale = TimeScale;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        // ------------------------------------------------------------------ settings

        [UnityTest]
        public IEnumerator SettingsMenu_ChangesAndSavesEveryOption()
        {
            var title = Object.FindFirstObjectByType<TitleScreenView>();
            title.OnSettingsClicked();
            yield return null;
            Assert.IsTrue(_menu.IsShown);
            Assert.AreEqual("SOUND: ON", Button(_menu, "SoundButton").Label);
            Assert.AreEqual("MUSIC: 100%", Button(_menu, "MusicButton").Label);

            _menu.OnMusicClicked();
            _menu.OnEffectsClicked();
            _menu.OnEffectsClicked();
            _menu.OnReducedMotionClicked();
            _menu.OnAssistedClicked();
            _menu.OnSoundClicked();
            yield return null;

            Assert.AreEqual("MUSIC: 80%", Button(_menu, "MusicButton").Label);
            Assert.AreEqual("EFFECTS: 60%", Button(_menu, "EffectsButton").Label);
            Assert.AreEqual("REDUCED MOTION: ON", Button(_menu, "ReducedMotionButton").Label);
            Assert.AreEqual("ASSISTED MODE: ON", Button(_menu, "AssistedButton").Label);
            Assert.AreEqual("SOUND: OFF", Button(_menu, "SoundButton").Label);
            Assert.AreEqual(0.8f, _audio.MusicVolume, 1e-4f);
            Assert.AreEqual(0.6f, _audio.EffectsVolume, 1e-4f);
            Assert.IsTrue(_flow.ReducedMotion);
            Assert.IsTrue(_flow.AssistedMode);
            Assert.IsTrue(_audio.Muted);

            SettingsData saved = SettingsSerializer.Load(_store, false);
            Assert.AreEqual(4, saved.MusicLevel);
            Assert.AreEqual(3, saved.EffectsLevel);
            Assert.IsTrue(saved.ReducedMotion && saved.AssistedMode && saved.Muted, "every change is saved");

            _menu.OnBackClicked();
            yield return null;
            Assert.IsFalse(_menu.IsShown);
            Assert.AreEqual("ASSISTED MODE: ON", Button(title, "AssistedButton").Label, "title toggles follow the menu");
        }

        [UnityTest]
        public IEnumerator SavedSettings_AreRestored_OnTheNextVisit()
        {
            var store = new MemorySettingsStore();
            SettingsSerializer.Save(store, new SettingsData
            {
                Muted = true, MusicLevel = 1, EffectsLevel = 2, ReducedMotion = true, AssistedMode = true, BestScore = 870,
            });
            _settings.UseStore(store);
            yield return null;
            Assert.IsTrue(_audio.Muted);
            Assert.AreEqual(0.2f, _audio.MusicVolume, 1e-4f);
            Assert.AreEqual(0.4f, _audio.EffectsVolume, 1e-4f);
            Assert.IsTrue(_flow.ReducedMotion);
            Assert.IsTrue(_flow.AssistedMode);
            Assert.AreEqual(870, _settings.BestScore);
        }

        [UnityTest]
        public IEnumerator SettingsFromPause_LocksAssistedMode_AndClosesWhenPlayResumes()
        {
            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;
            _flow.Pause();
            yield return null;
            Object.FindFirstObjectByType<PauseMenuView>().OnSettingsClicked();
            yield return null;
            Assert.IsTrue(_menu.IsShown);
            Assert.IsFalse(Button(_menu, "AssistedButton").Button.interactable, "assisted mode changes between attempts only");

            _menu.OnReducedMotionClicked();
            Assert.IsTrue(_flow.ReducedMotion, "reduced motion applies at once, even mid-attempt");

            _flow.TogglePause(); // Esc
            yield return null;
            Assert.IsFalse(_menu.IsShown, "resuming play closes the overlay");
            Assert.IsFalse(_flow.IsPaused);
        }

        // ------------------------------------------------------------------ scoring

        [UnityTest]
        public IEnumerator FirstWin_IsANewBest_AndShowsTheNextRankOrTopRank()
        {
            yield return StartAndWin();
            var complete = Object.FindFirstObjectByType<MissionCompleteView>();
            yield return WaitUntil(() => complete.TallyFinished);
            Assert.AreEqual(_flow.LastScore.Total, _settings.BestScore);
            Assert.IsTrue(_settings.LastScoreWasNewBest);
            Assert.AreEqual("NEW BEST!", FindText(complete, "Best").text);
            string next = FindText(complete, "NextRank").text;
            Assert.IsTrue(next == "TOP RANK!" || next.Contains(" AT "), next);
            Assert.AreEqual(_flow.LastScore.Total, SettingsSerializer.Load(_store, false).BestScore, "best score saved");
        }

        // ------------------------------------------------------------------ replay

        [UnityTest]
        public IEnumerator Replay_StepsThroughTheRealPlay_AndEndsWithTheRunnerHeldAtThird()
        {
            yield return StartAndWin();
            _flow.ShowReplayMode();
            yield return null;
            var view = Object.FindFirstObjectByType<ReplayScreenView>();
            var director = Object.FindFirstObjectByType<ReplayDirector>();
            Assert.IsTrue(view.IsShown);
            Assert.IsTrue(director.IsPlaying);
            Assert.AreEqual(0, director.Step);
            StringAssert.StartsWith("1/6", FindText(view, "Step").text);
            Assert.IsNotEmpty(FindText(view, "Narration").text);
            Assert.IsFalse(Marker(view, "Ball").gameObject.activeSelf, "no ball before the hit");

            int sounds = _audio.PlayCount;
            for (int i = 1; i < ReplayTimeline.StepCount; i++)
            {
                view.OnNextClicked();
                yield return null;
                Assert.AreEqual(i, director.Step);
            }
            Assert.Greater(_audio.PlayCount, sounds, "hit / catch / throw / cheer sounds");
            Assert.IsTrue(director.IsAtLastStep);
            Assert.IsFalse(Button(view, "NextButton").Button.interactable);
            StringAssert.StartsWith("6/6", FindText(view, "Step").text);

            // Final picture: lead runner on third base, never home.
            RectTransform runner = Marker(view, "LeadRunner");
            RectTransform diagram = (RectTransform)runner.parent;
            float k = diagram.rect.width / ReplayTimeline.DiagramWidth;
            Vector2 px = new Vector2(runner.anchoredPosition.x / k, -runner.anchoredPosition.y / k);
            Assert.Less(Vector2.Distance(px, ReplayTimeline.ThirdBase), 1.01f, $"runner at {px}");
            Assert.IsFalse(Marker(view, "Batter").gameObject.activeSelf, "batter is out");

            view.OnWatchAgainClicked();
            yield return null;
            Assert.AreEqual(0, director.Step);
            Assert.IsTrue(Button(view, "NextButton").Button.interactable);

            view.OnBackClicked();
            yield return null;
            Assert.AreEqual(GameState.Title, _flow.State);
            Assert.IsFalse(director.IsPlaying, "replay stops when the screen closes");
        }

        [UnityTest]
        public IEnumerator Replay_PlaysOnItsOwn()
        {
            yield return StartAndWin();
            _flow.ShowReplayMode();
            var director = Object.FindFirstObjectByType<ReplayDirector>();
            // Unscaled time: the first step lasts a few real seconds.
            yield return WaitUntil(() => director.Step >= 1);
            Assert.AreEqual(1, director.Step);
        }

        // ------------------------------------------------------------------ mobile + browser

        [UnityTest]
        public IEnumerator TouchControls_ShowOnlyInTouchMode_AndNameTheAction()
        {
            var touch = Object.FindFirstObjectByType<TouchControls>();
            var hud = Object.FindFirstObjectByType<HudView>();
            GameObject stick = FindChild(hud, "OnScreenStickBase");
            GameObject button = FindChild(hud, "OnScreenActionButton");
            Assert.IsFalse(touch.TouchMode, "no touchscreen in the test runner");
            Assert.IsFalse(stick.activeSelf, "desktop: no on-screen controls over the field");
            Assert.IsFalse(button.activeSelf);

            touch.SetTouchMode(true);
            Assert.IsTrue(stick.activeSelf && button.activeSelf);
            _flow.StartMission();
            yield return null;
            StringAssert.Contains("TAP", FindText(Object.FindFirstObjectByType<BriefingScreenView>(), "Controls").text,
                "touch instructions on the briefing");
            _flow.BeginMission();
            yield return null;
            Text label = FindText(hud, "OnScreenActionLabel");
            Assert.AreEqual("CATCH", label.text);
            yield return CatchCleanly();
            _flow.SimulateActionPress();
            yield return WaitUntil(() => _flow.State == GameState.Throw);
            Assert.AreEqual("THROW", label.text);
            touch.SetTouchMode(false);
        }

        [UnityTest]
        public IEnumerator BrowserPause_PausesLivePlay_Only()
        {
            var browser = Object.FindFirstObjectByType<BrowserEvents>();
            Assert.AreEqual(BrowserEvents.GameObjectName, browser.gameObject.name, "the web page addresses it by name");
            browser.OnBrowserPause();
            Assert.IsFalse(_flow.IsPaused, "title screen: nothing to pause");

            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;
            browser.OnBrowserPause();
            Assert.IsTrue(_flow.IsPaused, "tab hidden / focus lost mid-play pauses the game");
            _flow.Resume();
        }

        [UnityTest]
        public IEnumerator CrowdBed_DucksUnderTheResultJingle()
        {
            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;
            Assert.IsTrue(_audio.AmbiencePlaying);
            Assert.IsFalse(_audio.AmbienceDucked);
            yield return WaitUntil(() => _flow.State == GameState.MissionFailed);
            Assert.IsTrue(_audio.AmbienceDucked, "fail jingle reads over the crowd");
        }

        [UnityTest]
        public IEnumerator PressStartBlink_StaysBelowThreeFlashesPerSecond()
        {
            var title = Object.FindFirstObjectByType<TitleScreenView>();
            BlinkingText blink = title.GetComponentInChildren<BlinkingText>(true);
            Assert.IsNotNull(blink);
            var field = typeof(BlinkingText).GetField("cyclesPerSecond", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.LessOrEqual((float)field.GetValue(blink), 3f, "photosensitivity limit");
            _flow.SetReducedMotion(true);
            yield return null;
            Assert.IsTrue(blink.Steady, "reduced motion stops the blink");
        }

        // ------------------------------------------------------------------ helpers

        private IEnumerator StartAndWin()
        {
            _flow.StartMission();
            yield return null;
            _flow.BeginMission();
            yield return null;
            yield return CatchCleanly();
            _flow.SimulateActionPress(); // skip the catch sequence
            yield return WaitUntil(() => _flow.State == GameState.Throw);
            yield return null;
            yield return WaitUntil(() => Mathf.Abs(_throw.Needle - _throw.ZoneCenter) < _throw.ZoneWidth * 0.3f);
            _flow.SimulateActionPress();
            yield return WaitUntil(() => _flow.State == GameState.MissionComplete);
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

        private static PixelButton Button(Component root, string name)
        {
            foreach (PixelButton b in root.GetComponentsInChildren<PixelButton>(true))
                if (b.name == name) return b;
            Assert.Fail($"{root.GetType().Name} has no button '{name}'");
            return null;
        }

        private static Text FindText(Component root, string name)
        {
            foreach (Text t in root.GetComponentsInChildren<Text>(true))
                if (t.name == name) return t;
            Assert.Fail($"{root.GetType().Name} has no Text '{name}'");
            return null;
        }

        private static RectTransform Marker(Component root, string name) => (RectTransform)FindChild(root, name).transform;

        private static GameObject FindChild(Component root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            Assert.Fail($"{root.GetType().Name} has no child '{name}'");
            return null;
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
