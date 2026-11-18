using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// Scripted, deterministic fly-ball path (GDD fairness: no random events).
    /// The ball's ground position (where its shadow is) moves in a straight line from the
    /// launch point to the landing point; its height follows a parabola that ends at glove
    /// height exactly at <see cref="Duration"/>. After that the ball keeps dropping along the
    /// same line until it hits the ground (a missed ball).
    /// </summary>
    public readonly struct BallTrajectory
    {
        public readonly Vector2 Launch;
        public readonly Vector2 Landing;
        public readonly float Duration;
        public readonly float ApexHeight;
        public readonly float CatchHeight;
        public readonly float MaxGroundY;

        public BallTrajectory(Vector2 launch, Vector2 landing, float duration, float apexHeight,
            float catchHeight, float maxGroundY = float.PositiveInfinity)
        {
            Launch = launch;
            Landing = landing;
            Duration = Mathf.Max(0.01f, duration);
            ApexHeight = Mathf.Max(0f, apexHeight);
            CatchHeight = Mathf.Max(0f, catchHeight);
            MaxGroundY = maxGroundY;
        }

        /// <summary>Ground (shadow) position at time <paramref name="t"/> seconds after the hit.</summary>
        public Vector2 GroundAt(float t)
        {
            float s = Mathf.Max(0f, t) / Duration;
            // A missed ball carries on along the same line until the wall stops it dead.
            if (s > 1f && Landing.y > Launch.y && Landing.y < MaxGroundY)
                s = Mathf.Min(s, (MaxGroundY - Launch.y) / (Landing.y - Launch.y));
            return Vector2.LerpUnclamped(Launch, Landing, s);
        }

        /// <summary>Height above the ground at time <paramref name="t"/>; never negative.</summary>
        public float HeightAt(float t)
        {
            float s = Mathf.Max(0f, t) / Duration;
            float h = 4f * ApexHeight * s * (1f - s) + CatchHeight * s;
            return h > 0f ? h : 0f;
        }

        /// <summary>Where the ball sprite is drawn: ground position lifted by its height.</summary>
        public Vector2 VisualAt(float t) => GroundAt(t) + new Vector2(0f, HeightAt(t));

        /// <summary>Seconds after the hit at which a missed ball touches the ground.</summary>
        public float GroundContactTime
        {
            get
            {
                // Solve 4A s(1-s) + C s = 0 for s > 0  =>  s = 1 + C / (4A)
                if (ApexHeight <= 0f) return Duration;
                return Duration * (1f + CatchHeight / (4f * ApexHeight));
            }
        }
    }
}
