using NUnit.Framework;
using SayHeyDash.Gameplay;
using UnityEngine;

namespace SayHeyDash.Tests
{
    /// <summary>AC-04 / AC-05: catch and throw rules distinguish success from failure reliably.</summary>
    public sealed class CatchAndThrowJudgeTests
    {
        private const float Perfect = 3.6f;
        private const float Window = 0.4f;
        private const float Radius = 1f;

        [Test]
        public void PerfectPositionAndTiming_IsCaughtWithFullQuality()
        {
            CatchResult r = CatchJudge.Judge(Perfect, Perfect, Window, 0f, Radius);
            Assert.IsTrue(r.Success);
            Assert.AreEqual(1f, r.PositioningQuality, 1e-5f);
            Assert.AreEqual(1f, r.TimingQuality, 1e-5f);
        }

        [Test]
        public void WindowEdges_AreInclusive()
        {
            const float justInside = Window / 2f - 1e-4f;
            Assert.IsTrue(CatchJudge.Judge(Perfect - justInside, Perfect, Window, 0.2f, Radius).Success);
            Assert.IsTrue(CatchJudge.Judge(Perfect + justInside, Perfect, Window, 0.2f, Radius).Success);
            Assert.IsTrue(CatchJudge.Judge(Perfect, Perfect, Window, Radius, Radius).Success);
        }

        [Test]
        public void EarlyPress_IsTooEarly()
        {
            Assert.AreEqual(CatchOutcome.TooEarly, CatchJudge.Judge(Perfect - 0.21f, Perfect, Window, 0f, Radius).Outcome);
        }

        [Test]
        public void LatePress_IsTooLate()
        {
            Assert.AreEqual(CatchOutcome.TooLate, CatchJudge.Judge(Perfect + 0.21f, Perfect, Window, 0f, Radius).Outcome);
        }

        [Test]
        public void OutsideZone_IsOutOfZone_EvenWithPerfectTiming()
        {
            Assert.AreEqual(CatchOutcome.OutOfZone, CatchJudge.Judge(Perfect, Perfect, Window, 1.01f, Radius).Outcome);
        }

        [Test]
        public void HalfwayValues_GiveHalfQuality()
        {
            CatchResult r = CatchJudge.Judge(Perfect + 0.1f, Perfect, Window, 0.5f, Radius);
            Assert.AreEqual(0.5f, r.PositioningQuality, 1e-4f);
            Assert.AreEqual(0.5f, r.TimingQuality, 1e-4f);
        }

        [Test]
        public void NoPress_InZone_IsTooLate_OutOfZone_IsOutOfZone()
        {
            Assert.AreEqual(CatchOutcome.TooLate, CatchJudge.NoPress(0.5f, Radius, 0.2f).Outcome);
            Assert.AreEqual(CatchOutcome.OutOfZone, CatchJudge.NoPress(3f, Radius, 0.2f).Outcome);
        }

        [Test]
        public void InputOpenTime_IsNeverInsideTheWindow()
        {
            Assert.AreEqual(Perfect - 0.65f, CatchJudge.InputOpenTime(Perfect, Window, 0.65f), 1e-5f);
            // A huge (assisted) window pushes the open time earlier than the window start.
            float open = CatchJudge.InputOpenTime(Perfect, 2f, 0.65f);
            Assert.Less(open, Perfect - 1f);
        }

        [Test]
        public void Needle_StartsAtZero_AndPingPongs()
        {
            Assert.AreEqual(0f, ThrowJudge.NeedlePosition(0f, 1.1f), 1e-5f);
            Assert.AreEqual(0.5f, ThrowJudge.NeedlePosition(0.5f, 1f), 1e-5f);
            Assert.AreEqual(1f, ThrowJudge.NeedlePosition(1f, 1f), 1e-5f);
            Assert.AreEqual(0.75f, ThrowJudge.NeedlePosition(1.25f, 1f), 1e-5f);
            for (float t = 0f; t < 20f; t += 0.037f)
                Assert.That(ThrowJudge.NeedlePosition(t, 1.7f), Is.InRange(0f, 1f));
        }

        [Test]
        public void Throw_InsideZone_Succeeds_OutsideFails()
        {
            Assert.IsTrue(ThrowJudge.Judge(0.65f, 0.65f, 0.16f).Success);
            Assert.AreEqual(1f, ThrowJudge.Judge(0.65f, 0.65f, 0.16f).Quality, 1e-5f);
            Assert.IsTrue(ThrowJudge.Judge(0.7299f, 0.65f, 0.16f).Success, "just inside the edge");
            Assert.IsFalse(ThrowJudge.Judge(0.74f, 0.65f, 0.16f).Success);
            Assert.IsFalse(ThrowJudge.Judge(0.2f, 0.65f, 0.16f).Success);
            Assert.AreEqual(0.5f, ThrowJudge.Judge(0.69f, 0.65f, 0.16f).Quality, 1e-4f);
        }

        [Test]
        public void ThrowTimeout_IsAFailure()
        {
            ThrowResult r = ThrowJudge.TimedOut();
            Assert.IsFalse(r.Success);
            Assert.IsTrue(r.TimedOut);
        }

        [Test]
        public void Trajectory_IsDeterministic_AndEndsAtLandingPointAtGloveHeight()
        {
            var a = new BallTrajectory(new Vector2(0.5f, -8f), new Vector2(3.5f, -0.25f), 3.6f, 7f, 0.65f, 0.05f);
            var b = new BallTrajectory(new Vector2(0.5f, -8f), new Vector2(3.5f, -0.25f), 3.6f, 7f, 0.65f, 0.05f);
            for (float t = 0f; t <= 5f; t += 0.1f)
                Assert.AreEqual(a.VisualAt(t), b.VisualAt(t));

            Vector2 landing = a.GroundAt(3.6f);
            Assert.AreEqual(3.5f, landing.x, 1e-4f);
            Assert.AreEqual(-0.25f, landing.y, 1e-4f);
            Assert.AreEqual(0.65f, a.HeightAt(3.6f), 1e-4f);
            Assert.AreEqual(0f, a.HeightAt(0f), 1e-4f);
            Assert.Greater(a.HeightAt(1.8f), 6.9f, "apex near the middle of the flight");
        }

        [Test]
        public void Trajectory_AfterTheCatchMoment_FallsToGround_AndStopsAtTheWall()
        {
            var t = new BallTrajectory(new Vector2(0.5f, -8f), new Vector2(3.5f, -0.25f), 3.6f, 7f, 0.65f, 0.05f);
            float contact = t.GroundContactTime;
            Assert.Greater(contact, 3.6f);
            Assert.AreEqual(0f, t.HeightAt(contact), 1e-4f);
            Assert.AreEqual(0f, t.HeightAt(contact + 1f));
            Assert.LessOrEqual(t.GroundAt(10f).y, 0.05f + 1e-5f);
            Assert.AreEqual(t.GroundAt(10f), t.GroundAt(20f), "a dead ball must not slide along the wall");
        }
    }
}
