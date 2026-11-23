using System.Runtime.InteropServices;
using UnityEngine;

namespace SayHeyDash.Platform
{
    /// <summary>
    /// Thin C# facade over Plugins/WebGL/BrowserBridge.jslib.
    /// Every call is safe to make in the Editor and on non-web platforms (sensible fallbacks).
    /// </summary>
    public static class BrowserBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int SHD_IsEmbedded();
        [DllImport("__Internal")] private static extern int SHD_IsTouchDevice();
        [DllImport("__Internal")] private static extern int SHD_HasFocus();
        [DllImport("__Internal")] private static extern double SHD_GetDevicePixelRatio();
        [DllImport("__Internal")] private static extern void SHD_OpenUrlOnNextGesture(string url);
        [DllImport("__Internal")] private static extern int SHD_PrefersReducedMotion();
        [DllImport("__Internal")] private static extern int SHD_StorageAvailable();
#endif

        /// <summary>
        /// The browser / OS "reduce motion" accessibility preference. Used as the default for the
        /// game's reduced-motion setting on a first visit. Editor: false.
        /// </summary>
        public static bool PrefersReducedMotion
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return SHD_PrefersReducedMotion() == 1;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// False when the browser blocks storage for this page (e.g. a partitioned or sandboxed
        /// third-party iframe). Settings then last for the current visit only.
        /// </summary>
        public static bool StorageAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return SHD_StorageAvailable() == 1;
#else
                return true;
#endif
            }
        }

        public static bool IsWebPlayer =>
#if UNITY_WEBGL && !UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <summary>True when the game runs inside an iframe (e.g. a Wix embed).</summary>
        public static bool IsEmbedded
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return SHD_IsEmbedded() == 1;
#else
                return false;
#endif
            }
        }

        /// <summary>True when the browser reports a touch-capable device.</summary>
        public static bool IsTouchDevice
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return SHD_IsTouchDevice() == 1;
#else
                return Application.isMobilePlatform || UnityEngine.InputSystem.Touchscreen.current != null;
#endif
            }
        }

        /// <summary>True when the page (or iframe) document currently holds keyboard focus.</summary>
        public static bool HasFocus
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return SHD_HasFocus() == 1;
#else
                return Application.isFocused;
#endif
            }
        }

        public static float DevicePixelRatio
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return (float)SHD_GetDevicePixelRatio();
#else
                return 1f;
#endif
            }
        }

        /// <summary>
        /// Opens a URL in a new tab. On WebGL the browser only allows popups inside a genuine
        /// user gesture, so the URL is opened on the next pointer-up / key-up event.
        /// Call this from a pointer-DOWN handler so the matching pointer-up opens the link.
        /// </summary>
        public static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"[{nameof(BrowserBridge)}] OpenUrl called with an empty URL.");
                return;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            SHD_OpenUrlOnNextGesture(url);
#else
            Application.OpenURL(url);
#endif
        }
    }
}
