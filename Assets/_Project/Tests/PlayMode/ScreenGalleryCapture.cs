using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using SayHeyDash.Replay;
using SayHeyDash.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Not a regular test: renders every screen of the real Main scene to PNG (Docs/img/m4_screens)
    /// for art review and client reports. Run it on purpose:
    /// -runTests -testPlatform PlayMode -testFilter SayHeyDash.Tests.ScreenGalleryCapture
    /// Needs a graphics device (no -nographics).
    /// </summary>
    [Explicit("Writes review screenshots; run on purpose with -testFilter.")]
    public sealed class ScreenGalleryCapture
    {
        private const int Width = 1280, Height = 720;
        private string _dir;
        private Camera _cam;
        private Canvas _canvas;
        private RenderTexture _rt;

        [UnityTest]
        public IEnumerator CaptureAllScreens()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device (-nographics).");

            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var outfielder = Object.FindFirstObjectByType<OutfielderController>();
            var ball = Object.FindFirstObjectByType<BallFlight>();
            var catchSystem = Object.FindFirstObjectByType<CatchSystem>();
            var throwSystem = Object.FindFirstObjectByType<ThrowSystem>();
            Setup();
            try
            {
                yield return Wait(0.6f);
                Capture("01_title");
                Object.FindFirstObjectByType<TitleScreenView>().OnCreditsClicked();
                yield return null;
                Capture("01b_credits");
                Object.FindFirstObjectByType<TitleScreenView>().OnCreditsBackClicked();
                Object.FindFirstObjectByType<TitleScreenView>().OnSettingsClicked();
                yield return null;
                Capture("01c_settings");
                Object.FindFirstObjectByType<SettingsMenuView>().OnBackClicked();

                // Touch-device look (on-screen stick + CATCH/THROW button) for the rest of the run.
                Object.FindFirstObjectByType<TouchControls>().SetTouchMode(true);
                flow.StartMission();
                yield return Wait(0.2f);
                Capture("02_mission_objective_touch");

                flow.BeginMission();
                yield return Wait(0.6f);
                Capture("03_gameplay_track_touch");
                outfielder.TeleportTo(ball.Trajectory.Landing + new Vector2(-0.2f, 0f));
                while (catchSystem.Cue != CatchCue.Ready) yield return null;
                yield return null;
                Capture("03b_gameplay_get_ready");
                while (ball.Elapsed < catchSystem.PerfectTime - 0.02f) yield return null;
                flow.SimulateActionPress();
                yield return Wait(0.12f);
                Capture("04_catch_sequence");
                yield return Wait(0.55f);
                Capture("04b_spin");
                while (flow.State != GameState.Throw) yield return null;
                yield return Wait(0.3f);
                Capture("03c_throw_meter");
                while (Mathf.Abs(throwSystem.Needle - throwSystem.ZoneCenter) > throwSystem.ZoneWidth * 0.1f) yield return null;
                flow.SimulateActionPress();
                yield return Wait(0.1f);
                Capture("03d_throw_release");
                while (flow.State != GameState.MissionComplete) yield return null;
                var complete = Object.FindFirstObjectByType<MissionCompleteView>();
                while (!complete.TallyFinished) yield return null;
                yield return null;
                Capture("06_mission_complete");

                flow.ShowReplayMode();
                yield return null;
                var replay = Object.FindFirstObjectByType<ReplayScreenView>();
                var director = Object.FindFirstObjectByType<ReplayDirector>();
                Capture("07a_replay_setting");
                replay.OnNextClicked();                                     // the drive, mid-flight
                while (director.StepProgress < 0.55f) yield return null;
                Capture("07b_replay_drive");
                replay.OnNextClicked();
                replay.OnNextClicked();                                     // the throw, mid-way
                while (director.StepProgress < 0.5f) yield return null;
                Capture("07c_replay_throw");
                replay.OnNextClicked();
                replay.OnNextClicked();
                yield return null;
                Capture("07d_replay_why_it_matters");
                flow.BackToTitle();
                yield return null;
                flow.StartMission();
                yield return null;
                flow.BeginMission();
                yield return Wait(1f);
                flow.Pause();
                yield return null;
                Capture("pause_menu");
                Object.FindFirstObjectByType<PauseMenuView>().OnSettingsClicked();
                yield return null;
                Capture("pause_settings");
                flow.Resume();
                while (flow.State != GameState.MissionFailed) yield return null;
                yield return null;
                Capture("05_mission_failed");
            }
            finally
            {
                Teardown();
            }
        }

        /// <summary>M5: the client-logo splash shown after the web page's start click (Docs/img/m5_screens).</summary>
        [UnityTest]
        public IEnumerator CaptureBrandSplash()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device (-nographics).");

            Platform.BrandSplash.ResetForTests();
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
            Setup("m5_screens");
            var splash = Object.FindFirstObjectByType<BrandSplashView>();
            var splashCanvas = splash.GetComponent<Canvas>();
            splashCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            splashCanvas.worldCamera = _cam;
            splashCanvas.planeDistance = 0.5f;
            splashCanvas.sortingOrder = 2000; // above the game UI canvas (1000 here)
            try
            {
                yield return Wait(0.6f);
                Capture("00a_title_before_click");
                Object.FindFirstObjectByType<Platform.BrowserEvents>().OnBrowserStart();
                float start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - start < 0.2f) yield return null;
                Capture("00b_splash_fade_in");
                while (Time.realtimeSinceStartup - start < 1.5f) yield return null;
                Capture("00c_splash_hold");
                while (splash.IsShowing) yield return null;
                Capture("00d_after_splash");
            }
            finally
            {
                Teardown();
            }
        }

        private void Setup(string folder = "m4_screens")
        {
            _dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "img", folder));
            Directory.CreateDirectory(_dir);
            _cam = Camera.main;
            foreach (Behaviour b in _cam.GetComponents<Behaviour>())
                if (b.GetType().Name == "PixelPerfectCamera") b.enabled = false; // render straight at 1280x720
            _cam.orthographicSize = 180f / 32f;
            _rt = new RenderTexture(Width, Height, 24);
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && c.name == "UI") _canvas = c; // not the M5 "BrandSplash" canvas
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _cam;
            _canvas.planeDistance = 1f;
            _canvas.sortingOrder = 1000; // above every sprite in the 2D renderer
        }

        private void Teardown()
        {
            if (_cam != null) _cam.targetTexture = null;
            if (_rt != null) _rt.Release();
            Time.timeScale = 1f;
        }

        private void Capture(string name)
        {
            _cam.targetTexture = _rt;
            Canvas.ForceUpdateCanvases();
            _cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            _cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }
    }
}
