using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SayHeyDash.EditorTools.Import
{
    /// <summary>
    /// Applies the project's pixel-art import rules to every NEW texture placed under
    /// Assets/_Project/Art/Sprites/: Sprite, 16 PPU, Point filtering, no mipmaps, no compression.
    /// A file name containing <c>_slice{N}</c> (e.g. ui_panel_slice3.png) also gets an N-pixel
    /// 9-slice border on every side. Only runs on first import, so manual per-asset overrides
    /// are preserved. <see cref="Apply"/> lets tools re-apply the rules on demand.
    /// </summary>
    public sealed class PixelArtTexturePostprocessor : AssetPostprocessor
    {
        public const string SpritesRoot = "Assets/_Project/Art/Sprites/";
        public const int PixelsPerUnit = 16;

        private static readonly Regex SliceRegex = new Regex(@"_slice(\d+)", RegexOptions.Compiled);

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesRoot)) return;
            if (!assetImporter.importSettingsMissing) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        /// <summary>Sets the pixel-art sprite settings. Returns true if anything changed.</summary>
        public static bool Apply(TextureImporter importer, string path)
        {
            bool changed = false;
            void Set<T>(T current, T wanted, System.Action<T> set)
            {
                if (Equals(current, wanted)) return;
                set(wanted);
                changed = true;
            }

            Set(importer.textureType, TextureImporterType.Sprite, v => importer.textureType = v);
            Set(importer.spriteImportMode, SpriteImportMode.Single, v => importer.spriteImportMode = v);
            Set(importer.spritePixelsPerUnit, (float)PixelsPerUnit, v => importer.spritePixelsPerUnit = v);
            Set(importer.filterMode, FilterMode.Point, v => importer.filterMode = v);
            Set(importer.mipmapEnabled, false, v => importer.mipmapEnabled = v);
            Set(importer.alphaIsTransparency, true, v => importer.alphaIsTransparency = v);
            Set(importer.wrapMode, TextureWrapMode.Clamp, v => importer.wrapMode = v);
            Set(importer.textureCompression, TextureImporterCompression.Uncompressed, v => importer.textureCompression = v);

            Match m = SliceRegex.Match(System.IO.Path.GetFileNameWithoutExtension(path));
            if (m.Success)
            {
                float n = float.Parse(m.Groups[1].Value);
                var border = new Vector4(n, n, n, n);
                Set(importer.spriteBorder, border, v => importer.spriteBorder = v);
                // 9-slice sprites need a full-rect mesh.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    changed = true;
                }
            }
            return changed;
        }
    }
}
