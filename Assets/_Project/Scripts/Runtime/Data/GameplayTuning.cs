using UnityEngine;

namespace SayHeyDash.Data
{
    /// <summary>
    /// Every gameplay number a designer may want to tweak (GDD: "Player speed, catch timing,
    /// throw timing ... should be adjustable without rewriting the main game systems").
    /// World units: 16 pixels = 1 unit; the camera shows 20 x 11.25 units centred on (0, 0).
    /// The ground plane is drawn top-down-ish: +Y is deeper toward the outfield wall.
    /// </summary>
    [CreateAssetMenu(menuName = "Say Hey Dash/Gameplay Tuning", fileName = "GameplayTuning")]
    public sealed class GameplayTuning : ScriptableObject
    {
        [Header("Outfielder")]
        [Tooltip("Top running speed in world units per second.")]
        [Min(0.1f)] public float playerRunSpeed = 3.4f;

        [Tooltip("How quickly the outfielder reaches top speed / stops (units per second squared).")]
        [Min(0.1f)] public float playerAcceleration = 24f;

        [Tooltip("Feet position at the start of every attempt.")]
        public Vector2 playerStartPosition = new Vector2(-2f, -4f);

        [Tooltip("Area the outfielder's feet may move in (x, y = bottom-left; width, height).")]
        public Rect playerBounds = new Rect(-9f, -5f, 18f, 5f);

        [Header("Ball flight (scripted, deterministic - no randomness)")]
        [Tooltip("Ground point the ball leaves from (below the bottom of the screen = home plate direction).")]
        public Vector2 ballLaunchPoint = new Vector2(0.5f, -8f);

        [Tooltip("Ground point where the ball comes down - the centre of the landing indicator.")]
        public Vector2 landingPoint = new Vector2(3.5f, -0.25f);

        [Tooltip("Seconds from the hit until the ball reaches glove height at the landing point.")]
        [Min(0.5f)] public float ballFlightDuration = 3.6f;

        [Tooltip("Highest point of the arc above the ground, in world units.")]
        [Min(0f)] public float ballApexHeight = 7f;

        [Tooltip("Height above the ground at which the ball is caught (glove height of the over-the-shoulder pose).")]
        [Min(0f)] public float catchHeight = 1.1f;

        [Header("Catch (Stage B)")]
        [Tooltip("Radius of the landing zone. The outfielder's feet must be inside it to catch. Drawn as the ring.")]
        [Min(0.1f)] public float landingZoneRadius = 1f;

        [Tooltip("Total width in seconds of the catch-timing window, centred on the moment the ball reaches glove height.")]
        [Min(0.02f)] public float catchWindowSeconds = 0.4f;

        [Tooltip("How many seconds before the perfect moment presses start to count. Earlier presses are ignored.")]
        [Min(0.05f)] public float catchInputOpenSeconds = 0.65f;

        [Header("Catch sequence (Screen 04)")]
        [Min(0f)] public float catchSequenceSeconds = 1.2f;
        [Min(0f)] public float reducedMotionCatchSequenceSeconds = 0.4f;

        [Header("Throw (Stage C)")]
        [Tooltip("Needle speed in meter-widths per second (the needle sweeps back and forth).")]
        [Min(0.05f)] public float throwMeterSpeed = 1.1f;

        [Tooltip("Centre of the target zone along the meter (0 = left end, 1 = right end).")]
        [Range(0f, 1f)] public float throwTargetZoneCenter = 0.65f;

        [Tooltip("Width of the target zone as a fraction of the meter.")]
        [Range(0.01f, 1f)] public float throwTargetZoneWidth = 0.16f;

        [Tooltip("Seconds the player has to throw before it counts as THROW MISSED. 0 = no limit.")]
        [Min(0f)] public float throwTimeLimitSeconds = 5f;

        [Tooltip("Where a good throw goes (the infield cut-off, below the screen).")]
        public Vector2 throwTargetPoint = new Vector2(0f, -7.5f);

        [Tooltip("Seconds the thrown ball takes to leave the screen.")]
        [Min(0.05f)] public float throwFlightSeconds = 0.6f;

        [Header("Result pacing")]
        [Tooltip("Seconds the miss is shown on the field before the TRY AGAIN screen.")]
        [Min(0f)] public float missRevealSeconds = 1f;
    }
}
