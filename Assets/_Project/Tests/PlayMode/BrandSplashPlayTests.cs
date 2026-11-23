using System.Collections;
using NUnit.Framework;
using SayHeyDash.Flow;
using SayHeyDash.Platform;
using SayHeyDash.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Milestone 5: the client-logo splash played by the web page's start click (BrowserEvents.OnBrowserStart).
    /// </summary>
    public sealed class BrandSplashPlayTests
    {
        private BrowserEvents _events;
        private BrandSplashView _splash;
        private GameFlowController _flow;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            BrandSplash.ResetForTests();
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _events = Object.FindFirstObjectByType<BrowserEvents>();
            _splash = Object.FindFirstObjectByType<BrandSplashView>();
            _flow = Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsNotNull(_splash, "Main scene has no BrandSplashView - run Say Hey Dash > Build Main Scene.");
        }

        [UnityTest]
        public IEnumerator StartClick_ShowsLogoOnTop_MutesMenus_ThenHandsBackTheTitle()
        {
            var canvas = _splash.GetComponent<Canvas>();
            Assert.IsFalse(canvas.enabled, "hidden until the start click");
            var logo = _splash.transform.Find("Logo").GetComponent<Image>();
            Assert.IsNotNull(logo.sprite);
            StringAssert.Contains("FastballFuel", logo.sprite.name);
            Assert.That(canvas.sortingOrder, Is.GreaterThan(0), "drawn above the game UI");

            _events.OnBrowserStart();
            yield return null;
            Assert.IsTrue(_splash.IsShowing);
            Assert.IsTrue(canvas.enabled);
            Assert.IsFalse(EventSystem.current.sendNavigationEvents, "Space/Enter must not start a mission behind the logo");
            Assert.AreEqual(GameState.Title, _flow.State);

            float start = Time.realtimeSinceStartup;
            while (_splash.IsShowing && Time.realtimeSinceStartup - start < 6f) yield return null;
            float shown = Time.realtimeSinceStartup - start;
            Assert.IsFalse(_splash.IsShowing, "splash should end by itself");
            Assert.That(shown, Is.InRange(2.5f, 4.5f));
            Assert.IsFalse(canvas.enabled);
            Assert.IsTrue(EventSystem.current.sendNavigationEvents, "menus work again");

            _events.OnBrowserStart(); // a second start message in the same page load
            yield return null;
            Assert.IsFalse(_splash.IsShowing, "once per page load");
            Assert.AreEqual(1, _splash.TimesShown);
        }

        [UnityTest]
        public IEnumerator Splash_CanBeSkipped()
        {
            _splash.Play();
            yield return null;
            _splash.Skip();
            float start = Time.realtimeSinceStartup;
            while (_splash.IsShowing && Time.realtimeSinceStartup - start < 3f) yield return null;
            Assert.IsFalse(_splash.IsShowing);
            Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(1.5f));
        }
    }
}
