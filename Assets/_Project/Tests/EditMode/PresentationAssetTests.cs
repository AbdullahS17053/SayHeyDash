using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SayHeyDash.Audio;
using SayHeyDash.Data;
using SayHeyDash.Visuals;
using UnityEditor;
using UnityEngine;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// M3 asset checks: every on-screen string can be drawn by the Say Hey Pixel font, every
    /// sound cue has a clip, every outfielder animation has frames, and throw feedback words.
    /// </summary>
    public sealed class PresentationAssetTests
    {
        private const string ContentPath = "Assets/_Project/Data/Content/MissionContent.asset";
        private const string RegularFont = "Assets/_Project/Art/Fonts/SayHeyPixel-Regular.fontsettings";
        private const string BoldFont = "Assets/_Project/Art/Fonts/SayHeyPixel-Bold.fontsettings";
        private const string AudioLibraryPath = "Assets/_Project/Data/Audio/AudioCueLibrary.asset";
        private const string AnimationsPath = "Assets/_Project/Data/Visuals/OutfielderAnimations.asset";

        [TestCase(RegularFont)]
        [TestCase(BoldFont)]
        public void Font_CoversEveryCharacterInMissionContent(string fontPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            Assert.IsNotNull(font, $"{fontPath} missing - run Say Hey Dash > Build Main Scene.");
            var glyphs = new HashSet<int>(font.characterInfo.Select(ci => ci.index));

            var content = AssetDatabase.LoadAssetAtPath<MissionContent>(ContentPath);
            Assert.IsNotNull(content);
            var missing = new List<string>();
            foreach (FieldInfo f in typeof(MissionContent).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType != typeof(string) || f.Name == nameof(MissionContent.museumUrl)) continue;
                var value = (string)f.GetValue(content) ?? string.Empty;
                foreach (char c in value)
                    if (c != '\n' && c != '\r' && !glyphs.Contains(c))
                        missing.Add($"{f.Name}: '{c}' (U+{(int)c:X4})");
            }
            Assert.IsEmpty(missing, "Characters without a glyph:\n" + string.Join("\n", missing.Distinct()));
        }

        [Test]
        public void Font_HasPixelMetrics()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(RegularFont);
            Assert.IsNotNull(font);
            Assert.IsFalse(font.dynamic);
            Assert.AreEqual(10, font.fontSize, "Text.fontSize 40 must mean 4x");
            CharacterInfo a = font.characterInfo.First(ci => ci.index == 'A');
            Assert.AreEqual(6, a.advance, "5 px glyph + 1 px spacing");
            Assert.AreEqual(7, a.maxY, "cap height");
        }

        [Test]
        public void AudioLibrary_HasAClipForEveryCue()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioLibraryPath);
            Assert.IsNotNull(library, "Run Say Hey Dash > Build Main Scene.");
            var missing = Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>()
                .Where(c => c != AudioCue.None && !library.TryGet(c, out _)).ToList();
            Assert.IsEmpty(missing, "Cues without a clip: " + string.Join(", ", missing));
        }

        [Test]
        public void OutfielderAnimations_EveryClipHasFrames()
        {
            var set = AssetDatabase.LoadAssetAtPath<OutfielderAnimationSet>(AnimationsPath);
            Assert.IsNotNull(set, "Run Say Hey Dash > Build Main Scene.");
            foreach (FieldInfo f in typeof(OutfielderAnimationSet).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var clip = (SpriteAnimationClip)f.GetValue(set);
                Assert.IsTrue(clip != null && clip.IsValid && clip.frames.All(s => s != null), $"{f.Name} has no frames");
            }
            Assert.IsFalse(set.catchBall.loop, "the catch is one-shot");
            Assert.IsTrue(set.run.loop);
        }

        [Test]
        public void ThrowFeedback_UsesWordsForEveryOutcome()
        {
            var content = ScriptableObject.CreateInstance<MissionContent>();
            try
            {
                Assert.AreEqual(content.throwPerfectLabel, content.ThrowFeedback(true, false, 0.95f));
                Assert.AreEqual(content.throwGoodLabel, content.ThrowFeedback(true, false, 0.3f));
                Assert.AreEqual(content.throwWideLabel, content.ThrowFeedback(false, false, 0f));
                Assert.AreEqual(content.throwTooSlowLabel, content.ThrowFeedback(false, true, 0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(content);
            }
        }
    }
}
