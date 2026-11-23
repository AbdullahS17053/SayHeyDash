using System.Collections.Generic;
using System.IO;
using System.Linq;
using SayHeyDash.Audio;
using SayHeyDash.Controls;
using SayHeyDash.Data;
using SayHeyDash.EditorTools.Fonts;
using SayHeyDash.EditorTools.Import;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using SayHeyDash.Platform;
using SayHeyDash.Replay;
using SayHeyDash.Settings;
using SayHeyDash.UI;
using SayHeyDash.Visuals;
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
using SettingsService = SayHeyDash.Settings.SettingsService; // not UnityEditor.SettingsService

namespace SayHeyDash.EditorTools.Scenes
{
    /// <summary>
    /// Generates the single gameplay scene (Main.unity) and any missing data assets from code,
    /// so the scene is reproducible and every reference is wired the same way each time.
    /// Existing data assets are NEVER overwritten - tuning/text/audio/animation edits survive
    /// a rebuild (only empty animation clips / audio entries are filled in).
    /// UI layout is authored on the 320x180 art grid: 1 art pixel = 4 units on the 1280x720 canvas.
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
        public const string AnimationsPath = "Assets/_Project/Data/Visuals/OutfielderAnimations.asset";
        public const string AudioLibraryPath = "Assets/_Project/Data/Audio/AudioCueLibrary.asset";

        private const string Sprites = "Assets/_Project/Art/Sprites/";
        private const string AudioFolder = "Assets/_Project/Audio/";
        private const string ActionsPath = "Assets/_Project/Input/SayHeyDashControls.inputactions";
        private const int ReferenceWidth = 320;
        private const int ReferenceHeight = 180;
        private const int PixelsPerUnit = 16;
        private const float U = 4f;            // canvas units per art pixel
        private const int Body = 40;           // Text.fontSize: 1 font px = 1 art px
        private const int Big = 80;            // 2 art px per font px

        // "Sunset arcade" palette (client feedback 2026-11-20) - must match Tools/ArtGen.
        private static readonly Color32 Midnight = new Color32(0x1B, 0x10, 0x30, 0xFF);
        private static readonly Color32 Yellow = new Color32(0xFF, 0xD8, 0x3D, 0xFF);
        private static readonly Color32 Orange = new Color32(0xF5, 0x7F, 0x32, 0xFF);
        private static readonly Color32 OffWhite = new Color32(0xFB, 0xF0, 0xD5, 0xFF);
        private static readonly Color32 Red = new Color32(0xE0, 0x48, 0x58, 0xFF);
        private static readonly Color32 Dim = new Color32(0x1B, 0x10, 0x30, 0xD8);
        private static readonly Color32 DimLight = new Color32(0x1B, 0x10, 0x30, 0xA0);

        private enum ButtonStyle { Primary, Secondary }

        private static Font _regular;
        private static Font _bold;
        private static AudioService _audio;
        private static int _errors;
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

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
            SpriteCache.Clear();
            EnsureSpriteImportSettings();
            if (!PixelFontBuilder.Build(out _, out _)) return false;

            // Data assets first (created only if missing), then the scene. NewScene(Single)
            // unloads unused assets, so everything is (re)loaded after it.
            EnsureDataAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SpriteCache.Clear();

            _regular = AssetDatabase.LoadAssetAtPath<Font>(PixelFontBuilder.RegularPath);
            _bold = AssetDatabase.LoadAssetAtPath<Font>(PixelFontBuilder.BoldPath);
            var tuning = AssetDatabase.LoadAssetAtPath<GameplayTuning>(TuningPath);
            var defaultDifficulty = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(DefaultDifficultyPath);
            var assistedDifficulty = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(AssistedDifficultyPath);
            var scoring = AssetDatabase.LoadAssetAtPath<ScoringConfig>(ScoringPath);
            var content = AssetDatabase.LoadAssetAtPath<MissionContent>(ContentPath);
            var animations = AssetDatabase.LoadAssetAtPath<OutfielderAnimationSet>(AnimationsPath);
            var audioLibrary = AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioLibraryPath);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (_regular == null || _bold == null || tuning == null || defaultDifficulty == null ||
                assistedDifficulty == null || scoring == null || content == null || animations == null ||
                audioLibrary == null || actions == null)
            {
                Debug.LogError("[MainSceneBuilder] Missing fonts, data assets or input actions. Scene NOT saved.");
                return false;
            }

            // ---------------------------------------------------------------- world
            CreateCamera();
            EnvironmentAnimator environment = CreateStadium();

            var indicatorGo = new GameObject("LandingIndicator");
            SpriteRenderer ring = CreateSprite("Ring", LoadSprite("Gameplay/landing_ring_dashed"), Vector3.zero, 4, indicatorGo.transform);
            ring.enabled = false;
            var indicator = indicatorGo.AddComponent<LandingIndicator>();
            Set(indicator, "ring", ring);
            Set(indicator, "dashedSprite", LoadSprite("Gameplay/landing_ring_dashed"));
            Set(indicator, "solidSprite", LoadSprite("Gameplay/landing_ring_solid"));

            var outfielderGo = new GameObject("Outfielder");
            outfielderGo.transform.position = tuning.playerStartPosition;
            SpriteRenderer body = CreateSprite("Body", LoadSprite("Character/outfielder_idle_00"), Vector3.zero, 10, outfielderGo.transform);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0f); // root = feet (row 26 of the 32 px cell)
            var bodyAnimator = body.gameObject.AddComponent<SpriteAnimator>();
            Set(bodyAnimator, "spriteRenderer", body);

            var ballGo = new GameObject("Ball");
            SpriteRenderer shadow = CreateSprite("Shadow", LoadSprite("Character/ball_shadow"), Vector3.zero, 5, ballGo.transform);
            SpriteRenderer ballRenderer = CreateSprite("Ball", LoadSprite("Character/ball_spin_00"), Vector3.zero, 20, ballGo.transform);
            shadow.enabled = false;
            ballRenderer.enabled = false;
            var ballSpin = ballRenderer.gameObject.AddComponent<SpriteAnimator>();
            Set(ballSpin, "spriteRenderer", ballRenderer);
            SetClip(ballSpin, "startClip", LoadFrames("Character/ball_spin_"), 12f, true);

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

            var outfielderAnimator = outfielderGo.AddComponent<OutfielderAnimator>();
            Set(outfielderAnimator, "flow", flow);
            Set(outfielderAnimator, "outfielder", outfielder);
            Set(outfielderAnimator, "catchSystem", catchSystem);
            Set(outfielderAnimator, "throwSystem", throwSystem);
            Set(outfielderAnimator, "tuning", tuning);
            Set(outfielderAnimator, "clips", animations);
            Set(outfielderAnimator, "body", bodyAnimator);
            Set(outfielderAnimator, "bodyRenderer", body);

            Set(environment, "flow", flow);
            _audio = CreateAudio(audioLibrary, flow, catchSystem, throwSystem);

            // M4: persisted options + best score, and the web page's pause requests.
            var settings = new GameObject("Settings").AddComponent<SettingsService>();
            Set(settings, "flow", flow);
            Set(settings, "audioService", _audio);
            var browserEvents = new GameObject(BrowserEvents.GameObjectName).AddComponent<BrowserEvents>();
            Set(browserEvents, "flow", flow);

            CreateEventSystem(actions);
            CreateUi(flow, catchSystem, throwSystem, content, animations, settings);
            // M5: client-logo splash shown after the web page's start click.
            Set(browserEvents, "splash", CreateBrandSplash(flow));

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
            CreateIfMissing<OutfielderAnimationSet>(AnimationsPath, null);
            CreateIfMissing<AudioCueLibrary>(AudioLibraryPath, null);
            FillAnimations(AssetDatabase.LoadAssetAtPath<OutfielderAnimationSet>(AnimationsPath));
            FillAudioLibrary(AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioLibraryPath));
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

        /// <summary>Fills only clips that have no frames, so designer edits are kept.</summary>
        private static void FillAnimations(OutfielderAnimationSet set)
        {
            Sprite[] throwFrames = LoadFrames("Character/outfielder_throw_");
            bool changed = false;
            changed |= Fill(set.idle, LoadFrames("Character/outfielder_idle_"));
            changed |= Fill(set.run, LoadFrames("Character/outfielder_run_"));
            changed |= Fill(set.runReach, LoadFrames("Character/outfielder_run_reach_"));
            changed |= Fill(set.reachIdle, new[] { LoadSprite("Character/outfielder_run_reach_02") });
            changed |= Fill(set.catchBall, LoadFrames("Character/outfielder_catch_"));
            changed |= Fill(set.whiff, LoadFrames("Character/outfielder_whiff_"));
            changed |= Fill(set.spin, LoadFrames("Character/outfielder_spin_"));
            changed |= Fill(set.throwReady, throwFrames.Take(1).ToArray());
            changed |= Fill(set.throwRelease, throwFrames.Skip(1).ToArray());
            if (changed) EditorUtility.SetDirty(set);
        }

        private static bool Fill(SpriteAnimationClip clip, Sprite[] frames)
        {
            if (clip.IsValid || frames.Length == 0 || frames.Any(f => f == null)) return false;
            clip.frames = frames;
            return true;
        }

        private static void FillAudioLibrary(AudioCueLibrary library)
        {
            if (library.entries != null && library.entries.Length > 0) return;
            (AudioCue cue, string file, float volume)[] map =
            {
                // M4 mix: levels matched by measured loudness (Docs/07 section 4). The bat crack and
                // the catch lead, cues and jingles sit just under them, menus and music below,
                // the crowd bed lowest.
                (AudioCue.UiMove, "SFX/ui_move", 0.4f),
                (AudioCue.UiConfirm, "SFX/ui_confirm", 0.5f),
                (AudioCue.UiBack, "SFX/ui_back", 0.5f),
                (AudioCue.BatCrack, "SFX/sfx_bat_crack", 1f),
                (AudioCue.CrowdLoop, "SFX/sfx_crowd_loop", 0.35f),
                (AudioCue.CrowdCheer, "SFX/sfx_crowd_cheer", 0.5f),
                (AudioCue.CatchReady, "SFX/sfx_ready", 0.4f),
                (AudioCue.CatchNow, "SFX/sfx_now", 0.5f),
                (AudioCue.Catch, "SFX/sfx_catch", 1f),
                (AudioCue.Whiff, "SFX/sfx_whiff", 0.45f),
                (AudioCue.Throw, "SFX/sfx_throw", 0.85f),
                (AudioCue.SuccessJingle, "Music/jingle_success", 0.6f),
                (AudioCue.FailJingle, "Music/jingle_fail", 0.65f),
                (AudioCue.TitleMusic, "Music/music_title_loop", 0.5f),
            };
            var entries = new List<AudioCueLibrary.Entry>();
            foreach (var (cue, file, volume) in map)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + file + ".wav");
                if (clip == null)
                {
                    _errors++;
                    Debug.LogError($"[MainSceneBuilder] Audio clip not found: {AudioFolder}{file}.wav (run Tools/AudioGen/generate_audio.py)");
                    continue;
                }
                entries.Add(new AudioCueLibrary.Entry { cue = cue, clip = clip, volume = volume });
            }
            library.entries = entries.ToArray();
            EditorUtility.SetDirty(library);
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

        /// <summary>Polo Grounds-style centre field: base image, animated crowd band and flags.</summary>
        private static EnvironmentAnimator CreateStadium()
        {
            var root = new GameObject("Stadium");
            CreateSprite("Field", LoadSprite("Environment/env_stadium"), Vector3.zero, -100, root.transform);
            // Bands are placed by their pixel rows in the 320x180 image (see generate_m3_art.py).
            SpriteRenderer crowd = CreateSprite("Crowd", LoadSprite("Environment/env_crowd_00"),
                new Vector3(0f, BandCenterY(26, 44), 0f), -99, root.transform);
            SpriteRenderer flags = CreateSprite("Flags", LoadSprite("Environment/env_flags_00"),
                new Vector3(0f, BandCenterY(0, 12), 0f), -98, root.transform);
            var crowdAnim = crowd.gameObject.AddComponent<SpriteAnimator>();
            Set(crowdAnim, "spriteRenderer", crowd);
            var flagsAnim = flags.gameObject.AddComponent<SpriteAnimator>();
            Set(flagsAnim, "spriteRenderer", flags);

            var env = root.AddComponent<EnvironmentAnimator>();
            Set(env, "crowd", crowdAnim);
            Set(env, "flags", flagsAnim);
            Sprite[] crowdFrames = LoadFrames("Environment/env_crowd_");
            SetClip(env, "crowdIdle", crowdFrames.Take(2).ToArray(), 2f, true);
            SetClip(env, "crowdCheer", crowdFrames.Skip(1).Take(2).ToArray(), 6f, true);
            SetClip(env, "flagsWave", LoadFrames("Environment/env_flags_"), 3f, true);
            return env;
        }

        private static float BandCenterY(int topRow, int height) =>
            (ReferenceHeight / 2f - (topRow + height / 2f)) / PixelsPerUnit;

        private static AudioService CreateAudio(AudioCueLibrary library, GameFlowController flow,
            CatchSystem catchSystem, ThrowSystem throwSystem)
        {
            var go = new GameObject("Audio");
            AudioSource Source(string name)
            {
                var child = new GameObject(name);
                child.transform.SetParent(go.transform, false);
                var s = child.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                return s;
            }
            var service = go.AddComponent<AudioService>();
            Set(service, "library", library);
            Set(service, "flow", flow);
            Set(service, "catchSystem", catchSystem);
            Set(service, "throwSystem", throwSystem);
            Set(service, "sfxSource", Source("Sfx"));
            Set(service, "musicSource", Source("Music"));
            Set(service, "ambienceSource", Source("Ambience"));
            return service;
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

        /// <summary>
        /// M5: the client-logo splash (logo on black) on its own canvas above all game UI.
        /// Uses the transparent logo made by Say Hey Dash &gt; Branding.
        /// </summary>
        private static BrandSplashView CreateBrandSplash(GameFlowController flow)
        {
            var go = new GameObject("BrandSplash", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            backdrop.transform.SetParent(go.transform, false);
            backdrop.color = Color.black;
            var rt = backdrop.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-400f, -400f); // overscan: covers any aspect ratio
            rt.offsetMax = new Vector2(400f, 400f);

            const string logoPath = Branding.BrandingBuilder.LogoAlphaPath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
            if (sprite == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Missing {logoPath}. Run Say Hey Dash > Branding > Rebuild Logo Files + Apply.");
            }
            var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            logo.transform.SetParent(go.transform, false);
            logo.sprite = sprite;
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            var lrt = logo.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(760f, 468f);
            lrt.anchoredPosition = Vector2.zero;

            var view = go.AddComponent<BrandSplashView>();
            Set(view, "flow", flow);
            Set(view, "canvas", canvas);
            Set(view, "group", group);
            Set(view, "logo", lrt);
            return view;
        }

        private static void CreateUi(GameFlowController flow, CatchSystem catchSystem, ThrowSystem throwSystem,
            MissionContent content, OutfielderAnimationSet animations, SettingsService settings)
        {
            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = PixelsPerUnit * U; // 16 PPU sprites draw 4 units per pixel
            Transform root = canvasGo.transform;

            TouchControls touch = BuildHud(root, flow, catchSystem, throwSystem, content);
            TitleScreenView title = BuildTitle(root, flow, content, animations);
            BuildBriefing(root, flow, content, touch);
            BuildFailed(root, flow, content);
            BuildComplete(root, flow, content, settings);
            BuildReplay(root, flow, content);
            PauseMenuView pause = BuildPause(root, flow, content);
            // Last = drawn on top of the title screen and the pause menu that open it.
            SettingsMenuView menu = BuildSettings(root, flow, content, settings);
            Set(title, "settingsMenu", menu);
            Set(pause, "settingsMenu", menu);
        }

        private static T CreateView<T>(Transform root, string name, GameFlowController flow, MissionContent content,
            out RectTransform panel) where T : ScreenView
        {
            RectTransform viewRt = Stretch(new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(), root);
            var view = viewRt.gameObject.AddComponent<T>();
            panel = Stretch(new GameObject(name + "Panel", typeof(RectTransform)).GetComponent<RectTransform>(), viewRt);
            Set(view, "flow", flow);
            Set(view, "content", content);
            Set(view, "panel", panel.gameObject);
            panel.gameObject.SetActive(false); // the view shows it on the first state change
            return view;
        }

        // --- 03/04 HUD ------------------------------------------------------------------
        private static TouchControls BuildHud(Transform root, GameFlowController flow, CatchSystem catchSystem,
            ThrowSystem throwSystem, MissionContent content)
        {
            var view = CreateView<HudView>(root, "Hud", flow, content, out RectTransform hud);
            Set(view, "catchSystem", catchSystem);
            Set(view, "throwSystem", throwSystem);
            Set(view, "audioService", _audio);

            Set(view, "promptText", Label(hud, "Prompt", _bold, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 3), Px(312, 10), true));
            // Bottom centre: the top band holds the prompt, and below it the scoreboard must stay readable.
            Set(view, "subText", Label(hud, "SubPrompt", _bold, Body, OffWhite, TextAnchor.LowerCenter, Bottom(0, 5), Px(140, 10), true));
            Image chip = Panel(hud, "SituationChip", "UI/ui_chip_slice3", TopLeft(4, 13), Px(96, 12));
            Set(view, "situationText", Label(chip.transform, "Situation", _regular, Body, Yellow, TextAnchor.MiddleCenter,
                Center(0, 0), Px(96, 10), false));
            Set(view, "modeText", Label(hud, "ModeLabel", _regular, Body, Orange, TextAnchor.UpperLeft, TopLeft(4, 27), Px(116, 10), true));

            // Icons sit on the second row so the prompt line can use the full width.
            Button pause = IconButton(hud, "PauseButton", "UI/icon_pause", TopRight(4, 13), 2, out _);
            Wire(pause, view.OnPauseClicked);
            Image soundIcon;
            Button sound = IconButton(hud, "SoundButton", "UI/icon_sound_on", TopRight(28, 13), 2, out soundIcon);
            Wire(sound, view.OnSoundClicked);
            Set(view, "soundIcon", soundIcon);
            Set(view, "soundOnSprite", LoadSprite("UI/icon_sound_on"));
            Set(view, "soundOffSprite", LoadSprite("UI/icon_sound_off"));

            // 04 catch-sequence banner.
            RectTransform banner = Stretch(new GameObject("CatchBanner", typeof(RectTransform)).GetComponent<RectTransform>(), hud);
            Set(view, "catchBanner", banner.gameObject);
            Set(view, "catchHeadlineText", Label(banner, "Headline", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 40), Px(300, 20), true));
            Set(view, "skipHintText", Label(banner, "SkipHint", _regular, Body, OffWhite, TextAnchor.LowerCenter, Bottom(0, 6), Px(200, 10), true));
            banner.gameObject.SetActive(false);

            // Stage C meter + labels.
            ThrowMeterView meter = CreateThrowMeter(hud, throwSystem);
            Set(view, "throwMeter", meter);
            Set(view, "throwLabelText", Label(hud, "ThrowLabel", _bold, Body, Yellow, TextAnchor.LowerCenter, Bottom(0, 70), Px(120, 10), true));
            // Below the meter: at throw time the outfielder stands on the warning track above it.
            Set(view, "throwFeedbackText", Label(hud, "ThrowFeedback", _bold, Big, Orange, TextAnchor.UpperCenter, Bottom(0, 30), Px(300, 20), true));

            // M4 mobile controls: shown on touch devices only (TouchControls lives on the always-active view).
            CreateOnScreenControls(hud, out GameObject stick, out GameObject actionButton, out Text actionLabel);
            var touch = view.gameObject.AddComponent<TouchControls>();
            Set(touch, "flow", flow);
            Set(touch, "content", content);
            Set(touch, "stick", stick);
            Set(touch, "actionButton", actionButton);
            Set(touch, "actionLabel", actionLabel);
            return touch;
        }

        private static ThrowMeterView CreateThrowMeter(Transform parent, ThrowSystem throwSystem)
        {
            // Outer image = off-white frame. "Track" = inner area (no graphic of its own); the
            // left track piece, the zone and the right track piece tile it without overlapping
            // (a fill drawn over a same-material background did not render in WebGL - M2 fix).
            Image meter = Img(parent, "ThrowMeter", null, OffWhite, Bottom(0, 56), Px(160, 12));
            var track = new GameObject("Track", typeof(RectTransform)).GetComponent<RectTransform>();
            track.SetParent(meter.transform, false);
            track.anchorMin = Vector2.zero;
            track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(U, U);
            track.offsetMax = new Vector2(-U, -U);

            // Default layout (zone 0.42-0.58) so the scene looks right in the editor too.
            const float w = 160 * U - 2 * U, h = 12 * U - 2 * U;
            Image trackLeft = Img(track, "TrackLeft", null, Midnight, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(w * 0.42f, h));
            Image zone = Img(track, "TargetZone", null, Yellow, new Vector2(0f, 0.5f), new Vector2(w * 0.42f, 0f), new Vector2(w * 0.16f, h));
            Image trackRight = Img(track, "TrackRight", null, Midnight, new Vector2(0f, 0.5f), new Vector2(w * 0.58f, 0f), new Vector2(w * 0.42f, h));
            // Notches on the zone edges: a shape cue that works without colour.
            Image leftNotch = Img(zone.transform, "NotchLeft", null, OffWhite, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(U * 1.5f, 16 * U));
            Image rightNotch = Img(zone.transform, "NotchRight", null, OffWhite, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(U * 1.5f, 16 * U));
            leftNotch.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rightNotch.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image needle = Img(track, "Needle", null, Red, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3 * U, 18 * U));
            needle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Img(needle.transform, "NeedleCore", null, OffWhite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(U, 14 * U))
                .rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var meterView = meter.gameObject.AddComponent<ThrowMeterView>();
            Set(meterView, "throwSystem", throwSystem);
            Set(meterView, "track", track);
            Set(meterView, "trackLeft", trackLeft.rectTransform);
            Set(meterView, "zone", zone.rectTransform);
            Set(meterView, "trackRight", trackRight.rectTransform);
            Set(meterView, "needle", needle.rectTransform);
            foreach (Image i in meter.GetComponentsInChildren<Image>(true)) i.raycastTarget = false;
            meter.gameObject.SetActive(false);
            return meterView;
        }

        private static void CreateOnScreenControls(Transform parent, out GameObject stickRoot, out GameObject actionRoot,
            out Text actionLabel)
        {
            const float k = 6f; // touch controls are drawn 6x for finger-sized targets
            Image stickBase = Img(parent, "OnScreenStickBase", "UI/ui_stick_base", Color.white, new Vector2(0f, 0f),
                new Vector2(110f, 110f), new Vector2(28 * k, 28 * k));
            stickBase.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            stickBase.raycastTarget = false;
            stickBase.color = new Color(1f, 1f, 1f, 0.55f);
            Image stickHandle = Img(stickBase.transform, "OnScreenStickHandle", "UI/ui_stick_knob", Color.white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14 * k, 14 * k));
            stickHandle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var stick = stickHandle.gameObject.AddComponent<OnScreenStick>();
            stick.movementRange = 60f;
            SetString(stick, "m_ControlPath", "<Gamepad>/leftStick");

            Image actionButton = Img(parent, "OnScreenActionButton", "UI/ui_action_button", Color.white, new Vector2(1f, 0f),
                new Vector2(-110f, 110f), new Vector2(24 * k, 24 * k));
            actionButton.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var onScreenButton = actionButton.gameObject.AddComponent<OnScreenButton>();
            SetString(onScreenButton, "m_ControlPath", "<Gamepad>/buttonSouth");

            // What the button does right now (CATCH / THROW), just above it.
            actionLabel = Label(parent, "OnScreenActionLabel", _bold, Body, OffWhite, TextAnchor.LowerCenter,
                new Spot { Anchor = new Vector2(1f, 0f), Position = Vector2.zero }, Px(48, 10), true);
            actionLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            actionLabel.rectTransform.anchoredPosition = new Vector2(-110f, 110f + 12 * k + 2 * U);
            stickRoot = stickBase.gameObject;
            actionRoot = actionButton.gameObject;
        }

        // --- 01 Title ------------------------------------------------------------------
        private static TitleScreenView BuildTitle(Transform root, GameFlowController flow, MissionContent content,
            OutfielderAnimationSet animations)
        {
            var view = CreateView<TitleScreenView>(root, "Title", flow, content, out RectTransform panel);
            Set(view, "audioService", _audio);
            Image backdrop = Img(panel, "Backdrop", "Title/title_backdrop", Color.white, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(backdrop.rectTransform, panel);
            backdrop.raycastTarget = true; // blocks clicks to the field

            Set(view, "presentsText", Label(panel, "Presents", _regular, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 6), Px(300, 10), true));
            Set(view, "arcadeModeText", Label(panel, "ArcadeMode", _bold, Body, Orange, TextAnchor.UpperCenter, Top(0, 16), Px(200, 10), true));
            Sprite logo = LoadSprite("Title/title_logo");
            Img(panel, "Logo", "Title/title_logo", Color.white, Top(0, 27), logo != null ? Px(logo.rect.width, logo.rect.height) : Vector2.zero)
                .raycastTarget = false;
            Set(view, "infoText", Label(panel, "Info", _regular, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 86), Px(200, 30), true));
            Text pressStart = Label(panel, "PressStart", _bold, Body, Yellow, TextAnchor.UpperCenter, Top(0, 118), Px(200, 10), true);
            pressStart.gameObject.AddComponent<BlinkingText>();
            Set(view, "pressStartText", pressStart);

            // The outfielder running under the ball, glove up (title-screen artwork).
            Image hero = Img(panel, "TitleOutfielder", "Character/outfielder_run_reach_00", Color.white, TopLeft(244, 80), Px(64, 64));
            hero.raycastTarget = false;
            var heroAnim = hero.gameObject.AddComponent<SpriteAnimator>();
            Set(heroAnim, "image", hero);
            SetClip(heroAnim, "startClip", animations.runReach.frames, 10f, true);

            PixelButton start = PixelBtn(panel, "StartButton", ButtonStyle.Primary, Top(-40, 130), Px(72, 16), content.startButton);
            PixelButton settingsBtn = PixelBtn(panel, "SettingsButton", ButtonStyle.Secondary, Top(40, 130), Px(72, 16), content.settingsButton);
            Set(view, "settingsButton", settingsBtn);
            Wire(settingsBtn.Button, view.OnSettingsClicked);
            PixelButton sound = PixelBtn(panel, "SoundButton", ButtonStyle.Secondary, TopLeft(26, 152), Px(68, 15), content.soundOnLabel);
            PixelButton assisted = PixelBtn(panel, "AssistedButton", ButtonStyle.Secondary, TopLeft(102, 152), Px(120, 15), content.assistedOffLabel);
            PixelButton credits = PixelBtn(panel, "CreditsButton", ButtonStyle.Secondary, TopLeft(230, 152), Px(64, 15), content.creditsButton);
            Set(view, "startButton", start);
            Set(view, "soundButton", sound);
            Set(view, "assistedButton", assisted);
            Set(view, "creditsButton", credits);
            Set(view, "firstSelected", start.Button);
            Wire(start.Button, view.OnStartClicked);
            Wire(sound.Button, view.OnSoundClicked);
            Wire(assisted.Button, view.OnAssistedClicked);
            Wire(credits.Button, view.OnCreditsClicked);

            // Credits overlay.
            RectTransform creditsPanel = Stretch(new GameObject("CreditsPanel", typeof(RectTransform)).GetComponent<RectTransform>(), panel);
            Stretch(Img(creditsPanel, "Dim", null, Dim, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, creditsPanel);
            Image box = Panel(creditsPanel, "Box", "UI/ui_panel_slice3", Center(0, 0), Px(272, 166));
            Set(view, "creditsPanel", creditsPanel.gameObject);
            Set(view, "creditsTitleText", Label(box.transform, "Title", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 6), Px(240, 20), false));
            Set(view, "creditsBodyText", Label(box.transform, "Body", _regular, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 28), Px(256, 110), false));
            PixelButton back = PixelBtn(box.transform, "BackButton", ButtonStyle.Primary, Bottom(0, 5), Px(64, 16), content.closeButton);
            SetAudioCue(back, AudioCue.UiBack);
            // The only control on the overlay: arrow keys must not wander to the title buttons beneath it.
            back.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            Set(view, "creditsBackButton", back);
            Wire(back.Button, view.OnCreditsBackClicked);
            creditsPanel.gameObject.SetActive(false);
            return view;
        }

        // --- 02 Mission objective -----------------------------------------------------
        private static void BuildBriefing(Transform root, GameFlowController flow, MissionContent content, TouchControls touch)
        {
            var view = CreateView<BriefingScreenView>(root, "Briefing", flow, content, out RectTransform panel);
            Set(view, "touchControls", touch);
            Stretch(Img(panel, "Dim", null, Dim, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, panel).GetComponent<Image>().raycastTarget = true;
            Set(view, "headerText", Label(panel, "Header", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 6), Px(300, 20), true));
            Image board = Panel(panel, "Scoreboard", "UI/ui_board_slice3", Top(0, 28), Px(180, 60));
            Set(view, "factsText", Label(board.transform, "Facts", _bold, Body, OffWhite, TextAnchor.UpperLeft, TopLeft(10, 6), Px(160, 50), false));
            Set(view, "objectiveText", Label(panel, "Objective", _bold, Body, Yellow, TextAnchor.UpperCenter, Top(0, 93), Px(310, 10), true));
            Set(view, "setupText", Label(panel, "Setup", _regular, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 106), Px(290, 20), true));
            Set(view, "controlsText", Label(panel, "Controls", _regular, Body, Orange, TextAnchor.UpperCenter, Top(0, 128), Px(290, 30), true));
            PixelButton cont = PixelBtn(panel, "ContinueButton", ButtonStyle.Primary, Bottom(0, 4), Px(96, 16), content.continueButton);
            Set(view, "continueButton", cont);
            Set(view, "firstSelected", cont.Button);
            Wire(cont.Button, view.OnContinueClicked);
        }

        // --- 05 Mission failed ---------------------------------------------------------
        private static void BuildFailed(Transform root, GameFlowController flow, MissionContent content)
        {
            var view = CreateView<MissionFailedView>(root, "Failed", flow, content, out RectTransform panel);
            Stretch(Img(panel, "Dim", null, DimLight, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, panel);
            Image box = Panel(panel, "Box", "UI/ui_panel_slice3", Center(0, 0), Px(216, 112));
            Set(view, "titleText", Label(box.transform, "Title", _bold, Big, Red, TextAnchor.UpperCenter, Top(0, 8), Px(200, 20), false));
            Set(view, "reasonText", Label(box.transform, "Reason", _bold, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 32), Px(200, 10), false));
            Set(view, "hintText", Label(box.transform, "Hint", _regular, Body, Orange, TextAnchor.UpperCenter, Top(0, 46), Px(200, 30), false));
            Set(view, "promptText", Label(box.transform, "Prompt", _bold, Body, Yellow, TextAnchor.UpperCenter, Top(0, 76), Px(200, 10), false));
            PixelButton retry = PixelBtn(box.transform, "RetryButton", ButtonStyle.Primary, Bottom(0, 8), Px(80, 16), content.retryButton);
            Set(view, "retryButton", retry);
            Set(view, "firstSelected", retry.Button);
            Wire(retry.Button, view.OnRetryClicked);
        }

        // --- 06 Mission complete -------------------------------------------------------
        private static void BuildComplete(Transform root, GameFlowController flow, MissionContent content, SettingsService settings)
        {
            var view = CreateView<MissionCompleteView>(root, "Complete", flow, content, out RectTransform panel);
            Set(view, "settings", settings);
            Stretch(Img(panel, "Dim", null, DimLight, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, panel);
            Image box = Panel(panel, "Box", "UI/ui_panel_slice3", Center(0, 0), Px(280, 164));
            Transform b = box.transform;
            Set(view, "titleText", Label(b, "Title", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 6), Px(270, 20), false));
            Set(view, "subtitleText", Label(b, "Subtitle", _regular, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 28), Px(260, 20), false));
            Set(view, "labelsText", Label(b, "Labels", _regular, Body, OffWhite, TextAnchor.UpperLeft, TopLeft(40, 54), Px(80, 30), false));
            Set(view, "valuesText", Label(b, "Values", _bold, Body, OffWhite, TextAnchor.UpperRight, TopLeft(120, 54), Px(40, 30), false));
            Set(view, "totalLabelText", Label(b, "TotalLabel", _bold, Body, Yellow, TextAnchor.UpperLeft, TopLeft(40, 92), Px(60, 10), false));
            Set(view, "totalValueText", Label(b, "TotalValue", _bold, Big, Yellow, TextAnchor.UpperRight, TopLeft(80, 86), Px(80, 20), false));
            Image badge = Img(b, "RankBadge", "UI/badge_legendary", Color.white, TopLeft(190, 52), Px(52, 44));
            badge.raycastTarget = false;
            Set(view, "rankBadge", badge);
            Set(view, "rookieBadge", LoadSprite("UI/badge_rookie"));
            Set(view, "allStarBadge", LoadSprite("UI/badge_allstar"));
            Set(view, "legendaryBadge", LoadSprite("UI/badge_legendary"));
            Set(view, "rankText", Label(b, "Rank", _bold, Body, Orange, TextAnchor.UpperCenter, TopLeft(160, 100), Px(112, 10), false));
            Set(view, "nextRankText", Label(b, "NextRank", _regular, Body, OffWhite, TextAnchor.UpperCenter, TopLeft(160, 111), Px(112, 10), false));
            Set(view, "bestText", Label(b, "Best", _regular, Body, OffWhite, TextAnchor.UpperLeft, TopLeft(40, 106), Px(120, 10), false));
            Set(view, "outcomeText", Label(b, "Outcome", _regular, Body, OffWhite, TextAnchor.UpperCenter, Top(0, 124), Px(264, 10), false));
            PixelButton replay = PixelBtn(b, "ReplayMissionButton", ButtonStyle.Primary, new Vector2(0.5f, 0f), new Vector2(-52 * U, 8 * U), Px(80, 16), content.replayMissionButton);
            PixelButton replayMode = PixelBtn(b, "ReplayModeButton", ButtonStyle.Secondary, new Vector2(0.5f, 0f), new Vector2(48 * U, 8 * U), Px(104, 16), content.replayModeButton);
            Set(view, "replayButton", replay);
            Set(view, "replayModeButton", replayMode);
            Set(view, "firstSelected", replay.Button);
            Wire(replay.Button, view.OnReplayClicked);
            Wire(replayMode.Button, view.OnReplayModeClicked);
        }

        // --- 07 Replay ------------------------------------------------------------------
        private static void BuildReplay(Transform root, GameFlowController flow, MissionContent content)
        {
            var view = CreateView<ReplayScreenView>(root, "Replay", flow, content, out RectTransform panel);
            Stretch(Img(panel, "Dim", null, Dim, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, panel)
                .GetComponent<Image>().raycastTarget = true;
            Image box = Panel(panel, "Box", "UI/ui_panel_slice3", Center(0, 0), Px(312, 172));
            Transform b = box.transform;
            Set(view, "headerText", Label(b, "Header", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 5), Px(300, 20), false));

            // Left: the field diagram with the moving markers, the step caption and a shape legend.
            Image frame = Panel(b, "DiagramFrame", "UI/ui_chip_slice3", TopLeft(8, 28), Px(132, 84));
            Image diagram = Img(frame.transform, "Diagram", "Replay/replay_field", Color.white, Center(0, 0),
                Px(ReplayTimeline.DiagramWidth, ReplayTimeline.DiagramHeight));
            RectTransform d = diagram.rectTransform;
            var trail = new RectTransform[10];
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i] = Marker(d, "TrailDot" + i, null, Yellow, 1);
                trail[i].gameObject.SetActive(false);
            }
            RectTransform leadRunner = Marker(d, "LeadRunner", "Replay/replay_runner", Color.white, 7);
            RectTransform trailRunner = Marker(d, "TrailRunner", "Replay/replay_runner", Color.white, 7);
            RectTransform batter = Marker(d, "Batter", "Replay/replay_runner", Color.white, 7);
            RectTransform fielder = Marker(d, "Fielder", "Replay/replay_fielder", Color.white, 7);
            RectTransform ball = Marker(d, "Ball", null, OffWhite, 2);

            var director = view.gameObject.AddComponent<ReplayDirector>();
            Set(director, "flow", flow);
            Set(director, "content", content);
            Set(director, "audioService", _audio);
            Set(director, "diagram", d);
            Set(director, "ball", ball);
            Set(director, "fielder", fielder);
            Set(director, "leadRunner", leadRunner);
            Set(director, "trailRunner", trailRunner);
            Set(director, "batter", batter);
            SetObjectArray(director, "trail", trail);
            Set(view, "director", director);

            Set(view, "stepText", Label(b, "Step", _bold, Body, Yellow, TextAnchor.UpperCenter, TopLeft(8, 114), Px(132, 10), false));
            Img(b, "LegendFielder", "Replay/replay_fielder", Color.white, TopLeft(14, 127), Px(7, 7));
            Set(view, "legendFielderText", Label(b, "LegendFielderLabel", _regular, Body, OffWhite, TextAnchor.UpperLeft, TopLeft(24, 127), Px(50, 10), false));
            Img(b, "LegendRunner", "Replay/replay_runner", Color.white, TopLeft(76, 127), Px(7, 7));
            Set(view, "legendRunnerText", Label(b, "LegendRunnerLabel", _regular, Body, OffWhite, TextAnchor.UpperLeft, TopLeft(86, 127), Px(56, 10), false));

            // Right: the narration for the current step, then the museum / STORY MODE link.
            Set(view, "narrationText", Label(b, "Narration", _regular, Body, OffWhite, TextAnchor.UpperLeft, TopLeft(148, 30), Px(156, 96), false));
            PixelButton museum = PixelBtn(b, "MuseumButton", ButtonStyle.Secondary, TopLeft(148, 130), Px(156, 15), content.museumButton);

            PixelButton again = PixelBtn(b, "WatchAgainButton", ButtonStyle.Secondary, BottomLeft(8, 6), Px(88, 16), content.watchAgainButton);
            PixelButton next = PixelBtn(b, "NextButton", ButtonStyle.Secondary, BottomLeft(100, 6), Px(64, 16), content.nextStepButton);
            PixelButton back = PixelBtn(b, "BackToTitleButton", ButtonStyle.Primary, BottomRight(8, 6), Px(104, 16), content.backToTitleButton);
            Set(view, "watchAgainButton", again);
            Set(view, "nextButton", next);
            Set(view, "museumButton", museum);
            Set(view, "backButton", back);
            Set(view, "firstSelected", next.Button);
            Wire(again.Button, view.OnWatchAgainClicked);
            Wire(next.Button, view.OnNextClicked);
            Wire(museum.Button, view.OnMuseumClicked);
            Wire(back.Button, view.OnBackClicked);
        }

        /// <summary>A replay marker: child of the diagram, anchored top-left, centred on its position.</summary>
        private static RectTransform Marker(RectTransform diagram, string name, string sprite, Color color, int sizePx)
        {
            Image img = Img(diagram, name, sprite, color, new Vector2(0f, 1f), Vector2.zero, Px(sizePx, sizePx));
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return img.rectTransform;
        }

        // --- Pause overlay --------------------------------------------------------------
        private static PauseMenuView BuildPause(Transform root, GameFlowController flow, MissionContent content)
        {
            var view = CreateView<PauseMenuView>(root, "Pause", flow, content, out RectTransform panel);
            Set(view, "audioService", _audio);
            Stretch(Img(panel, "Dim", null, DimLight, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, panel);
            Image box = Panel(panel, "Box", "UI/ui_panel_slice3", Center(0, 0), Px(150, 118));
            Set(view, "titleText", Label(box.transform, "Title", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 8), Px(140, 20), false));
            PixelButton resume = PixelBtn(box.transform, "ResumeButton", ButtonStyle.Primary, Top(0, 34), Px(100, 16), content.resumeButton);
            PixelButton restart = PixelBtn(box.transform, "RestartButton", ButtonStyle.Secondary, Top(0, 55), Px(100, 15), content.restartButton);
            PixelButton sound = PixelBtn(box.transform, "SoundButton", ButtonStyle.Secondary, Top(0, 75), Px(100, 15), content.soundOnLabel);
            Set(view, "resumeButton", resume);
            Set(view, "restartButton", restart);
            Set(view, "soundButton", sound);
            Set(view, "firstSelected", resume.Button);
            Wire(resume.Button, view.OnResumeClicked);
            Wire(restart.Button, view.OnRestartClicked);
            Wire(sound.Button, view.OnSoundClicked);
            PixelButton settingsBtn = PixelBtn(box.transform, "SettingsButton", ButtonStyle.Secondary, Top(0, 95), Px(100, 15), content.settingsButton);
            Set(view, "settingsButton", settingsBtn);
            Wire(settingsBtn.Button, view.OnSettingsClicked);
            return view;
        }

        // --- Settings overlay (M4) -------------------------------------------------------
        private static SettingsMenuView BuildSettings(Transform root, GameFlowController flow, MissionContent content,
            SettingsService settings)
        {
            var view = CreateView<SettingsMenuView>(root, "SettingsMenu", flow, content, out RectTransform panel);
            Set(view, "settings", settings);
            Set(view, "audioService", _audio);
            Stretch(Img(panel, "Dim", null, Dim, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, panel)
                .GetComponent<Image>().raycastTarget = true; // blocks clicks to the menu underneath
            Image box = Panel(panel, "Box", "UI/ui_panel_slice3", Center(0, 0), Px(200, 162));
            Transform b = box.transform;
            Set(view, "titleText", Label(b, "Title", _bold, Big, Yellow, TextAnchor.UpperCenter, Top(0, 6), Px(180, 20), false));
            string[] names = { "SoundButton", "MusicButton", "EffectsButton", "ReducedMotionButton", "AssistedButton" };
            string[] fields = { "soundButton", "musicButton", "effectsButton", "reducedMotionButton", "assistedButton" };
            UnityAction[] actions = { view.OnSoundClicked, view.OnMusicClicked, view.OnEffectsClicked, view.OnReducedMotionClicked, view.OnAssistedClicked };
            string[] labels =
            {
                content.soundOnLabel, string.Format(content.musicVolumeFormat, 100), string.Format(content.effectsVolumeFormat, 100),
                content.reducedMotionOffLabel, content.assistedOffLabel,
            };
            var rows = new PixelButton[names.Length + 1];
            for (int i = 0; i < names.Length; i++)
            {
                rows[i] = PixelBtn(b, names[i], ButtonStyle.Secondary, Top(0, 30 + i * 17), Px(170, 15), labels[i]);
                Set(view, fields[i], rows[i]);
                Wire(rows[i].Button, actions[i]);
            }
            Set(view, "noteText", Label(b, "Note", _regular, Body, Orange, TextAnchor.UpperCenter, Top(0, 117), Px(190, 20), false));
            PixelButton back = PixelBtn(b, "BackButton", ButtonStyle.Primary, Bottom(0, 5), Px(64, 16), content.closeButton);
            SetAudioCue(back, AudioCue.UiBack);
            rows[names.Length] = back;
            Set(view, "backButton", back);
            Wire(back.Button, view.OnBackClicked);
            Set(view, "firstSelected", rows[0].Button);
            // Explicit up/down loop: focus must never wander to the screen underneath the overlay.
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = rows[(i - 1 + rows.Length) % rows.Length].Button,
                    selectOnDown = rows[(i + 1) % rows.Length].Button,
                };
            }
            return view;
        }

        // ---------------------------------------------------------------- UI helpers
        // Positions/sizes are given in art pixels and converted to canvas units (x4).

        private static Vector2 Px(float w, float h) => new Vector2(w * U, h * U);

        private struct Spot { public Vector2 Anchor; public Vector2 Position; }

        private static Spot Top(float x, float y) => new Spot { Anchor = new Vector2(0.5f, 1f), Position = new Vector2(x * U, -y * U) };
        private static Spot TopLeft(float x, float y) => new Spot { Anchor = new Vector2(0f, 1f), Position = new Vector2(x * U, -y * U) };
        private static Spot TopRight(float x, float y) => new Spot { Anchor = new Vector2(1f, 1f), Position = new Vector2(-x * U, -y * U) };
        private static Spot Bottom(float x, float y) => new Spot { Anchor = new Vector2(0.5f, 0f), Position = new Vector2(x * U, y * U) };
        private static Spot BottomLeft(float x, float y) => new Spot { Anchor = new Vector2(0f, 0f), Position = new Vector2(x * U, y * U) };
        private static Spot BottomRight(float x, float y) => new Spot { Anchor = new Vector2(1f, 0f), Position = new Vector2(-x * U, y * U) };
        private static Spot Center(float x, float y) => new Spot { Anchor = new Vector2(0.5f, 0.5f), Position = new Vector2(x * U, -y * U) };

        private static RectTransform Stretch(RectTransform rt, Transform parent)
        {
            if (rt.parent != parent) rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Text Label(Transform parent, string name, Font font, int size, Color color, TextAnchor alignment,
            Spot spot, Vector2 dimensions, bool shadow)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), spot.Anchor, spot.Position, dimensions);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            t.raycastTarget = false;
            t.text = name;
            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = Midnight;
                s.effectDistance = new Vector2(U, -U);
            }
            return t;
        }

        private static Image Img(Transform parent, string name, string sprite, Color color, Spot spot, Vector2 size) =>
            Img(parent, name, sprite, color, spot.Anchor, spot.Position, size);

        private static Image Img(Transform parent, string name, string sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), anchor, position, size);
            var img = go.GetComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = LoadSprite(sprite);
            img.raycastTarget = false;
            return img;
        }

        private static Image Panel(Transform parent, string name, string sprite, Spot spot, Vector2 size)
        {
            Image img = Img(parent, name, sprite, Color.white, spot, size);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true; // panels block clicks to whatever is behind them
            return img;
        }

        private static PixelButton PixelBtn(Transform parent, string name, ButtonStyle style, Spot spot, Vector2 size, string label) =>
            PixelBtn(parent, name, style, spot.Anchor, spot.Position, size, label);

        private static PixelButton PixelBtn(Transform parent, string name, ButtonStyle style, Vector2 anchor, Vector2 position,
            Vector2 size, string label)
        {
            bool primary = style == ButtonStyle.Primary;
            Image bg = Img(parent, name, primary ? "UI/ui_button_primary_slice3" : "UI/ui_button_secondary_slice3",
                Color.white, anchor, position, size);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LoadSprite("UI/ui_button_selected_slice3"),
                selectedSprite = LoadSprite("UI/ui_button_selected_slice3"),
                pressedSprite = LoadSprite("UI/ui_button_pressed_slice3"),
                disabledSprite = LoadSprite("UI/ui_button_disabled_slice3"),
            };
            Text text = Label(bg.transform, "Label", _bold, Body, primary ? Midnight : OffWhite, TextAnchor.MiddleCenter,
                Center(0, -0.5f), size, false);
            text.text = label;
            Image cursor = Img(bg.transform, "Cursor", "UI/ui_cursor", Color.white, new Vector2(0f, 0.5f), new Vector2(3 * U, 0f), Px(6, 7));
            cursor.rectTransform.pivot = new Vector2(0f, 0.5f);
            cursor.enabled = false;

            var pixel = bg.gameObject.AddComponent<PixelButton>();
            Set(pixel, "label", text);
            Set(pixel, "cursor", cursor);
            Set(pixel, "audioService", _audio);
            SetColor(pixel, "labelColor", primary ? Midnight : OffWhite);
            SetColor(pixel, "selectedLabelColor", Midnight);
            return pixel;
        }

        private static Button IconButton(Transform parent, string name, string sprite, Spot spot, int scale, out Image icon)
        {
            Sprite s = LoadSprite(sprite);
            Vector2 size = s != null ? Px(s.rect.width * scale, s.rect.height * scale) : Px(16, 16);
            icon = Img(parent, name, sprite, Color.white, spot, size);
            icon.raycastTarget = true;
            var button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Yellow;
            colors.selectedColor = Color.white;
            colors.pressedColor = Orange;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            // No navigation: a HUD button must never hold focus, or Space would press it mid-play.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var pixel = icon.gameObject.AddComponent<PixelButton>();
            Set(pixel, "audioService", _audio);
            SetBool(pixel, "selectOnHover", false);
            return button;
        }

        private static void SetAudioCue(PixelButton button, AudioCue cue)
        {
            var so = new SerializedObject(button);
            so.FindProperty("clickCue").intValue = (int)cue;
            so.ApplyModifiedPropertiesWithoutUndo();
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

        // ---------------------------------------------------------------- assets

        /// <summary>Makes sure every M3 sprite uses the pixel-art import rules (and 9-slice borders).</summary>
        private static void EnsureSpriteImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art/Sprites" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Prototype/")) continue;
                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    PixelArtTexturePostprocessor.Apply(importer, path))
                    importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string relativePathNoExtension)
        {
            if (SpriteCache.TryGetValue(relativePathNoExtension, out Sprite cached) && cached != null) return cached;
            string path = Sprites + relativePathNoExtension + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Sprite not found: {path} (run Tools/ArtGen/generate_m3_art.py)");
            }
            SpriteCache[relativePathNoExtension] = sprite;
            return sprite;
        }

        /// <summary>Loads prefix00, prefix01 ... until a frame is missing.</summary>
        private static Sprite[] LoadFrames(string relativePrefix)
        {
            var frames = new List<Sprite>();
            for (int i = 0; i < 64; i++)
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}{relativePrefix}{i:00}.png");
                if (s == null) break;
                frames.Add(s);
            }
            if (frames.Count == 0)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] No frames found for {Sprites}{relativePrefix}NN.png");
            }
            return frames.ToArray();
        }

        // ---------------------------------------------------------------- serialization helpers

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

        private static SerializedProperty Find(SerializedObject so, Object target, string field)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                _errors++;
                Debug.LogError($"[MainSceneBuilder] Field '{field}' not found on {target.GetType().Name}.");
            }
            return prop;
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = Find(so, target, field);
            if (prop == null) return;
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = Find(so, target, field);
            if (prop == null) return;
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = Find(so, target, field);
            if (prop == null) return;
            prop.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = Find(so, target, field);
            if (prop == null) return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetClip(Object target, string field, Sprite[] frames, float fps, bool loop)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = Find(so, target, field);
            if (prop == null) return;
            prop.FindPropertyRelative("name").stringValue = field;
            SerializedProperty array = prop.FindPropertyRelative("frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            prop.FindPropertyRelative("framesPerSecond").floatValue = fps;
            prop.FindPropertyRelative("loop").boolValue = loop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
