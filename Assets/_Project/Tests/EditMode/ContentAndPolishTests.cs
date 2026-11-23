using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SayHeyDash.Audio;
using SayHeyDash.Data;
using SayHeyDash.Replay;
using SayHeyDash.Scoring;
using SayHeyDash.Settings;
using UnityEditor;
using UnityEngine;

namespace SayHeyDash.Tests
{
    /// <summary>
    /// Milestone 4 rules: settings persistence (best-effort), the replay never shows an
    /// alternative history (AC-11), next-rank hints (AC-06), replay text fits its box, text
    /// colours meet WCAG contrast (AC-12) and the sound mix keeps its order.
    /// </summary>
    public sealed class ContentAndPolishTests
    {
        private const string ContentPath = "Assets/_Project/Data/Content/MissionContent.asset";
        private const string RegularFont = "Assets/_Project/Art/Fonts/SayHeyPixel-Regular.fontsettings";
        private const string AudioLibraryPath = "Assets/_Project/Data/Audio/AudioCueLibrary.asset";

        // ------------------------------------------------------------------ settings

        [Test]
        public void Settings_FirstVisit_UsesDefaults_AndFollowsReducedMotionPreference()
        {
            var store = new MemorySettingsStore();
            SettingsData d = SettingsSerializer.Load(store, reducedMotionDefault: true);
            Assert.IsFalse(d.Muted);
            Assert.AreEqual(SettingsData.VolumeSteps, d.MusicLevel);
            Assert.AreEqual(SettingsData.VolumeSteps, d.EffectsLevel);
            Assert.IsTrue(d.ReducedMotion, "browser 'reduce motion' preference is the first-visit default");
            Assert.IsFalse(d.AssistedMode);
            Assert.AreEqual(0, d.BestScore);
        }

        [Test]
        public void Settings_RoundTrip_ThroughTheStore()
        {
            var store = new MemorySettingsStore();
            var saved = new SettingsData
            {
                Muted = true, MusicLevel = 2, EffectsLevel = 4, ReducedMotion = true, AssistedMode = true, BestScore = 912,
            };
            SettingsSerializer.Save(store, saved);
            Assert.AreEqual(1, store.SaveCount);

            SettingsData loaded = SettingsSerializer.Load(store, reducedMotionDefault: false);
            Assert.AreEqual(saved.Muted, loaded.Muted);
            Assert.AreEqual(saved.MusicLevel, loaded.MusicLevel);
            Assert.AreEqual(saved.EffectsLevel, loaded.EffectsLevel);
            Assert.AreEqual(saved.ReducedMotion, loaded.ReducedMotion, "a saved choice beats the browser preference");
            Assert.AreEqual(saved.AssistedMode, loaded.AssistedMode);
            Assert.AreEqual(912, loaded.BestScore);
        }

        [Test]
        public void Settings_CorruptValues_AreClamped()
        {
            var store = new MemorySettingsStore();
            store.SetInt(SettingsSerializer.MusicKey, 99);
            store.SetInt(SettingsSerializer.EffectsKey, -3);
            store.SetInt(SettingsSerializer.BestScoreKey, -50);
            SettingsData d = SettingsSerializer.Load(store, false);
            Assert.AreEqual(SettingsData.VolumeSteps, d.MusicLevel);
            Assert.AreEqual(0, d.EffectsLevel);
            Assert.AreEqual(0, d.BestScore);
        }

        [Test]
        public void Settings_NoStore_StillGivesDefaults()
        {
            SettingsData d = SettingsSerializer.Load(null, false);
            Assert.AreEqual(SettingsData.VolumeSteps, d.MusicLevel);
            Assert.DoesNotThrow(() => SettingsSerializer.Save(null, d));
        }

        [Test]
        public void VolumeButton_StepsDownToZero_ThenWrapsToFull()
        {
            var seen = new List<int>();
            int level = SettingsData.VolumeSteps;
            for (int i = 0; i <= SettingsData.VolumeSteps + 1; i++)
            {
                seen.Add(SettingsData.Percent(level));
                level = SettingsData.NextLevelDown(level);
            }
            CollectionAssert.AreEqual(new[] { 100, 80, 60, 40, 20, 0, 100 }, seen);
            Assert.AreEqual(0f, SettingsData.LevelToVolume(0));
            Assert.AreEqual(1f, SettingsData.LevelToVolume(SettingsData.VolumeSteps));
        }

        // ------------------------------------------------------------------ replay (AC-11)

        [Test]
        public void Replay_LeadRunner_EndsOnThird_AndNeverReachesHome()
        {
            for (int step = 0; step < ReplayTimeline.StepCount; step++)
            for (int i = 0; i <= 20; i++)
            {
                Vector2 p = ReplayTimeline.Position(ReplayActor.LeadRunner, step, i / 20f);
                Assert.Greater(Vector2.Distance(p, ReplayTimeline.Home), 8f, $"step {step} t {i / 20f}: runner near home");
            }
            Assert.AreEqual(ReplayTimeline.ThirdBase, ReplayTimeline.Position(ReplayActor.LeadRunner, ReplayTimeline.Throw, 1f));
            Assert.AreEqual(ReplayTimeline.ThirdBase, ReplayTimeline.Position(ReplayActor.LeadRunner, ReplayTimeline.Result, 1f));
            Assert.AreEqual(ReplayTimeline.ThirdBase, ReplayTimeline.Position(ReplayActor.LeadRunner, ReplayTimeline.Legacy, 0.5f));
        }

        [Test]
        public void Replay_LeadRunner_TagsUpAtSecond_BeforeHeadingToThird()
        {
            Assert.AreEqual(ReplayTimeline.SecondBase, ReplayTimeline.Position(ReplayActor.LeadRunner, ReplayTimeline.Catch, 1f));
            Assert.AreEqual(ReplayTimeline.SecondBase, ReplayTimeline.Position(ReplayActor.LeadRunner, ReplayTimeline.Throw, 0f));
        }

        [Test]
        public void Replay_FielderAndBall_MeetAtTheCatch_ThenTheBallGoesToTheInfield()
        {
            Assert.AreEqual(ReplayTimeline.FielderStart, ReplayTimeline.Position(ReplayActor.Fielder, ReplayTimeline.Setting, 0f));
            Assert.AreEqual(ReplayTimeline.CatchSpot, ReplayTimeline.Position(ReplayActor.Fielder, ReplayTimeline.Drive, 1f));
            Vector2 ballAtCatch = ReplayTimeline.Position(ReplayActor.Ball, ReplayTimeline.Drive, 1f);
            Assert.Less(Vector2.Distance(ballAtCatch, ReplayTimeline.CatchSpot), 4f, "ball ends in the glove");
            Assert.AreEqual(ReplayTimeline.Home, ReplayTimeline.Position(ReplayActor.Ball, ReplayTimeline.Drive, 0f));
            Assert.AreEqual(ReplayTimeline.Cutoff, ReplayTimeline.Position(ReplayActor.Ball, ReplayTimeline.Throw, 1f));
            Assert.IsFalse(ReplayTimeline.IsVisible(ReplayActor.Ball, ReplayTimeline.Setting, 0.5f));
            Assert.Greater(ReplayTimeline.BallScale(ReplayTimeline.Drive, 0.5f), 2f, "fly ball looks high mid-flight");
        }

        [Test]
        public void Replay_TrailRunnerGoesBackToFirst_AndTheBatterIsOut()
        {
            Assert.AreEqual(ReplayTimeline.FirstBase, ReplayTimeline.Position(ReplayActor.TrailRunner, ReplayTimeline.Result, 1f));
            Assert.IsTrue(ReplayTimeline.IsVisible(ReplayActor.Batter, ReplayTimeline.Drive, 1f));
            Assert.IsFalse(ReplayTimeline.IsVisible(ReplayActor.Batter, ReplayTimeline.Throw, 0f), "batter is out on the catch");
        }

        [Test]
        public void Replay_EveryPositionIsOnTheDiagram()
        {
            foreach (ReplayActor actor in Enum.GetValues(typeof(ReplayActor)))
            for (int step = 0; step < ReplayTimeline.StepCount; step++)
            for (int i = 0; i <= 10; i++)
            {
                Vector2 p = ReplayTimeline.Position(actor, step, i / 10f);
                Assert.That(p.x, Is.InRange(4f, ReplayTimeline.DiagramWidth - 4f), $"{actor} step {step}");
                Assert.That(p.y, Is.InRange(4f, ReplayTimeline.DiagramHeight - 4f), $"{actor} step {step}");
            }
        }

        [Test]
        public void ReplayContent_HasOneBeatPerStep_WithWordsThatFitTheScreen()
        {
            var content = AssetDatabase.LoadAssetAtPath<MissionContent>(ContentPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(RegularFont);
            Assert.IsNotNull(content);
            Assert.IsNotNull(font);
            Assert.AreEqual(ReplayTimeline.StepCount, content.replayBeats.Length);
            foreach (MissionContent.ReplayBeat beat in content.replayBeats)
            {
                Assert.IsNotEmpty(beat.title);
                Assert.IsNotEmpty(beat.narration);
                // Narration box: 156 x 96 art px, 10 px lines (MainSceneBuilder.BuildReplay).
                int lines = WrappedLineCount(font, beat.narration, 156);
                Assert.LessOrEqual(lines, 9, $"'{beat.title}' narration needs {lines} lines");
            }
            for (int i = 0; i < content.replayBeats.Length - 1; i++)
                Assert.Greater(content.replayBeats[i].seconds, 0f, $"step {i + 1} needs a duration");
        }

        // ------------------------------------------------------------------ scoring (AC-06)

        [TestCase(500, Rank.AllStar, 750)]
        [TestCase(749, Rank.AllStar, 750)]
        [TestCase(750, Rank.Legendary, 900)]
        [TestCase(899, Rank.Legendary, 900)]
        public void NextRank_PointsTheWayUp(int total, Rank expectedRank, int expectedAt)
        {
            Assert.IsTrue(ScoreCalculator.TryGetNextRank(ScoringRules.Default, total, out Rank next, out int at));
            Assert.AreEqual(expectedRank, next);
            Assert.AreEqual(expectedAt, at);
        }

        [TestCase(900)]
        [TestCase(1000)]
        public void NextRank_NoneAtTheTop(int total)
        {
            Assert.IsFalse(ScoreCalculator.TryGetNextRank(ScoringRules.Default, total, out _, out _));
        }

        // ------------------------------------------------------------------ accessibility (AC-12)


        /// <summary>
        /// Every text / background pair the menus use. Body text needs 4.5:1 (WCAG AA); the big
        /// TRY AGAIN title is large text and needs 3:1.
        /// </summary>
        [TestCase("off-white on panel", 0xFBF0D5, 0x33204F, 4.5)]
        [TestCase("yellow on panel", 0xFFD83D, 0x33204F, 4.5)]
        [TestCase("orange on panel", 0xF57F32, 0x33204F, 4.5)]
        [TestCase("off-white on secondary button", 0xFBF0D5, 0x1B1030, 4.5)]
        [TestCase("midnight on primary button", 0x1B1030, 0xF57F32, 4.5)]
        [TestCase("midnight on focused button", 0x1B1030, 0xFFD83D, 4.5)]
        [TestCase("off-white on dim overlay", 0xFBF0D5, 0x1B1030, 4.5)]
        [TestCase("red title on panel (large)", 0xE04858, 0x33204F, 3.0)]
        public void TextColours_MeetWcagContrast(string pair, int fg, int bg, double minimum)
        {
            double ratio = Contrast(Rgb(fg), Rgb(bg));
            Assert.GreaterOrEqual(ratio, minimum, $"{pair}: {ratio:0.00}:1");
        }

        // ------------------------------------------------------------------ sound mix

        [Test]
        public void Mix_KeepsTheCrowdBedQuietest_AndTheBatCrackLoudest()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioLibraryPath);
            Assert.IsNotNull(library);
            float Vol(AudioCue c) => library.entries.First(e => e.cue == c).volume;
            foreach (AudioCueLibrary.Entry e in library.entries)
                Assert.That(e.volume, Is.InRange(0.05f, 1f), e.cue.ToString());
            Assert.AreEqual(library.entries.Min(e => e.volume), Vol(AudioCue.CrowdLoop), "crowd bed sits under everything");
            Assert.AreEqual(1f, Vol(AudioCue.BatCrack), "the crack of the bat leads the mix");
            Assert.Less(Vol(AudioCue.UiMove), Vol(AudioCue.UiConfirm), "frequent menu ticks are quieter than confirms");
        }

        // ------------------------------------------------------------------ helpers

        private static Color32 Rgb(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 0xFF);

        private static double Contrast(Color32 a, Color32 b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color32 c)
        {
            double Lin(byte v)
            {
                double s = v / 255.0;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Lin(c.r) + 0.7152 * Lin(c.g) + 0.0722 * Lin(c.b);
        }

        /// <summary>Greedy word wrap with the pixel font's advances (same as uGUI's wrap for this font).</summary>
        private static int WrappedLineCount(Font font, string text, int widthPx)
        {
            var advance = font.characterInfo.ToDictionary(ci => ci.index, ci => ci.advance);
            int Width(string s) => s.Sum(ch => advance.TryGetValue(ch, out int a) ? a : 6);
            int lines = 0;
            foreach (string paragraph in text.Split('\n'))
            {
                lines++;
                int x = 0;
                foreach (string word in paragraph.Split(' '))
                {
                    int w = Width(word);
                    int space = x > 0 ? Width(" ") : 0;
                    if (x > 0 && x + space + w > widthPx)
                    {
                        lines++;
                        x = w;
                    }
                    else
                    {
                        x += space + w;
                    }
                }
            }
            return lines;
        }
    }
}
