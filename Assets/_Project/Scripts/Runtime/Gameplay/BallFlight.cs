using SayHeyDash.Data;
using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// Moves the ball and its shadow along the scripted <see cref="BallTrajectory"/>, and
    /// handles the two non-flight beats: the ball held in the glove and the thrown ball.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallFlight : MonoBehaviour
    {
        private enum Mode { Hidden, Flying, Held, Thrown }

        [SerializeField] private GameplayTuning tuning;
        [SerializeField] private SpriteRenderer ball;
        [SerializeField] private SpriteRenderer shadow;
        [Tooltip("Ground Y the ball cannot pass (base of the outfield wall).")]
        [SerializeField] private float wallBaseY = 0.05f;

        private Mode _mode = Mode.Hidden;
        private BallTrajectory _trajectory;
        private float _elapsed;
        private Transform _holder;
        private Vector2 _holderOffset;
        private Vector2 _throwFrom;
        private Vector2 _throwTo;
        private float _throwDuration;

        /// <summary>Seconds since the hit (only advances while flying).</summary>
        public float Elapsed => _elapsed;

        public bool IsFlying => _mode == Mode.Flying;

        public BallTrajectory Trajectory => _trajectory;

        /// <summary>Seconds after the hit when the ball reaches glove height (the perfect catch moment).</summary>
        public float PerfectCatchTime => _trajectory.Duration;

        private void Awake()
        {
            if (tuning == null || ball == null || shadow == null)
            {
                Debug.LogError($"[{nameof(BallFlight)}] Missing tuning / ball / shadow reference.", this);
                enabled = false;
            }
        }

        public void ResetBall()
        {
            _mode = Mode.Hidden;
            _elapsed = 0f;
            _holder = null;
            ball.enabled = false;
            shadow.enabled = false;
        }

        /// <summary>Starts the fly ball from the launch point (the "crack of the bat").</summary>
        public void Launch()
        {
            _trajectory = new BallTrajectory(tuning.ballLaunchPoint, tuning.landingPoint, tuning.ballFlightDuration,
                tuning.ballApexHeight, tuning.catchHeight, wallBaseY);
            _elapsed = 0f;
            _mode = Mode.Flying;
            ball.enabled = true;
            shadow.enabled = true;
            ApplyFlightPose();
        }

        /// <summary>The ball sticks to a transform (the glove) until thrown.</summary>
        public void HoldAt(Transform holder, Vector2 worldPosition)
        {
            _mode = Mode.Held;
            _holder = holder;
            _holderOffset = worldPosition - (Vector2)holder.position;
            ball.enabled = true;
            shadow.enabled = false;
            ball.transform.position = worldPosition;
        }

        public void Throw(Vector2 to, float duration)
        {
            _throwFrom = ball.transform.position;
            _throwTo = to;
            _throwDuration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _holder = null;
            _mode = Mode.Thrown;
            ball.enabled = true;
            shadow.enabled = false;
        }

        private void Update()
        {
            switch (_mode)
            {
                case Mode.Flying:
                    _elapsed += Time.deltaTime;
                    ApplyFlightPose();
                    break;
                case Mode.Held:
                    if (_holder != null) ball.transform.position = (Vector2)_holder.position + _holderOffset;
                    break;
                case Mode.Thrown:
                    _elapsed += Time.deltaTime;
                    float s = Mathf.Clamp01(_elapsed / _throwDuration);
                    // Low, flat arc toward the infield.
                    Vector2 p = Vector2.Lerp(_throwFrom, _throwTo, s) + new Vector2(0f, 1.2f * 4f * s * (1f - s));
                    ball.transform.position = p;
                    if (s >= 1f) { _mode = Mode.Hidden; ball.enabled = false; }
                    break;
            }
        }

        private void ApplyFlightPose()
        {
            Vector2 ground = _trajectory.GroundAt(_elapsed);
            shadow.transform.position = ground;
            ball.transform.position = ground + new Vector2(0f, _trajectory.HeightAt(_elapsed));
            // Ball draws in front of the wall while high, and its shadow shrinks a little with height.
            float h = _trajectory.HeightAt(_elapsed);
            float scale = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(h / Mathf.Max(0.01f, tuning.ballApexHeight)));
            shadow.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
