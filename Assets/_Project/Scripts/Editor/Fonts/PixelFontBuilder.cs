using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SayHeyDash.EditorTools.Fonts
{
    /// <summary>
    /// Turns the generated bitmap font (Tools/ArtGen -> Art/Fonts/sayhey_pixel_atlas.png +
    /// sayhey_pixel_metrics.json) into two legacy Unity custom fonts, "Say Hey Pixel" Regular
    /// and Bold, used by uGUI Text. The typeface is original to the project, so no third-party
    /// font licence applies.
    /// Font units: 1 font pixel = 1 unit and the font size is 10, so a Text with fontSize 40
    /// draws each font pixel as 4 canvas units (= one game pixel on the 1280x720 canvas).
    /// Menu: Say Hey Dash > Rebuild Pixel Fonts.
    /// </summary>
    public static class PixelFontBuilder
    {
        public const string FontFolder = "Assets/_Project/Art/Fonts/";
        public const string AtlasPath = FontFolder + "sayhey_pixel_atlas.png";
        public const string MetricsPath = FontFolder + "sayhey_pixel_metrics.json";
        public const string RegularPath = FontFolder + "SayHeyPixel-Regular.fontsettings";
        public const string BoldPath = FontFolder + "SayHeyPixel-Bold.fontsettings";
        /// <summary>Font size stored in the font asset; Text.fontSize / NativeSize = pixel scale.</summary>
        public const int NativeSize = 10;

        [Serializable] private sealed class Glyph { public int code, x, y, w, h, advance; }
        [Serializable] private sealed class Face { public string name; public Glyph[] glyphs; }
        [Serializable]
        private sealed class Metrics
        {
            public int cellRows, ascent, descent, lineHeight, atlasWidth, atlasHeight;
            public Face[] faces;
        }

        [MenuItem("Say Hey Dash/Rebuild Pixel Fonts", priority = 20)]
        public static void RebuildMenu()
        {
            if (Build(out _, out _)) Debug.Log("[PixelFontBuilder] Fonts rebuilt.");
        }

        public static bool Build(out Font regular, out Font bold)
        {
            regular = bold = null;
            if (!File.Exists(MetricsPath))
            {
                Debug.LogError($"[PixelFontBuilder] {MetricsPath} not found - run Tools/ArtGen/generate_m3_art.py.");
                return false;
            }
            EnsureAtlasImport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (atlas == null)
            {
                Debug.LogError($"[PixelFontBuilder] Atlas not found: {AtlasPath}");
                return false;
            }
            var metrics = JsonUtility.FromJson<Metrics>(File.ReadAllText(MetricsPath));
            if (metrics?.faces == null || metrics.faces.Length < 2)
            {
                Debug.LogError("[PixelFontBuilder] Metrics file is invalid.");
                return false;
            }
            regular = BuildFace(metrics, metrics.faces[0], atlas, RegularPath);
            bold = BuildFace(metrics, metrics.faces[1], atlas, BoldPath);
            AssetDatabase.SaveAssets();
            return regular != null && bold != null;
        }

        private static Font BuildFace(Metrics m, Face face, Texture2D atlas, string path)
        {
            string matPath = Path.ChangeExtension(path, ".mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find("UI/Default")) { name = Path.GetFileNameWithoutExtension(matPath) };
                AssetDatabase.CreateAsset(material, matPath);
            }
            material.mainTexture = atlas;
            EditorUtility.SetDirty(material);

            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            bool create = font == null;
            if (create) font = new Font(Path.GetFileNameWithoutExtension(path));
            font.material = material;

            var infos = new List<CharacterInfo>();
            float w = m.atlasWidth, h = m.atlasHeight;
            foreach (Glyph g in face.glyphs)
            {
                var ci = new CharacterInfo { index = g.code, advance = g.advance };
                if (g.w > 0 && g.h > 0)
                {
                    float u0 = g.x / w, u1 = (g.x + g.w) / w;
                    float vTop = 1f - g.y / h, vBottom = 1f - (g.y + g.h) / h;
                    ci.uvBottomLeft = new Vector2(u0, vBottom);
                    ci.uvBottomRight = new Vector2(u1, vBottom);
                    ci.uvTopLeft = new Vector2(u0, vTop);
                    ci.uvTopRight = new Vector2(u1, vTop);
                    ci.minX = 0;
                    ci.maxX = g.w;
                    ci.minY = -m.descent;
                    ci.maxY = g.h - m.descent;
                }
                infos.Add(ci);
                // Lower case shares the capitals (all game text is upper case, but be forgiving).
                if (g.code >= 'A' && g.code <= 'Z')
                {
                    CharacterInfo lower = ci;
                    lower.index = g.code + 32;
                    infos.Add(lower);
                }
            }
            font.characterInfo = infos.ToArray();

            if (create) AssetDatabase.CreateAsset(font, path);
            var so = new SerializedObject(font);
            SetNumber(so, "m_FontSize", NativeSize);
            SetNumber(so, "m_LineSpacing", m.lineHeight);
            SetNumber(so, "m_Ascent", m.ascent);
            SetNumber(so, "m_Descent", -m.descent);
            SetNumber(so, "m_CharacterSpacing", 0);
            SetNumber(so, "m_CharacterPadding", 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font);
            return font;
        }

        private static void SetNumber(SerializedObject so, string name, float value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
            else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(value);
        }

        private static void EnsureAtlasImport()
        {
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null) return;
            bool dirty = importer.textureType != TextureImporterType.Default ||
                         importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                         importer.textureCompression != TextureImporterCompression.Uncompressed ||
                         importer.npotScale != TextureImporterNPOTScale.None ||
                         importer.alphaSource != TextureImporterAlphaSource.FromInput;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
