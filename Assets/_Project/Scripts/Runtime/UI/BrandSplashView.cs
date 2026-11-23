using System.Collections;
using SayHeyDash.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// The client-logo splash shown in the web build right after "CLICK OR TAP TO PLAY"
    /// (Milestone 5). Same look as the Player Settings splash: the Fastball Fuel logo on black.
    /// Unity's own splash can't be shown after that click in WebGL, so the game draws it here, on a
    /// canvas above everything else: fade in, hold, fade out (unscaled time, a gentle zoom unless
    /// reduced motion is on). A click, tap or key press after a short moment skips it. Menu keys
    /// are muted while it shows so Space / Enter can't start a mission behind the logo.
    /// Started by <see cref="Platform.BrowserEvents.OnBrowserStart"/>; other platforms use the
    /// Player Settings splash at start-up instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BrandSplashView : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform logo;

        [Header("Timing (seconds, unscaled)")]
        [SerializeField, Min(0f)] private float fadeIn = 0.4f;
        [SerializeField, Min(0f)] private float hold = 2.2f;
        [SerializeField, Min(0f)] private float fadeOut = 0.5f;
        [SerializeField, Min(0f)] private float skippableAfter = 0.6f;
        [SerializeField, Min(1f)] private float zoomTo = 1.06f;

        public bool IsShowing { get; private set; }
        public int TimesShown { get; private set; }

        private Coroutine _routine;

        private void Awake()
        {
            if (canvas == null || group == null || logo == null)
            {
                Debug.LogError($"[{nameof(BrandSplashView)}] Missing reference.", this);
                enabled = false;
                return;
            }
            canvas.enabled = false;
        }

        /// <summary>Shows the splash (restarts it if it is already showing).</summary>
        public void Play()
        {
            if (!enabled) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run());
        }

        /// <summary>Jumps to the fade-out (as a click or key press does).</summary>
        public void Skip() => _skip = true;

        private bool _skip;

        private IEnumerator Run()
        {
            IsShowing = true;
            TimesShown++;
            _skip = false;
            bool reduced = flow != null && flow.ReducedMotion;
            EventSystem events = EventSystem.current;
            bool navigation = events != null && events.sendNavigationEvents;
            if (events != null) events.sendNavigationEvents = false;

            canvas.enabled = true;
            float total = fadeIn + hold, t = 0f;
            while (t < total && !_skip)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = fadeIn > 0f ? Mathf.Clamp01(t / fadeIn) : 1f;
                logo.localScale = Vector3.one * (reduced ? 1f : Mathf.Lerp(1f, zoomTo, t / (total + fadeOut)));
                if (t > skippableAfter && AnyPress()) _skip = true;
                yield return null;
            }

            float startAlpha = group.alpha;
            for (float f = 0f; f < fadeOut; f += Time.unscaledDeltaTime)
            {
                group.alpha = startAlpha * (1f - f / fadeOut);
                yield return null;
            }

            group.alpha = 0f;
            canvas.enabled = false;
            // Release the menu keys one frame later, so the press that skipped the logo isn't also a menu press.
            yield return null;
            if (events != null) events.sendNavigationEvents = navigation;
            IsShowing = false;
            _routine = null;
        }

        private static bool AnyPress()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            var pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        }
    }
}
