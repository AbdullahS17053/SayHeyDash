using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SayHeyDash.EditorTools.Handover
{
    /// <summary>
    /// Guided tour of the project in the Unity Editor (Milestone 5 handover). Opens the game scene,
    /// plays a short auto-piloted mission, then shows where gameplay numbers, text, branding,
    /// Player Settings, the web build and the tests live. Each step shows its caption as an
    /// Editor notification. Nothing is left changed: the one value it edits for the demo is restored.
    ///
    /// Menu: Say Hey Dash &gt; Handover &gt; Play Editor Tour (Stop Editor Tour to cancel).
    /// The handover video was recorded with Tools/Tutorial/record_tutorial.ps1, which starts the
    /// Editor with: -executeMethod SayHeyDash.EditorTools.Handover.EditorTour.RunFromCommandLine
    ///   -tourTimings &lt;file: "stepId seconds" per line&gt; -tourLog &lt;file&gt; -tourGo &lt;file&gt; -tourQuit -tourRecording
    /// The tour survives the domain reload of entering Play Mode (state lives in SessionState).
    /// </summary>
    [InitializeOnLoad]
    public static class EditorTour
    {
        public const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string TuningPath = "Assets/_Project/Data/Tuning/GameplayTuning.asset";
        private const string ContentPath = "Assets/_Project/Data/Content/MissionContent.asset";
        private const string AssistedPath = "Assets/_Project/Data/Tuning/Difficulty_Assisted.asset";
        private const string ScoringPath = "Assets/_Project/Data/Scoring/ScoringConfig.asset";
        private const string LogoPath = "Assets/_Project/Art/Branding/FastballFuel_Logo.png";
        private const string DemoProperty = "ballFlightDuration";
        private const float DemoValue = 4.2f;

        private const string KeyActive = "SHD.Tour.Active";
        private const string KeyStep = "SHD.Tour.Step";
        private const string KeyEntered = "SHD.Tour.Entered";
        private const string KeyStepStart = "SHD.Tour.StepStart";
        private const string KeyLaunch = "SHD.Tour.Launch";
        private const string KeyTimings = "SHD.Tour.Timings";
        private const string KeyLog = "SHD.Tour.Log";
        private const string KeyGo = "SHD.Tour.Go";
        private const string KeyQuit = "SHD.Tour.Quit";
        private const string KeyRecording = "SHD.Tour.Recording";
        private const string KeyOriginal = "SHD.Tour.Original";

        private sealed class Step
        {
            public string Id;
            public string Caption;
            public float Seconds;              // used when no timings file is given
            public Func<bool> Enter;           // returns false to be retried next frame
            public Action<float> Tick;         // seconds since the step started
        }

        private static readonly Step[] Steps =
        {
            new Step { Id = "overview", Seconds = 7, Caption = "Everything that belongs to the game is in Assets/_Project",
                Enter = () => { FocusProject(); Ping("Assets/_Project"); return true; } },
            new Step { Id = "scene", Seconds = 7, Caption = "The whole game is one scene: Scenes/Main.unity",
                Enter = () => { OpenMainScene(); Ping(MainScenePath); return true; } },
            new Step { Id = "play", Seconds = 6, Caption = "Press Play (top centre) to run the game in the Editor",
                Enter = () => { if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode(); return true; } },
            new Step { Id = "controls", Seconds = 16, Caption = "Arrows / WASD run  ·  Space catches and throws  ·  Esc pauses",
                Enter = Autopilot.Begin, Tick = Autopilot.Tick },
            new Step { Id = "data", Seconds = 9, Caption = "Gameplay numbers: Data/Tuning/GameplayTuning (edit them in the Inspector)",
                Enter = () =>
                {
                    if (EditorApplication.isPlaying)
                    {
                        Autopilot.Release();
                        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
                        return false;
                    }
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
                    Select(TuningPath); return true;
                } },
            new Step { Id = "edit", Seconds = 10, Caption = "Example: Ball Flight Duration 3.6 → 4.2 gives the player more time",
                Enter = () => { Select(TuningPath); return true; }, Tick = DemoEdit },
            new Step { Id = "text", Seconds = 8, Caption = "Every on-screen word, incl. the history replay: Data/Content/MissionContent",
                Enter = () => { Select(ContentPath); return true; } },
            new Step { Id = "more", Seconds = 8, Caption = "Assisted mode: Data/Tuning/Difficulty_Assisted  ·  Score formula: Data/Scoring/ScoringConfig",
                Enter = () => { Select(AssistedPath); return true; },
                Tick = t => { if (t > 4f && Selection.activeObject != AssetDatabase.LoadMainAssetAtPath(ScoringPath)) Select(ScoringPath); } },
            new Step { Id = "brand", Seconds = 10, Caption = "Client logo: Art/Branding  →  Say Hey Dash ▸ Branding ▸ Rebuild Logo Files + Apply",
                Enter = () => { Select(LogoPath); return true; } },
            new Step { Id = "player", Seconds = 8, Caption = "Edit ▸ Project Settings ▸ Player: company, product name, icon, splash screen",
                Enter = () => { SettingsService.OpenProjectSettings("Project/Player"); return true; } },
            new Step { Id = "build", Seconds = 10, Caption = "Web build: Say Hey Dash ▸ Build ▸ WebGL (Release)  →  Builds/WebGL",
                Enter = () =>
                {
                    CloseWindows("ProjectSettingsWindow");
                    EditorApplication.ExecuteMenuItem("File/Build Profiles");
                    return true;
                } },
            new Step { Id = "tests", Seconds = 9, Caption = "Window ▸ General ▸ Test Runner: run EditMode + PlayMode tests before shipping",
                Enter = () =>
                {
                    CloseWindows("BuildProfileWindow", "BuildPlayerWindow");
                    EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                    return true;
                } },
        };

        public static IEnumerable<string> StepIds => Steps.Select(s => s.Id);

        static EditorTour()
        {
            EditorApplication.update -= Tick;
            if (SessionState.GetBool(KeyActive, false)) EditorApplication.update += Tick;
        }

        // ------------------------------------------------------------------ entry points

        [MenuItem("Say Hey Dash/Handover/Play Editor Tour", priority = 60)]
        public static void PlayFromMenu() => Start(timingsFile: null, logFile: null, goFile: null, quit: false, recording: false);

        [MenuItem("Say Hey Dash/Handover/Stop Editor Tour", priority = 61)]
        public static void StopFromMenu() => Finish(aborted: true);

        public static void RunFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }
            Start(Arg("-tourTimings"), Arg("-tourLog"), Arg("-tourGo"), args.Contains("-tourQuit"), args.Contains("-tourRecording"));
        }

        private static void Start(string timingsFile, string logFile, string goFile, bool quit, bool recording)
        {
            SessionState.SetBool(KeyActive, true);
            SessionState.SetInt(KeyStep, 0);
            SessionState.SetInt(KeyEntered, -1);
            SessionState.SetString(KeyLaunch, NowMs().ToString(CultureInfo.InvariantCulture));
            SessionState.SetString(KeyTimings, timingsFile != null && File.Exists(timingsFile) ? File.ReadAllText(timingsFile) : "");
            SessionState.SetString(KeyLog, logFile ?? "");
            SessionState.SetString(KeyGo, goFile ?? "");
            SessionState.SetBool(KeyQuit, quit);
            SessionState.SetBool(KeyRecording, recording);
            if (!string.IsNullOrEmpty(logFile)) File.WriteAllText(logFile, "");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        // ------------------------------------------------------------------ state machine

        private static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            int index = SessionState.GetInt(KeyStep, 0);
            int entered = SessionState.GetInt(KeyEntered, -1);

            if (index == 0 && entered < 0 && !ReadyToBegin()) return;
            if (index >= Steps.Length) { Finish(aborted: false); return; }

            Step step = Steps[index];
            if (entered != index)
            {
                bool ok;
                try { ok = step.Enter == null || step.Enter(); }
                catch (Exception e) { Debug.LogException(e); ok = true; }
                if (!ok) return;
                SessionState.SetInt(KeyEntered, index);
                SessionState.SetString(KeyStepStart, NowMs().ToString(CultureInfo.InvariantCulture));
                Log(step.Id);
                if (!SessionState.GetBool(KeyRecording, false)) Notify(step.Caption);
            }

            float elapsed = (NowMs() - long.Parse(SessionState.GetString(KeyStepStart, "0"), CultureInfo.InvariantCulture)) / 1000f;
            try { step.Tick?.Invoke(elapsed); }
            catch (Exception e) { Debug.LogException(e); }

            if (elapsed >= Duration(step)) SessionState.SetInt(KeyStep, index + 1);
        }

        private static bool ReadyToBegin()
        {
            string go = SessionState.GetString(KeyGo, "");
            long launch = long.Parse(SessionState.GetString(KeyLaunch, "0"), CultureInfo.InvariantCulture);
            float sinceLaunch = (NowMs() - launch) / 1000f;
            if (sinceLaunch < 3f) return false;                       // let the Editor settle
            if (string.IsNullOrEmpty(go)) return true;
            return File.Exists(go) || sinceLaunch > 120f;              // recorder ready (or give up waiting)
        }

        private static float Duration(Step step)
        {
            foreach (string line in SessionState.GetString(KeyTimings, "").Split('\n'))
            {
                string[] parts = line.Trim().Split(' ');
                if (parts.Length == 2 && parts[0] == step.Id &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
                    return Mathf.Max(s, step.Id == "controls" ? 13f : 0f);
            }
            return step.Seconds;
        }

        private static void Finish(bool aborted)
        {
            RestoreDemoValue();
            Autopilot.Release();
            bool quit = SessionState.GetBool(KeyQuit, false);
            SessionState.SetBool(KeyActive, false);
            EditorApplication.update -= Tick;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            CloseWindows("TestRunnerWindow", "ProjectSettingsWindow", "BuildProfileWindow", "BuildPlayerWindow");
            Log(aborted ? "aborted" : "end");
            if (!SessionState.GetBool(KeyRecording, false)) Notify(aborted ? "Tour stopped" : "End of tour - see Docs/Developer_Guide.md");
            if (quit) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------------ step helpers

        private static void DemoEdit(float t)
        {
            var tuning = AssetDatabase.LoadMainAssetAtPath(TuningPath);
            if (tuning == null) return;
            var so = new SerializedObject(tuning);
            SerializedProperty prop = so.FindProperty(DemoProperty);
            if (prop == null) return;

            float end = Duration(Steps.First(s => s.Id == "edit"));
            if (t > 2.5f && t < end - 2.5f && string.IsNullOrEmpty(SessionState.GetString(KeyOriginal, "")))
            {
                SessionState.SetString(KeyOriginal, prop.floatValue.ToString("R", CultureInfo.InvariantCulture));
                prop.floatValue = DemoValue;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (t >= end - 2.5f) RestoreDemoValue();
        }

        private static void RestoreDemoValue()
        {
            string original = SessionState.GetString(KeyOriginal, "");
            if (string.IsNullOrEmpty(original)) return;
            var tuning = AssetDatabase.LoadMainAssetAtPath(TuningPath);
            if (tuning != null)
            {
                var so = new SerializedObject(tuning);
                so.FindProperty(DemoProperty).floatValue = float.Parse(original, CultureInfo.InvariantCulture);
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(tuning);
            }
            SessionState.EraseString(KeyOriginal);
        }

        private static void OpenMainScene()
        {
            if (EditorSceneManager.GetActiveScene().path != MainScenePath)
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        private static void FocusProject() => EditorUtility.FocusProjectWindow();

        private static void Ping(string path)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) return;
            FocusProject();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void Select(string path) => Ping(path);

        private static void CloseWindows(params string[] typeNames)
        {
            foreach (EditorWindow w in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (w != null && typeNames.Contains(w.GetType().Name)) w.Close();
        }

        private static void Notify(string text)
        {
            EditorWindow target = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.focusedWindow;
            if (target != null) target.ShowNotification(new GUIContent(text), 4.0);
        }

        private static void Log(string id)
        {
            string path = SessionState.GetString(KeyLog, "");
            if (!string.IsNullOrEmpty(path)) File.AppendAllText(path, $"{id} {NowMs()}\n");
        }

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // ------------------------------------------------------------------ demo player

        /// <summary>Plays one clean attempt (run to the ring, catch on time, throw in the zone).</summary>
        private static class Autopilot
        {
            private static GameFlowController _flow;
            private static OutfielderController _outfielder;
            private static CatchSystem _catch;
            private static BallFlight _ball;
            private static ThrowSystem _throw;
            private static bool _caught, _thrown;

            public static bool Begin()
            {
                if (!EditorApplication.isPlaying) return false;
                _flow = Object.FindFirstObjectByType<GameFlowController>();
                _outfielder = Object.FindFirstObjectByType<OutfielderController>();
                _catch = Object.FindFirstObjectByType<CatchSystem>();
                _ball = Object.FindFirstObjectByType<BallFlight>();
                _throw = Object.FindFirstObjectByType<ThrowSystem>();
                if (_flow == null || _flow.State == GameState.None) return false;
                _caught = _thrown = false;
                if (_flow.State == GameState.Title) _flow.StartMission();
                return true;
            }

            public static void Tick(float t)
            {
                if (_flow == null || !EditorApplication.isPlaying) return;
                switch (_flow.State)
                {
                    case GameState.MissionBriefing:
                        if (t > 2.0f) _flow.BeginMission();
                        break;
                    case GameState.TrackBall:
                    case GameState.CatchWindow:
                        Vector2 target = _ball.Trajectory.Landing + new Vector2(0.1f, 0f);
                        Vector2 delta = target - _outfielder.Position;
                        _outfielder.MoveOverride = delta.magnitude > 0.08f ? delta.normalized : Vector2.zero;
                        if (!_caught && _catch.Cue == CatchCue.Now && _ball.Elapsed >= _catch.PerfectTime - 0.03f)
                        {
                            _caught = true;
                            _flow.SimulateActionPress();
                        }
                        break;
                    case GameState.Throw:
                        _outfielder.MoveOverride = null;
                        if (!_thrown && _throw.Elapsed > 0.8f && Mathf.Abs(_throw.Needle - _throw.ZoneCenter) < _throw.ZoneWidth * 0.3f)
                        {
                            _thrown = true;
                            _flow.SimulateActionPress();
                        }
                        break;
                    default:
                        _outfielder.MoveOverride = null;
                        break;
                }
            }

            public static void Release()
            {
                if (_outfielder != null) _outfielder.MoveOverride = null;
                _flow = null;
            }
        }
    }
}
