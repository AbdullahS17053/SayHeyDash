using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SayHeyDash.EditorTools.Branding
{
    /// <summary>
    /// Client branding (Milestone 5). Takes the client-supplied logo
    /// <see cref="SourceLogoPath"/> (never modified) and
    /// 1. generates the derived files: a trimmed transparent logo (splash + web loading screen),
    ///    a square 1024 px app icon, and the web page's favicon / home-screen icon;
    /// 2. applies them to Player Settings: default icon, splash screen (client logo on black,
    ///    no Unity logo), company / product name.
    ///
    /// To change the logo: replace FastballFuel_Logo.png (same name, any size, black or
    /// transparent background) and run <b>Say Hey Dash &gt; Branding &gt; Rebuild Logo Files + Apply</b>.
    /// WebGL builds re-apply the Player Settings part automatically (<see cref="Build.WebGLBuildPipeline"/>).
    ///
    /// Batchmode: Unity.exe -batchmode -quit -projectPath &lt;path&gt;
    ///            -executeMethod SayHeyDash.EditorTools.Branding.BrandingBuilder.BuildFromCommandLine
    /// </summary>
    public static class BrandingBuilder
    {
        public const string Folder = "Assets/_Project/Art/Branding";
        public const string SourceLogoPath = Folder + "/FastballFuel_Logo.png";
        public const string LogoAlphaPath = Folder + "/FastballFuel_Logo_Alpha.png";
        public const string IconPath = Folder + "/FastballFuel_Icon.png";
        public const string TemplateDataFolder = "Assets/WebGLTemplates/SayHeyDash/TemplateData";
        public const string FaviconPath = TemplateDataFolder + "/favicon.png";
        public const string TouchIconPath = TemplateDataFolder + "/apple-touch-icon.png";
        public const string WebLogoPath = TemplateDataFolder + "/logo.png";

        public const string CompanyName = "Fastball Fuel Coffee Company";
        public const string ProductName = "The Say Hey Dash";

        public const int IconSize = 1024;
        public const int FaviconSize = 64;
        public const int TouchIconSize = 180;
        public const int WebLogoMaxWidth = 640;
        public const float SplashLogoSeconds = 2.5f;
        public static readonly Color SplashBackground = Color.black;

        /// <summary>Pixels darker than this (0-255, brightest channel) count as background.</summary>
        private const int BackgroundFloor = 10;
        /// <summary>Margin kept around the trimmed logo, as a fraction of its larger side.</summary>
        private const float TrimPadding = 0.04f;
        /// <summary>Share of the icon's width the logo may use.</summary>
        private const float IconLogoWidth = 0.92f;

        [MenuItem("Say Hey Dash/Branding/Rebuild Logo Files + Apply", priority = 40)]
        public static void RebuildAndApply()
        {
            if (GenerateFiles()) ApplyPlayerSettings();
        }

        [MenuItem("Say Hey Dash/Branding/Apply To Player Settings", priority = 41)]
        public static void ApplyFromMenu() => ApplyPlayerSettings();

        public static void BuildFromCommandLine()
        {
            bool ok = GenerateFiles() && ApplyPlayerSettings();
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        // ------------------------------------------------------------------ files

        /// <summary>Writes the derived logo/icon files from the source logo. Returns false on error.</summary>
        public static bool GenerateFiles()
        {
            if (!File.Exists(SourceLogoPath))
            {
                Debug.LogError($"[BrandingBuilder] Source logo not found: {SourceLogoPath}");
                return false;
            }

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(SourceLogoPath)))
            {
                Debug.LogError($"[BrandingBuilder] Could not read {SourceLogoPath}");
                return false;
            }

            Image src = Image.From(source);
            UnityEngine.Object.DestroyImmediate(source);

            RectInt content = FindContent(src);
            int pad = Mathf.RoundToInt(Mathf.Max(content.width, content.height) * TrimPadding);
            RectInt crop = Expand(content, pad, src.Width, src.Height);

            Image logoAlpha = ToTransparent(src.Crop(crop));
            Image logoOnBlack = src.Crop(crop).Flatten(Color.black);

            // Square icon: logo centred on black, as wide as the icon allows.
            float scale = IconSize * IconLogoWidth / logoOnBlack.Width;
            Image scaledLogo = logoOnBlack.Resize(Mathf.RoundToInt(logoOnBlack.Width * scale),
                                                  Mathf.RoundToInt(logoOnBlack.Height * scale));
            Image icon = Image.Filled(IconSize, IconSize, Color.black);
            icon.Paste(scaledLogo, (IconSize - scaledLogo.Width) / 2, (IconSize - scaledLogo.Height) / 2);

            int webWidth = Mathf.Min(WebLogoMaxWidth, logoAlpha.Width);
            Image webLogo = logoAlpha.Resize(webWidth, Mathf.RoundToInt(logoAlpha.Height * (webWidth / (float)logoAlpha.Width)));

            Write(LogoAlphaPath, logoAlpha);
            Write(IconPath, icon);
            Write(FaviconPath, icon.Resize(FaviconSize, FaviconSize));
            Write(TouchIconPath, icon.Resize(TouchIconSize, TouchIconSize));
            Write(WebLogoPath, webLogo);

            AssetDatabase.Refresh();
            ConfigureImporters();
            Debug.Log($"[BrandingBuilder] Logo files rebuilt from {SourceLogoPath} " +
                      $"(logo {logoAlpha.Width}x{logoAlpha.Height}, icon {IconSize}x{IconSize}).");
            return true;
        }

        private static void ConfigureImporters()
        {
            // The source logo is reference material only (never shipped): keep it out of the build's texture settings.
            Configure(SourceLogoPath, sprite: false, maxSize: 1024);
            Configure(LogoAlphaPath, sprite: true, maxSize: 1024);
            Configure(IconPath, sprite: false, maxSize: IconSize);
        }

        private static void Configure(string path, bool sprite, int maxSize)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
            }
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = !sprite;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void Write(string path, Image image)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var tex = image.ToTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        // ------------------------------------------------------------------ player settings

        /// <summary>Applies icon, splash screen and names. Returns false if the logo files are missing.</summary>
        public static bool ApplyPlayerSettings(bool saveAssets = true)
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoAlphaPath);
            if (icon == null || logo == null)
            {
                Debug.LogError("[BrandingBuilder] Logo files missing. Run Say Hey Dash > Branding > Rebuild Logo Files + Apply.");
                return false;
            }

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            // Default icon (used by every platform that has no icon of its own).
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            // Splash screen: the client logo on black. The "Made with Unity" logo is optional since Unity 6.
            // WebGL builds stop this start-up splash (it would play hidden behind the page's "click to play"
            // gate) and show the same logo in-game right after that click instead (Platform/BrandSplash, UI/BrandSplashView).
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.backgroundColor = SplashBackground;
            PlayerSettings.SplashScreen.background = null;
            PlayerSettings.SplashScreen.backgroundPortrait = null;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(SplashLogoSeconds, logo) };

            if (saveAssets) AssetDatabase.SaveAssets();
            return true;
        }

        // ------------------------------------------------------------------ image maths

        private static RectInt FindContent(Image img)
        {
            int minX = img.Width, minY = img.Height, maxX = -1, maxY = -1;
            for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if (Brightness(img[x, y]) <= BackgroundFloor || img[x, y].a < 0.05f) continue;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
            return maxX < 0 ? new RectInt(0, 0, img.Width, img.Height)
                            : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static RectInt Expand(RectInt r, int pad, int w, int h)
        {
            int x0 = Math.Max(0, r.xMin - pad), y0 = Math.Max(0, r.yMin - pad);
            int x1 = Math.Min(w, r.xMax + pad), y1 = Math.Min(h, r.yMax + pad);
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }

        private static int Brightness(Color c) =>
            Mathf.RoundToInt(Mathf.Max(c.r, Mathf.Max(c.g, c.b)) * c.a * 255f);

        /// <summary>
        /// Logo on black -> logo on transparent ("unmultiply" black): alpha comes from brightness, colour is
        /// divided back out, so glows and soft edges keep their shape on any background.
        /// Pixels that are already transparent keep their own alpha.
        /// </summary>
        private static Image ToTransparent(Image img)
        {
            var result = new Image(img.Width, img.Height);
            float floor = BackgroundFloor / 255f;
            for (int i = 0; i < img.Pixels.Length; i++)
            {
                Color c = img.Pixels[i];
                float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                float a = Mathf.Clamp01((m - floor) / (1f - floor)) * c.a;
                result.Pixels[i] = a <= 0f
                    ? new Color(0, 0, 0, 0)
                    : new Color(Mathf.Clamp01(c.r / m), Mathf.Clamp01(c.g / m), Mathf.Clamp01(c.b / m), a);
            }
            return result;
        }

        /// <summary>Small RGBA float image (y = 0 is the bottom row, as in Unity textures).</summary>
        private sealed class Image
        {
            public readonly int Width, Height;
            public readonly Color[] Pixels;

            public Image(int w, int h) { Width = w; Height = h; Pixels = new Color[w * h]; }

            public Color this[int x, int y] => Pixels[y * Width + x];

            public static Image From(Texture2D tex)
            {
                var img = new Image(tex.width, tex.height);
                Array.Copy(tex.GetPixels(), img.Pixels, img.Pixels.Length);
                return img;
            }

            public static Image Filled(int w, int h, Color c)
            {
                var img = new Image(w, h);
                for (int i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = c;
                return img;
            }

            public Texture2D ToTexture()
            {
                var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                tex.SetPixels(Pixels);
                tex.Apply();
                return tex;
            }

            public Image Crop(RectInt r)
            {
                var img = new Image(r.width, r.height);
                for (int y = 0; y < r.height; y++)
                    Array.Copy(Pixels, (r.y + y) * Width + r.x, img.Pixels, y * r.width, r.width);
                return img;
            }

            /// <summary>Composites over a solid colour (removes transparency).</summary>
            public Image Flatten(Color bg)
            {
                var img = new Image(Width, Height);
                for (int i = 0; i < Pixels.Length; i++)
                {
                    Color c = Pixels[i];
                    img.Pixels[i] = new Color(Mathf.Lerp(bg.r, c.r, c.a), Mathf.Lerp(bg.g, c.g, c.a), Mathf.Lerp(bg.b, c.b, c.a), 1f);
                }
                return img;
            }

            public void Paste(Image other, int ox, int oy)
            {
                for (int y = 0; y < other.Height; y++)
                for (int x = 0; x < other.Width; x++)
                {
                    int tx = ox + x, ty = oy + y;
                    if (tx < 0 || ty < 0 || tx >= Width || ty >= Height) continue;
                    Pixels[ty * Width + tx] = other[x, y];
                }
            }

            /// <summary>
            /// Supersampled bilinear resize in premultiplied alpha: averages k x k bilinear taps per output
            /// pixel (k grows with the reduction), so downscales stay smooth and upscales stay sharp.
            /// </summary>
            public Image Resize(int w, int h)
            {
                var img = new Image(w, h);
                float sx = Width / (float)w, sy = Height / (float)h;
                int kx = Mathf.Clamp(Mathf.CeilToInt(sx), 1, 8), ky = Mathf.Clamp(Mathf.CeilToInt(sy), 1, 8);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int j = 0; j < ky; j++)
                    for (int i = 0; i < kx; i++)
                    {
                        float u = (x + (i + 0.5f) / kx) * sx - 0.5f;
                        float v = (y + (j + 0.5f) / ky) * sy - 0.5f;
                        Color c = Sample(u, v);
                        r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                    }
                    img.Pixels[y * w + x] = a <= 0f ? new Color(0, 0, 0, 0) : new Color(r / a, g / a, b / a, a / (kx * ky));
                }
                return img;
            }

            private Color Sample(float u, float v)
            {
                u = Mathf.Clamp(u, 0, Width - 1); v = Mathf.Clamp(v, 0, Height - 1);
                int x0 = (int)u, y0 = (int)v;
                int x1 = Math.Min(x0 + 1, Width - 1), y1 = Math.Min(y0 + 1, Height - 1);
                float fx = u - x0, fy = v - y0;
                Color c00 = this[x0, y0], c10 = this[x1, y0], c01 = this[x0, y1], c11 = this[x1, y1];
                // Premultiplied interpolation keeps transparent pixels from darkening the edges.
                Color P(Color c) => new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                Color top = Color.LerpUnclamped(P(c00), P(c10), fx);
                Color bottom = Color.LerpUnclamped(P(c01), P(c11), fx);
                Color p = Color.LerpUnclamped(top, bottom, fy);
                return p.a <= 0f ? new Color(0, 0, 0, 0) : new Color(p.r / p.a, p.g / p.a, p.b / p.a, p.a);
            }
        }
    }
}
