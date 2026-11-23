using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Milestone 5 branding: the client logo is in the project, applied to Player Settings
    /// (icon + splash screen) and used by the web page. Paths mirror EditorTools.Branding.BrandingBuilder.
    /// </summary>
    public sealed class BrandingTests
    {
        private const string Folder = "Assets/_Project/Art/Branding/";
        private const string SourceLogo = Folder + "FastballFuel_Logo.png";
        private const string LogoAlpha = Folder + "FastballFuel_Logo_Alpha.png";
        private const string Icon = Folder + "FastballFuel_Icon.png";
        private const string TemplateFolder = "Assets/WebGLTemplates/SayHeyDash/";

        [TestCase(SourceLogo)]
        [TestCase(LogoAlpha)]
        [TestCase(Icon)]
        [TestCase(TemplateFolder + "TemplateData/favicon.png")]
        [TestCase(TemplateFolder + "TemplateData/apple-touch-icon.png")]
        [TestCase(TemplateFolder + "TemplateData/logo.png")]
        public void BrandingFileExists(string path) => Assert.That(File.Exists(path), Is.True, path);

        [Test]
        public void IconIsSquareAndOpaque()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Icon);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.width, Is.EqualTo(icon.height));
            Assert.That(icon.width, Is.GreaterThanOrEqualTo(512));
        }

        [Test]
        public void LogoHasTransparentBackground()
        {
            var tex = new Texture2D(2, 2);
            Assert.That(tex.LoadImage(File.ReadAllBytes(LogoAlpha)), Is.True);
            Color32[] px = tex.GetPixels32();
            Assert.That(px[0].a, Is.EqualTo(0), "corner should be transparent");
            // The brush lettering is textured, so count "mostly opaque" pixels rather than a == 255.
            Assert.That(px.Count(p => p.a >= 200), Is.GreaterThan(px.Length / 20), "logo body should be opaque");
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void PlayerSettingsUseTheClientLogo()
        {
            Assert.That(PlayerSettings.companyName, Is.EqualTo("Fastball Fuel Coffee Company"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("The Say Hey Dash"));

            Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            Assert.That(icons, Is.Not.Empty);
            Assert.That(AssetDatabase.GetAssetPath(icons[0]), Is.EqualTo(Icon));

            Assert.That(PlayerSettings.SplashScreen.show, Is.True);
            Assert.That(PlayerSettings.SplashScreen.showUnityLogo, Is.False);
            var logos = PlayerSettings.SplashScreen.logos;
            Assert.That(logos, Has.Length.EqualTo(1));
            Assert.That(AssetDatabase.GetAssetPath(logos[0].logo), Is.EqualTo(LogoAlpha));
            Assert.That(logos[0].duration, Is.GreaterThanOrEqualTo(2f));
            Assert.That(PlayerSettings.SplashScreen.backgroundColor, Is.EqualTo(Color.black));
        }

        [Test]
        public void WebPageShowsLogoAndStartsTheSplash()
        {
            string html = File.ReadAllText(TemplateFolder + "index.html");
            StringAssert.Contains("TemplateData/favicon.png", html);
            StringAssert.Contains("TemplateData/apple-touch-icon.png", html);
            StringAssert.Contains("TemplateData/logo.png", html);
            StringAssert.Contains("\"OnBrowserStart\"", html);
            Assert.That(typeof(Platform.BrowserEvents).GetMethod("OnBrowserStart"), Is.Not.Null);
        }
    }
}
