using UnityEngine;
using UnityEngine.Rendering;

namespace SayHeyDash.Platform
{
    /// <summary>
    /// The client's logo splash (Milestone 5). Player Settings &gt; Splash Image holds the logo
    /// (set up by <i>Say Hey Dash &gt; Branding</i>) and shows it at start-up on every platform
    /// except the web: there the page covers the game with "click or tap to play" until the first
    /// click, so a start-up splash would play unseen behind it - and Unity's splash cannot be
    /// restarted onto the WebGL canvas afterwards. In WebGL builds it is therefore stopped at
    /// start-up, and <see cref="UI.BrandSplashView"/> shows the same logo right after the click
    /// (<see cref="BrowserEvents.OnBrowserStart"/>). The Editor never shows Unity's splash.
    /// </summary>
    public static class BrandSplash
    {
        /// <summary>True in WebGL builds, where the logo is shown after the page's start click.</summary>
        public static bool ShownAfterStartClick { get; private set; }

        private static bool _claimed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void HoldUntilPageStart()
        {
            _claimed = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            ShownAfterStartClick = true;
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
#endif
        }

        /// <summary>
        /// Returns true the first time it is called after start-up (the start click shows the
        /// logo once per page load), false afterwards.
        /// </summary>
        public static bool ClaimStartSplash()
        {
            if (_claimed) return false;
            _claimed = true;
            return true;
        }

        /// <summary>Test hook: behave as if the page had just loaded.</summary>
        internal static void ResetForTests() => _claimed = false;
    }
}
