using UnityEditor;
using UnityEngine;

namespace SayHeyDash.EditorTools.Import
{
    /// <summary>
    /// Applies the project's pixel-art import rules to every NEW texture placed under
    /// Assets/_Project/Art/Sprites/: Sprite, 16 PPU, Point filtering, no mipmaps, no compression.
    /// Only runs on first import, so manual per-asset overrides are preserved.
    /// </summary>
    public sealed class PixelArtTexturePostprocessor : AssetPostprocessor
    {
        public const string SpritesRoot = "Assets/_Project/Art/Sprites/";
        public const int PixelsPerUnit = 16;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesRoot)) return;
            if (!assetImporter.importSettingsMissing) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
