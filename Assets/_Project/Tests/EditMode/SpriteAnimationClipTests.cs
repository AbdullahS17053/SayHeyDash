using NUnit.Framework;
using SayHeyDash.Visuals;

namespace SayHeyDash.Tests
{
    /// <summary>Frame timing of the flip-book animations (M3).</summary>
    public sealed class SpriteAnimationClipTests
    {
        [TestCase(0f, 0)]
        [TestCase(0.0999f, 0)]
        [TestCase(0.1f, 1)]
        [TestCase(0.55f, 5)]
        [TestCase(0.6f, 0)]   // wraps after 6 frames at 10 fps
        [TestCase(1.25f, 0)]
        [TestCase(1.35f, 1)]
        public void Looping_WrapsAround(float time, int expected) =>
            Assert.AreEqual(expected, SpriteAnimationClip.FrameAt(6, 10f, time, true));

        [TestCase(0.25f, 2)]
        [TestCase(0.39f, 3)]
        [TestCase(0.4f, 3)]   // one-shot holds its last frame
        [TestCase(10f, 3)]
        public void OneShot_HoldsLastFrame(float time, int expected) =>
            Assert.AreEqual(expected, SpriteAnimationClip.FrameAt(4, 10f, time, false));

        [Test]
        public void EdgeCases()
        {
            Assert.AreEqual(-1, SpriteAnimationClip.FrameAt(0, 10f, 1f, true), "no frames");
            Assert.AreEqual(0, SpriteAnimationClip.FrameAt(1, 10f, 5f, true), "single frame");
            Assert.AreEqual(0, SpriteAnimationClip.FrameAt(5, 10f, -1f, false), "negative time");
        }

        [Test]
        public void OneShot_FinishesAfterItsDuration()
        {
            Assert.IsFalse(SpriteAnimationClip.IsFinishedAt(4, 10f, 0.39f, false));
            Assert.IsTrue(SpriteAnimationClip.IsFinishedAt(4, 10f, 0.4f, false));
            Assert.IsFalse(SpriteAnimationClip.IsFinishedAt(4, 10f, 99f, true), "loops never finish");
        }
    }
}
