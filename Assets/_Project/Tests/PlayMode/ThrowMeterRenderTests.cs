using System.Collections;
using NUnit.Framework;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Regression test for the throw meter's target zone not being drawn. The UI canvas is
    /// rendered through a camera into a texture from the start of the scene (the normal
    /// runtime update path), then the pixels in the middle of the zone and of the track are read.
    /// Needs a graphics device: run without -nographics.
    /// </summary>
    public sealed class ThrowMeterRenderTests
    {
        [UnityTest]
        public IEnumerator TargetZone_IsDrawnInZoneColour()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device (-nographics).");

            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var outfielder = Object.FindFirstObjectByType<OutfielderController>();
            var ball = Object.FindFirstObjectByType<BallFlight>();
            var catchSystem = Object.FindFirstObjectByType<CatchSystem>();
            Canvas canvas = null;
            // The game UI canvas (M5 added a second root canvas, "BrandSplash", for the logo splash).
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && c.name == "UI") canvas = c;
            Assert.IsNotNull(canvas);

            var rt = new RenderTexture(1280, 720, 24);
            var cam = new GameObject("UiTestCamera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.magenta;
            cam.cullingMask = 1 << 5; // UI layer
            cam.targetTexture = rt;
            canvas.gameObject.layer = 5;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            yield return null;

            Time.timeScale = 4f;
            try
            {
                flow.StartMission(); yield return null;
                flow.BeginMission(); yield return null;
                outfielder.TeleportTo(ball.Trajectory.Landing);
                while (catchSystem.Cue != CatchCue.Now) yield return null;
                while (ball.Elapsed < catchSystem.PerfectTime - 0.03f) yield return null;
                flow.SimulateActionPress(); yield return null;
                flow.SimulateActionPress(); // skip the catch sequence
                while (flow.State != GameState.Throw) yield return null;
                for (int i = 0; i < 5; i++) yield return null;
                Time.timeScale = 0f; // freeze the needle

                Image zone = null, left = null;
                foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
                {
                    if (img.name == "TargetZone") zone = img;
                    if (img.name == "TrackLeft") left = img;
                    // The needle can sit over either sample point; this test is about the zone.
                    if (img.name == "Needle") img.gameObject.SetActive(false);
                }
                yield return null;   // let the normal canvas update run (no forced rebuild)
                cam.Render();
                Assert.IsNotNull(zone, "TargetZone not found");
                Assert.IsNotNull(left, "TrackLeft not found");

                Color zonePixel = Sample(rt, cam, zone.rectTransform, 0.5f);
                Color leftPixel = Sample(rt, cam, left.rectTransform, 0.1f);
                Assert.That(Distance(zonePixel, zone.color), Is.LessThan(0.08f), $"zone pixel {zonePixel} vs {zone.color}");
                Assert.That(Distance(leftPixel, left.color), Is.LessThan(0.08f), $"track pixel {leftPixel} vs {left.color}");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        private static Color Sample(RenderTexture rt, Camera cam, RectTransform target, float xFraction)
        {
            Rect r = target.rect;
            Vector3 world = target.TransformPoint(new Vector3(r.xMin + r.width * xFraction, r.center.y, 0f));
            Vector3 screen = cam.WorldToScreenPoint(world);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect((int)screen.x, (int)screen.y, 1, 1), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color c = tex.GetPixel(0, 0);
            Object.Destroy(tex);
            return c;
        }

        private static float Distance(Color a, Color b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));
    }
}
