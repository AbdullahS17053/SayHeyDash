using UnityEngine;

namespace SayHeyDash.Replay
{
    /// <summary>Who moves in the Screen 07 replay.</summary>
    public enum ReplayActor
    {
        Ball,
        /// <summary>The center fielder (Mays).</summary>
        Fielder,
        /// <summary>The runner on second (Doby): tags up, takes third, does not score.</summary>
        LeadRunner,
        /// <summary>The runner on first: goes back to first.</summary>
        TrailRunner,
        /// <summary>The batter (Wertz): out on the catch.</summary>
        Batter,
    }

    /// <summary>
    /// Pure choreography of the simplified animated replay (Screen 07), unit-tested so the replay
    /// can never show an alternative history: the lead runner ends on third base and never
    /// reaches home. Positions are in pixels of the top-down field diagram
    /// (<see cref="DiagramWidth"/> x <see cref="DiagramHeight"/>, origin top-left, +y down),
    /// matching <c>replay_field.png</c> from Tools/ArtGen.
    /// Each step moves every actor from where the previous step left it along a short path.
    /// </summary>
    public static class ReplayTimeline
    {
        public const int DiagramWidth = 128;
        public const int DiagramHeight = 80;

        public const int StepCount = 6;
        public const int Setting = 0, Drive = 1, Catch = 2, Throw = 3, Result = 4, Legacy = 5;

        // Field geometry (about 0.14 px per foot; Polo Grounds-style deep center field).
        public static readonly Vector2 Home = new Vector2(64, 74);
        public static readonly Vector2 FirstBase = new Vector2(73, 65);
        public static readonly Vector2 SecondBase = new Vector2(64, 56);
        public static readonly Vector2 ThirdBase = new Vector2(55, 65);
        public static readonly Vector2 FielderStart = new Vector2(63, 32);
        public static readonly Vector2 CatchSpot = new Vector2(67, 14);
        public static readonly Vector2 Glove = CatchSpot + new Vector2(2, -2);
        /// <summary>The infielder the throw goes to, on the outfield side of second base.</summary>
        public static readonly Vector2 Cutoff = new Vector2(66, 51);

        private static readonly Vector2 BatterBox = new Vector2(60, 74);
        private static readonly Vector2 LeadOff = new Vector2(62, 58);
        private static readonly Vector2 FirstLeadOff = new Vector2(71, 63);

        // Paths[actor][step] = waypoints after the step's start point (empty = stays put).
        private static readonly Vector2[][][] Paths =
        {
            // Ball
            new[]
            {
                new[] { Home }, new[] { Glove }, new Vector2[0], new[] { Cutoff }, new Vector2[0], new Vector2[0],
            },
            // Fielder
            new[]
            {
                new[] { FielderStart }, new[] { CatchSpot }, new Vector2[0], new Vector2[0], new Vector2[0], new Vector2[0],
            },
            // Lead runner: off with the hit, back to tag up at second, then to third - and no further.
            new[]
            {
                new[] { LeadOff }, new[] { new Vector2(59, 61) }, new[] { SecondBase }, new[] { ThirdBase },
                new Vector2[0], new Vector2[0],
            },
            // Trail runner: off with the hit, then back to first.
            new[]
            {
                new[] { FirstLeadOff }, new[] { new Vector2(67, 59) }, new[] { FirstBase }, new Vector2[0],
                new Vector2[0], new Vector2[0],
            },
            // Batter: runs through first, out on the catch.
            new[]
            {
                new[] { BatterBox }, new[] { FirstBase, new Vector2(76, 63) }, new Vector2[0], new Vector2[0],
                new Vector2[0], new Vector2[0],
            },
        };

        /// <summary>Diagram position of <paramref name="actor"/> at progress <paramref name="t"/> (0..1) of <paramref name="step"/>.</summary>
        public static Vector2 Position(ReplayActor actor, int step, float t)
        {
            step = Mathf.Clamp(step, 0, StepCount - 1);
            t = Mathf.Clamp01(t);
            Vector2[][] paths = Paths[(int)actor];
            Vector2 start = StartOf(paths, step);
            Vector2[] way = paths[step];
            if (step == 0 || way.Length == 0) return step == 0 ? way[0] : start;
            return AlongPath(start, way, Ease(actor, step, t));
        }

        /// <summary>False while the actor is off the diagram (ball before the hit, batter after the out).</summary>
        public static bool IsVisible(ReplayActor actor, int step, float t)
        {
            switch (actor)
            {
                case ReplayActor.Ball: return step >= Drive;
                case ReplayActor.Batter: return step < Catch || (step == Catch && t < 0.35f);
                default: return true;
            }
        }

        /// <summary>Ball size multiplier that suggests height in a top-down view (1 = on the ground).</summary>
        public static float BallScale(int step, float t)
        {
            t = Mathf.Clamp01(t);
            if (step == Drive) return 1f + 1.5f * Mathf.Sin(Mathf.PI * t);
            if (step == Throw) return 1f + 0.4f * Mathf.Sin(Mathf.PI * t);
            return 1f;
        }

        private static Vector2 StartOf(Vector2[][] paths, int step)
        {
            Vector2 p = paths[0][0];
            for (int s = 1; s < step; s++)
                if (paths[s].Length > 0) p = paths[s][paths[s].Length - 1];
            return p;
        }

        private static float Ease(ReplayActor actor, int step, float t)
        {
            if (actor == ReplayActor.Ball) return t; // a struck / thrown ball travels evenly
            return t * t * (3f - 2f * t);           // people speed up and slow down
        }

        private static Vector2 AlongPath(Vector2 start, Vector2[] way, float t)
        {
            float total = 0f;
            Vector2 prev = start;
            foreach (Vector2 w in way) { total += Vector2.Distance(prev, w); prev = w; }
            if (total <= 0f) return way[way.Length - 1];

            float target = t * total;
            prev = start;
            foreach (Vector2 w in way)
            {
                float seg = Vector2.Distance(prev, w);
                if (target <= seg && seg > 0f) return Vector2.Lerp(prev, w, target / seg);
                target -= seg;
                prev = w;
            }
            return way[way.Length - 1];
        }
    }
}
