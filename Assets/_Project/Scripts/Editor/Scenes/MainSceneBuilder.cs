using System.IO;
using System.Linq;
using SayHeyDash.Controls;
using SayHeyDash.Data;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using SayHeyDash.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayHeyDash.EditorTools.Scenes
{
    /// <summary>
    /// Generates the single gameplay scene (Main.unity) and any missing data assets from code,
    /// so the scene is reproducible and every reference is wired the same way each time.
    /// Existing data assets are NEVER overwritten - tuning/text edits survive a rebuild.
    /// Menu: Say Hey Dash > Build Main Scene.
    /// </summary>
    public static class MainSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        public const string TuningPath = "Assets/_Project/Data/Tuning/GameplayTuning.asset";
        public const string DefaultDifficultyPath = "Assets/_Project/Data/Tuning/Difficulty_Default.asset";
        public const string AssistedDifficultyPath = "Assets/_Project/Data/Tuning/Difficulty_Assisted.asset";
        public const string ScoringPath = "Assets/_Project/Data/Scoring/ScoringConfig.asset";
        public const string ContentPath = "Assets/_Project/Data/Content/MissionContent.asset";

        private const string SpriteFolder = "Assets/_Project/Art/Sprites/Prototype/";
        private const string ActionsPath = "Assets/_Project/Input/SayHeyDashControls.inputactions";
        private const int ReferenceWidth = 320;
        private const int ReferenceHeight = 180;
        private const int PixelsPerUnit = 16;

        private static readonly Color32 Midnight = new Color32(0x17, 0x15, 0x2F, 0xFF);
        private static readonly Color32 Yellow = new Color32(0xF6, 0xCE, 0x4B, 0xFF);
        private static readonly Color32 Orange = new Color32(0xF3, 0x8B, 0x37, 0xFF);
        private static readonly Color32 Green = new Color32(0x3C, 0x76, 0x59, 0xFF);
        private static readonly Color32 OffWhite = new Color32(0xF5, 0xF1, 0xD9, 0xFF);
        private static readonly Color32 Dim = new Color32(0x17, 0x15, 0x2F, 0xE0);

        private static Font _font;
        private static int _errors;

        [MenuItem("Say Hey Dash/Build Main Scene", priority = 0)]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (Build()) EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Batch mode: -executeMethod SayHeyDash.EditorTools.Scenes.MainSceneBuilder.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build()
        {
            _errors = 0;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Data assets first (created only if missing), then the scene. NewScene(Single)
            // unloads unused assets, so everything is (re)loaded after it.
            EnsureDataAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var tuning = AssetDatabase.LoadAssetAtPath<GameplayTuning>(TuningPath);
            var defaultDifficulty = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(DefaultDifficultyPath);
            var assistedDifficulty = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(AssistedDifficultyPath);
            var scoring = AssetDatabase.LoadAssetAtPath<ScoringConfig>(ScoringPath);
            var content = AssetDatabase.LoadAssetAtPath<MissionContent>(ContentPath);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            Sprite field = LoadSprite("proto_field_background.png");
            Sprite outfielderSprite = LoadSprite("proto_outfielder_idle.png");
            Sprite ballSprite = LoadSprite("proto_ball.png");
            Sprite shadowSprite = LoadSprite("proto_ball_shadow.png");
            Sprite ringDashed = LoadSprite("proto_landing_ring_dashed.png");
            Sprite ringSolid = LoadSprite("proto_landing_ring_solid.png");
            if (tuning == null || defaultDifficulty == null || assistedDifficulty == null || scoring == null ||
                content == null || actions == null || field == null || outfielderSprite == null ||
                ballSprite == null || shadowSprite == null || ringDashed == null || ringSolid == null)
            {
                Debug.LogError("[MainSceneBuilder] Missing data assets, input actions or sprites " +
                               "(run Tools/ArtGen/generate_placeholder_art.py). Scene NOT saved.");
                return false;
            }

            // ---------------------------------------------------------------- world
            CreateCamera();
            CreateSprite("FieldBackground", field, Vector3.zero, -100, null);

            var indicatorGo = new GameObject("LandingIndicator");
            SpriteRenderer ring = CreateSprite("Ring", ringDashed, Vector3.zero, 4, indicatorGo.transform);
            ring.enabled = false;
            var indicator = indicatorGo.AddComponent<LandingIndicator>();
            Set(indicator, "ring", ring);
            Set(indicator, "dashedSprite", ringDashed);
            Set(indicator, "solidSprite", ringSolid);

            var outfielderGo = new GameObject("Outfielder");
            outfielderGo.transform.position = tuning.playerStartPosition;
            SpriteRenderer body = CreateSprite("Body", outfielderSprite, Vector3.zero, 10, outfielderGo.transform);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0f); // root = feet

            var ballGo = new GameObject("Ball");
            SpriteRenderer shadow = CreateSprite("Shadow", shadowSprite, Vector3.zero, 5, ballGo.transform);
            SpriteRenderer ballRenderer = CreateSprite("Ball", ballSprite, Vector3.zero, 20, ballGo.transform);
            shadow.enabled = false;
            ballRenderer.enabled = false;

            // ---------------------------------------------------------------- systems
            var systems = new GameObject("Systems");
            var gameInput = systems.AddComponent<GameInput>();
            Set(gameInput, "actions", actions);

            var outfielder = outfielderGo.AddComponent<OutfielderController>();
            Set(outfielder, "input", gameInput);
            Set(outfielder, "tuning", tuning);
            Set(outfielder, "body", body);

            var ballFlight = ballGo.AddComponent<BallFlight>();
            Set(ballFlight, "tuning", tuning);
            Set(ballFlight, "ball", ballRenderer);
            Set(ballFlight, "shadow", shadow);

            var catchSystem = systems.AddComponent<CatchSystem>();
            Set(catchSystem, "tuning", tuning);
            Set(catchSystem, "ball", ballFlight);
            Set(catchSystem, "outfielder", outfielder);
            Set(catchSystem, "landingIndicator", indicator);

            var throwSystem = systems.AddComponent<ThrowSystem>();
            Set(throwSystem, "tuning", tuning);

            var flow = systems.AddComponent<GameFlowController>();
            Set(flow, "input", gameInput);
            Set(flow, "outfielder", outfielder);
            Set(flow, "ball", ballFlight);
            Set(flow, "catchSystem", catchSystem);
            Set(flow, "throwSystem", throwSystem);
            Set(flow, "tuning", tuning);
            Set(flow, "scoring", scoring);
            Set(flow, "defaultDifficulty", defaultDifficulty);
            Set(flow, "assistedDifficulty", assistedDifficulty);

            CreateEventSystem(actions);
            CreateUi(flow, catchSystem, throwSystem, content);

            if (_errors > 0)
            {
                Debug.LogError($"[MainSceneBuilder] {_errors} error(s). Scene NOT saved.");
                return false;
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[MainSceneBuilder] Saved {ScenePath} and set it as the only build scene.");
            return true;
        }

        // ---------------------------------------------------------------- data assets

        private static void EnsureDataAssets()
        {
            CreateIfMissing<GameplayTuning>(TuningPath, null);
            CreateIfMissing<DifficultyProfile>(DefaultDifficultyPath, p => p.displayName = "DEFAULT");
            CreateIfMissing<DifficultyProfile>(AssistedDifficultyPath, p =>
            {
                p.displayName = "ASSISTED MODE";
                p.catchWindowMultiplier = 2f;
                p.landingZoneMultiplier = 1.25f;
                p.throwZoneMultiplier = 1.5f;
            });
            CreateIfMissing<ScoringConfig>(ScoringPath, null);
            CreateIfMissing<MissionContent>(ContentPath, null);
            AssetDatabase.SaveAssets();
        }

        private static void CreateIfMissing<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[MainSceneBuilder] Created {path}");
        }

        // ---------------------------------------------------------------- world helpers

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ReferenceHeight / (2f * PixelsPerUnit);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Midnight;
            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<AudioListener>();
            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PixelsPerUnit;
            ppc.refResolutionX = ReferenceWidth;
            ppc.refResolutionY = ReferenceHeight;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
        }

        private static SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 position, int order, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static void CreateEventSystem(InputActionAsset actions)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = go.GetComponent<InputSystemUIInputModule>();
            // Use the project's own UI map (Space/Enter submit, arrows/WASD navigate, multi-touch
            // pointers for the on-screen stick + button) instead of the package defaults.
            InputActionReference[] refs = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<InputActionReference>().ToArray();
            module.actionsAsset = actions;
            module.move = FindRef(refs, "UI", "Navigate");
            module.submit = FindRef(refs, "UI", "Submit");
            module.cancel = FindRef(refs, "UI", "Cancel");
            module.point = FindRef(refs, "UI", "Point");
            module.leftClick = FindRef(refs, "UI", "Click");
            module.scrollWheel = FindRef(refs, "UI", "ScrollWheel");
            module.rightClick = null;
            module.middleClick = null;
            module.trackedDevicePosition = null;
            module.trackedDeviceOrientation = null;
        }

        private static InputActionReference FindRef(InputActionReference[] refs, string map, string action)
        {
            InputActionReference r = refs.FirstOrDefault(x => x.action != null &&
                                                             x.action.actionMap.name == map && x.action.name == action);
            if (r == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Input action reference {map}/{action} not found in {ActionsPath}.");
            }
            return r;
        }

        // ---------------------------------------------------------------- UI

        private static void CreateUi(GameFlowController flow, CatchSystem catchSystem, ThrowSystem throwSystem,
            MissionContent content)
        {
            var canvasGo = new GameObject("PlaceholderCanvas (M2)", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;
            var view = canvasGo.AddComponent<PlaceholderFlowView>();
            Set(view, "flow", flow);
            Set(view, "catchSystem", catchSystem);
            Set(view, "content", content);

            // --- HUD (gameplay + catch sequence + throw) -----------------------------
            RectTransform hud = CreatePanel(root, "HudPanel", null);
            Set(view, "hudPanel", hud.gameObject);
            Set(view, "promptText", CreateText(hud, "Prompt", 36, Yellow, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1100f, 50f)));
            Set(view, "subText", CreateText(hud, "SubPrompt", 24, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1100f, 34f)));
            Set(view, "modeText", CreateText(hud, "ModeLabel", 20, Orange, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(400f, 30f)));
            Button pause = CreateButton(hud, "PauseButton", new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(72f, 60f));
            pause.navigation = new Navigation { mode = Navigation.Mode.None };
            Set(view, "pauseButton", pause);
            Wire(pause, view.OnPauseClicked);
            CreateThrowMeter(hud, throwSystem, view);
            CreateOnScreenControls(hud);

            // --- 01 Title --------------------------------------------------------------
            RectTransform title = CreatePanel(root, "TitlePanel", Dim);
            Set(view, "titlePanel", title.gameObject);
            Set(view, "titleText", CreateText(title, "TitleText", 30, OffWhite, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1100f, 380f)));
            Text pressStart = CreateText(title, "PressStart", 34, Yellow, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(800f, 50f));
            pressStart.gameObject.AddComponent<BlinkingText>();
            Set(view, "pressStartText", pressStart);
            Button start = CreateButton(title, "StartButton", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(360f, 56f));
            Button assisted = CreateButton(title, "AssistedButton", new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 56f));
            Set(view, "startButton", start);
            Set(view, "assistedButton", assisted);
            Wire(start, view.OnStartClicked);
            Wire(assisted, view.OnAssistedClicked);

            // --- 02 Briefing ----------------------------------------------------------
            RectTransform briefing = CreatePanel(root, "BriefingPanel", Dim);
            Set(view, "briefingPanel", briefing.gameObject);
            Set(view, "briefingText", CreateText(briefing, "BriefingText", 26, OffWhite, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1150f, 520f)));
            Button cont = CreateButton(briefing, "ContinueButton", new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 56f));
            Set(view, "continueButton", cont);
            Wire(cont, view.OnContinueClicked);

            // --- Pause overlay --------------------------------------------------------
            RectTransform pausePanel = CreatePanel(root, "PausePanel", Dim);
            Set(view, "pausePanel", pausePanel.gameObject);
            Set(view, "pauseTitleText", CreateText(pausePanel, "PauseTitle", 48, Yellow, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(800f, 70f)));
            Button resume = CreateButton(pausePanel, "ResumeButton", new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(360f, 56f));
            Button restart = CreateButton(pausePanel, "RestartButton", new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(360f, 56f));
            Set(view, "resumeButton", resume);
            Set(view, "restartButton", restart);
            Wire(resume, view.OnResumeClicked);
            Wire(restart, view.OnRestartClicked);

            // --- 05 Failed ------------------------------------------------------------
            RectTransform failed = CreatePanel(root, "FailedPanel", Dim);
            Set(view, "failedPanel", failed.gameObject);
            Set(view, "failedText", CreateText(failed, "FailedText", 32, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1150f, 380f)));
            Button retry = CreateButton(failed, "RetryButton", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(360f, 56f));
            Set(view, "retryButton", retry);
            Wire(retry, view.OnRetryClicked);

            // --- 06 Complete ----------------------------------------------------------
            RectTransform complete = CreatePanel(root, "CompletePanel", Dim);
            Set(view, "completePanel", complete.gameObject);
            Set(view, "completeText", CreateText(complete, "CompleteText", 30, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1150f, 420f)));
            Button replayMission = CreateButton(complete, "ReplayMissionButton", new Vector2(0.5f, 0f), new Vector2(-200f, 70f), new Vector2(360f, 56f));
            Button replayMode = CreateButton(complete, "ReplayModeButton", new Vector2(0.5f, 0f), new Vector2(200f, 70f), new Vector2(360f, 56f));
            Set(view, "replayMissionButton", replayMission);
            Set(view, "replayModeButton", replayMode);
            Wire(replayMission, view.OnReplayMissionClicked);
            Wire(replayMode, view.OnReplayModeClicked);

            // --- 07 Replay ------------------------------------------------------------
            RectTransform replay = CreatePanel(root, "ReplayPanel", Dim);
            Set(view, "replayPanel", replay.gameObject);
            Set(view, "replayText", CreateText(replay, "ReplayText", 28, OffWhite, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1150f, 420f)));
            Button museum = CreateButton(replay, "MuseumButton", new Vector2(0.5f, 0f), new Vector2(-200f, 70f), new Vector2(360f, 56f));
            Button back = CreateButton(replay, "BackToTitleButton", new Vector2(0.5f, 0f), new Vector2(200f, 70f), new Vector2(360f, 56f));
            Set(view, "museumButton", museum);
            Set(view, "backToTitleButton", back);
            Wire(museum, view.OnMuseumClicked);
            Wire(back, view.OnBackToTitleClicked);

            // Everything starts hidden; the view shows the right panel on the first state change.
            foreach (RectTransform p in new[] { hud, title, briefing, pausePanel, failed, complete, replay })
                p.gameObject.SetActive(false);
        }

        private static void CreateThrowMeter(Transform parent, ThrowSystem throwSystem, PlaceholderFlowView view)
        {
            // Outer image = off-white frame. "Track" = inner area (no graphic of its own); the
            // left track piece, the zone and the right track piece tile it without overlapping.
            // ThrowMeterView lays them out in pixels when the throw starts.
            Image meter = CreateImage(parent, "ThrowMeter", OffWhite, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(648f, 48f));
            var trackGo = new GameObject("Track", typeof(RectTransform));
            trackGo.transform.SetParent(meter.transform, false);
            var track = trackGo.GetComponent<RectTransform>();
            track.anchorMin = Vector2.zero;
            track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(4f, 4f);
            track.offsetMax = new Vector2(-4f, -4f);

            // Default layout (zone 0.42-0.58) so the scene looks right in the editor too.
            Image trackLeft = CreateImage(track, "TrackLeft", Midnight, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(268.8f, 40f));
            Image zone = CreateImage(track, "TargetZone", Green, new Vector2(0f, 0.5f), new Vector2(268.8f, 0f), new Vector2(102.4f, 40f));
            Image trackRight = CreateImage(track, "TrackRight", Midnight, new Vector2(0f, 0.5f), new Vector2(371.2f, 0f), new Vector2(268.8f, 40f));
            // Notches on the zone edges: a shape cue that works without colour.
            Image leftNotch = CreateImage(zone.transform, "NotchLeft", Yellow, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, 64f));
            Image rightNotch = CreateImage(zone.transform, "NotchRight", Yellow, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(6f, 64f));
            leftNotch.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rightNotch.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image needle = CreateImage(track, "Needle", Yellow, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(10f, 72f));
            needle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            CreateImage(needle.transform, "NeedleCore", Midnight, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 60f))
                .rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var meterView = meter.gameObject.AddComponent<ThrowMeterView>();
            Set(meterView, "throwSystem", throwSystem);
            Set(meterView, "track", track);
            Set(meterView, "trackLeft", trackLeft.rectTransform);
            Set(meterView, "zone", zone.rectTransform);
            Set(meterView, "trackRight", trackRight.rectTransform);
            Set(meterView, "needle", needle.rectTransform);
            Set(view, "throwMeter", meterView);
            foreach (Image i in meter.GetComponentsInChildren<Image>(true)) i.raycastTarget = false;
        }

        private static void CreateOnScreenControls(Transform parent)
        {
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var faded = new Color32(0xF5, 0xF1, 0xD9, 0x50);

            Image stickBase = CreateImage(parent, "OnScreenStickBase", faded, new Vector2(0f, 0f), new Vector2(150f, 150f), new Vector2(200f, 200f));
            stickBase.sprite = knob;
            stickBase.raycastTarget = false;
            stickBase.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Image stickHandle = CreateImage(stickBase.transform, "OnScreenStickHandle", OffWhite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            stickHandle.sprite = knob;
            stickHandle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var stick = stickHandle.gameObject.AddComponent<OnScreenStick>();
            stick.movementRange = 60f;
            SetString(stick, "m_ControlPath", "<Gamepad>/leftStick");

            Image actionButton = CreateImage(parent, "OnScreenActionButton", Orange, new Vector2(1f, 0f), new Vector2(-130f, 130f), new Vector2(140f, 140f));
            actionButton.sprite = knob;
            actionButton.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            CreateText(actionButton.transform, "Label", 48, Midnight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 140f)).text = "A";
            var onScreenButton = actionButton.gameObject.AddComponent<OnScreenButton>();
            SetString(onScreenButton, "m_ControlPath", "<Gamepad>/buttonSouth");
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color? background)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (background.HasValue) go.AddComponent<Image>().color = background.Value; // also blocks clicks to the field
            return rt;
        }

        private static Text CreateText(Transform parent, string name, int size, Color color, TextAnchor alignment,
            Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), anchor, position, dimensions);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Image CreateImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), anchor, position, dimensions);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image bg = CreateImage(parent, name, Color.white, anchor, position, size);
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Orange;
            colors.highlightedColor = Yellow;
            colors.selectedColor = Yellow;
            colors.pressedColor = OffWhite;
            colors.disabledColor = new Color32(0x80, 0x80, 0x80, 0xFF);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            CreateText(bg.transform, "Label", 24, Midnight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, size).text = name;
            return button;
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Wire(Button button, UnityAction action) =>
            UnityEventTools.AddPersistentListener(button.onClick, action);

        // ---------------------------------------------------------------- serialization helpers

        private static Sprite LoadSprite(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + fileName);
            if (sprite == null) Debug.LogError($"[MainSceneBuilder] Sprite not found: {SpriteFolder}{fileName}");
            return sprite;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Field '{field}' not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (value != null && prop.objectReferenceValue == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Reference '{field}' on {target.GetType().Name} did not stick.");
            }
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Field '{field}' not found on {target.GetType().Name}.");
                return;
            }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
