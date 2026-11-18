using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// Pure rules for Stage B (unit-tested). Position is checked first, so pressing outside
    /// the ring always gets the "follow the shadow" hint even if the timing was also off.
    /// </summary>
    public static class CatchJudge
    {
        /// <param name="pressTime">Seconds since the hit when Action was pressed.</param>
        /// <param name="perfectTime">Seconds since the hit when the ball reaches glove height.</param>
        /// <param name="windowSeconds">Full width of the timing window (already difficulty-scaled).</param>
        /// <param name="distance">Feet-to-landing-point distance at the press.</param>
        /// <param name="zoneRadius">Landing zone radius (already difficulty-scaled).</param>
        public static CatchResult Judge(float pressTime, float perfectTime, float windowSeconds,
            float distance, float zoneRadius)
        {
            float error = pressTime - perfectTime;
            float halfWindow = Mathf.Max(0.0001f, windowSeconds * 0.5f);

            if (distance > zoneRadius)
                return new CatchResult(CatchOutcome.OutOfZone, 0f, 0f, error, distance);
            if (error < -halfWindow)
                return new CatchResult(CatchOutcome.TooEarly, 0f, 0f, error, distance);
            if (error > halfWindow)
                return new CatchResult(CatchOutcome.TooLate, 0f, 0f, error, distance);

            float positioning = 1f - Mathf.Clamp01(distance / Mathf.Max(0.0001f, zoneRadius));
            float timing = 1f - Mathf.Clamp01(Mathf.Abs(error) / halfWindow);
            return new CatchResult(CatchOutcome.Caught, positioning, timing, error, distance);
        }

        /// <summary>The window closed without a press.</summary>
        public static CatchResult NoPress(float distanceAtDeadline, float zoneRadius, float halfWindow)
        {
            CatchOutcome outcome = distanceAtDeadline > zoneRadius ? CatchOutcome.OutOfZone : CatchOutcome.TooLate;
            return new CatchResult(outcome, 0f, 0f, halfWindow, distanceAtDeadline);
        }

        /// <summary>Seconds after the hit when presses start to count (never inside the window).</summary>
        public static float InputOpenTime(float perfectTime, float windowSeconds, float inputOpenSeconds)
        {
            float lead = Mathf.Max(inputOpenSeconds, windowSeconds * 0.5f + 0.1f);
            return perfectTime - lead;
        }
    }
}
