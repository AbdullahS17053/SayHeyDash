using SayHeyDash.Flow;
using SayHeyDash.UI;
using UnityEngine;

namespace SayHeyDash.Platform
{
    /// <summary>
    /// Receives messages from the web page (WebGL template) through
    /// <c>unityInstance.SendMessage("BrowserEvents", ...)</c>. The GameObject MUST be named
    /// <see cref="GameObjectName"/>. The page pauses the game when the tab is hidden, when the
    /// player clicks outside the game (embedded page loses focus) and when the "rotate your
    /// device" overlay covers it - so a mission is never lost to something outside the game.
    /// Pausing outside live play is a no-op (the flow controller decides).
    /// Milestone 5: the page's start click plays the client-logo splash.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BrowserEvents : MonoBehaviour
    {
        public const string GameObjectName = "BrowserEvents";

        [SerializeField] private GameFlowController flow;
        [SerializeField] private BrandSplashView splash;

        public int PauseRequests { get; private set; }

        private void Awake()
        {
            if (flow == null || splash == null)
            {
                Debug.LogError($"[{nameof(BrowserEvents)}] Missing reference.", this);
                enabled = false;
            }
            if (gameObject.name != GameObjectName)
                Debug.LogError($"[{nameof(BrowserEvents)}] GameObject must be named '{GameObjectName}' for the web page to reach it.", this);
        }

        /// <summary>Called by the page: tab hidden, focus lost or device rotated to portrait.</summary>
        public void OnBrowserPause()
        {
            if (!enabled) return;
            PauseRequests++;
            flow.Pause();
        }

        /// <summary>
        /// Called by the page when the player clicks "click or tap to play": shows the client-logo
        /// splash once per page load (it replaces the start-up splash that <see cref="BrandSplash"/>
        /// holds back in WebGL, which would otherwise play unseen behind the page's start gate).
        /// </summary>
        public void OnBrowserStart()
        {
            if (!enabled || !BrandSplash.ClaimStartSplash()) return;
            splash.Play();
        }
    }
}
