using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>Pure rules for Stage C (unit-tested).</summary>
    public static class ThrowJudge
    {
        /// <summary>
        /// Needle position (0..1) after <paramref name="elapsed"/> seconds, starting at 0 and
        /// sweeping back and forth at <paramref name="speed"/> meter-widths per second.
        /// </summary>
        public static float NeedlePosition(float elapsed, float speed) =>
            Mathf.PingPong(Mathf.Max(0f, elapsed) * Mathf.Max(0f, speed), 1f);

        /// <summary>Judges a press. Zone edges count as inside.</summary>
        public static ThrowResult Judge(float needle, float zoneCenter, float zoneWidth)
        {
            float half = Mathf.Max(0.0001f, zoneWidth * 0.5f);
            float offset = Mathf.Abs(needle - zoneCenter);
            if (offset > half) return new ThrowResult(false, 0f, needle);
            return new ThrowResult(true, 1f - Mathf.Clamp01(offset / half), needle);
        }

        public static ThrowResult TimedOut() => new ThrowResult(false, 0f, -1f);
    }
}
