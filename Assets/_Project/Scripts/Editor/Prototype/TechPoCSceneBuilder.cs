using System.IO;
using SayHeyDash.Controls;
using SayHeyDash.Prototype;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayHeyDash.EditorTools.Prototype
{
    /// <summary>
    /// PROTOTYPE TOOLING (Milestone 1).
    /// Generates the technical proof-of-concept scene from code so it is reproducible and
    /// reviewable. Re-run any time via: Say Hey Dash > Prototype > Rebuild M1 Tech PoC Scene.
    /// The scene and everything in Scripts/Prototype can be deleted once Milestone 2 ships.
    /// </summary>
    public static class TechPoCSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Prototype/M1_TechPoC.unity";
        private const string SpriteFolder = "Assets/_Project/Art/Sprites/Prototype/";
        private const string ActionsPath = "Assets/_Project/Input/SayHeyDashControls.inputactions";

        private const int ReferenceWidth = 320;
        private const int ReferenceHeight = 180;
        private const int PixelsPerUnit = 16;

        private static readonly Color32 Midnight = new Color32(0x17, 0x15, 0x2F, 0xFF);
        private static readonly Color32 Yellow = new Color32(0xF6, 0xCE, 0x4B, 0xFF);
        private static readonly Color32 Orange = new Color32(0xF3, 0x8B, 0x37, 0xFF);
        private static readonly Color32 OffWhite = new Color32(0xF5, 0xF1, 0xD9, 0xFF);
        private static readonly Color32 PanelBg = new Color32(0x17, 0x15, 0x2F, 0xD0);

        private static Font _font;
        private static int _referenceErrors;

        [MenuItem("Say Hey Dash/Prototype/Rebuild M1 Tech PoC Scene")]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (Build()) EditorSceneManager.OpenScene(ScenePath);
        }

        public static void BuildFromCommandLine()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            // Create the scene BEFORE loading assets: NewScene(Single) unloads unused assets,
            // which would silently turn previously loaded references into null.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _referenceErrors = 0;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            Sprite field = LoadSprite("proto_field_background.png");
            Sprite outfielder = LoadSprite("proto_outfielder_idle.png");
            Sprite ball = LoadSprite("proto_ball.png");
            Sprite shadow = LoadSprite("proto_ball_shadow.png");
            if (actions == null || field == null || outfielder == null || ball == null || shadow == null)
            {
                Debug.LogError("[TechPoCSceneBuilder] Missing input actions or prototype sprites. " +
                               "Run Tools/ArtGen/generate_placeholder_art.py and reimport.");
                return false;
            }

            CreateCamera();
            CreateSprite("FieldBackground", field, Vector3.zero, -100);
            CreateSprite("Ball (static art preview)", ball, new Vector3(4f, 2.5f, 0f), 20);
            CreateSprite("BallShadow (static art preview)", shadow, new Vector3(4f, -3.2f, 0f), 5);
            SpriteRenderer player = CreateSprite("Outfielder (Prototype)", outfielder, new Vector3(0f, -3f, 0f), 10);

            var systems = new GameObject("Systems");
            var gameInput = systems.AddComponent<GameInput>();
            SetReference(gameInput, "actions", actions);

            var feedback = systems.AddComponent<PrototypeActionFeedback>();
            SetReference(feedback, "input", gameInput);
            SetReference(feedback, "target", player);

            var mover = player.gameObject.AddComponent<PrototypeMover>();
            SetReference(mover, "input", gameInput);

            CreateEventSystem();
            CreateUi(gameInput, feedback);

            if (_referenceErrors > 0)
            {
                Debug.LogError($"[TechPoCSceneBuilder] {_referenceErrors} reference(s) failed to serialize. Scene NOT saved.");
                return false;
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[TechPoCSceneBuilder] Saved {ScenePath} and set it as the only build scene.");
            return true;
        }

        // ---------------------------------------------------------------- world

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

        private static SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 position, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        // ---------------------------------------------------------------- UI

        private static void CreateUi(GameInput gameInput, PrototypeActionFeedback feedback)
        {
            var canvasGo = new GameObject("PrototypeCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            // Header + blinking prompt.
            CreateText(root, "Header", "THE SAY HEY DASH  -  M1 TECHNICAL PROOF (PROTOTYPE)", 26, Yellow,
                TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1100f, 40f));
            Text prompt = CreateText(root, "Prompt", "MOVE: ARROWS / WASD / STICK      ACTION: SPACE / (A)", 22, Orange,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(1100f, 32f));
            var blink = prompt.gameObject.AddComponent<PrototypeBlink>();
            SetReference(blink, "target", prompt);

            // Diagnostics panel (top-left).
            Image panel = CreateImage(root, "DiagnosticsPanel", PanelBg, null,
                new Vector2(0f, 1f), new Vector2(16f, -90f), new Vector2(430f, 196f));
            Text diagText = CreateText(panel.transform, "DiagnosticsText", "...", 18, OffWhite,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(12f, -10f), new Vector2(410f, 180f));
            var diagnostics = panel.gameObject.AddComponent<PrototypeDiagnosticsPanel>();
            SetReference(diagnostics, "output", diagText);
            SetReference(diagnostics, "input", gameInput);
            SetReference(diagnostics, "actionFeedback", feedback);

            // Test buttons (top-right).
            var controlsPanel = canvasGo.AddComponent<PrototypeControlsPanel>();
            Button mute = CreateButton(root, "MuteButton", "MUTE / UNMUTE", new Vector2(-16f, -90f));
            UnityEventTools.AddPersistentListener(mute.onClick, controlsPanel.ToggleMute);
            Button fullscreen = CreateButton(root, "FullscreenButton", "FULLSCREEN", new Vector2(-16f, -146f));
            UnityEventTools.AddPersistentListener(fullscreen.onClick, controlsPanel.ToggleFullscreen);
            Button link = CreateButton(root, "OpenLinkButton", "OPEN LINK TEST", new Vector2(-16f, -202f));
            link.gameObject.AddComponent<PrototypeLinkButton>();

            CreateOnScreenControls(root);
        }

        private static void CreateOnScreenControls(Transform root)
        {
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var faded = new Color32(0xF5, 0xF1, 0xD9, 0x50);

            // Virtual stick -> <Gamepad>/leftStick -> "Move" action.
            Image stickBase = CreateImage(root, "OnScreenStickBase", faded, knob,
                new Vector2(0f, 0f), new Vector2(150f, 150f), new Vector2(200f, 200f));
            stickBase.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Image stickHandle = CreateImage(stickBase.transform, "OnScreenStickHandle", OffWhite, knob,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            stickHandle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var stick = stickHandle.gameObject.AddComponent<OnScreenStick>();
            stick.movementRange = 60f;
            SetString(stick, "m_ControlPath", "<Gamepad>/leftStick");

            // Virtual action button -> <Gamepad>/buttonSouth -> "Action" action.
            Image actionButton = CreateImage(root, "OnScreenActionButton", Orange, knob,
                new Vector2(1f, 0f), new Vector2(-130f, 130f), new Vector2(140f, 140f));
            actionButton.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            CreateText(actionButton.transform, "Label", "A", 48, Midnight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 140f)).raycastTarget = false;
            var onScreenButton = actionButton.gameObject.AddComponent<OnScreenButton>();
            SetString(onScreenButton, "m_ControlPath", "<Gamepad>/buttonSouth");
        }

        private static Text CreateText(Transform parent, string name, string text, int size, Color color,
            TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), anchor, position, dimensions);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;
            return t;
        }

        private static Image CreateImage(Transform parent, string name, Color color, Sprite sprite,
            Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), anchor, position, dimensions);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            return img;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 topRightOffset)
        {
            Image bg = CreateImage(parent, name, Orange, null, new Vector2(1f, 1f), topRightOffset, new Vector2(240f, 46f));
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            CreateText(bg.transform, "Label", label, 20, Midnight, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 46f));
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

        // ---------------------------------------------------------------- helpers

        private static Sprite LoadSprite(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + fileName);
            if (sprite == null) Debug.LogError($"[TechPoCSceneBuilder] Sprite not found: {SpriteFolder}{fileName}");
            return sprite;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[TechPoCSceneBuilder] Field '{field}' not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            bool isProjectAsset = value != null && !(value is Component) && !(value is GameObject);
            if (value != null && (prop.objectReferenceValue == null || (isProjectAsset && !EditorUtility.IsPersistent(value))))
            {
                _referenceErrors++;
                Debug.LogError($"[TechPoCSceneBuilder] Reference '{field}' on {target.GetType().Name} did not stick.");
            }
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[TechPoCSceneBuilder] Field '{field}' not found on {target.GetType().Name}.");
                return;
            }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
