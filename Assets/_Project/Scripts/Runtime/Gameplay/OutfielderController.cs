using SayHeyDash.Controls;
using SayHeyDash.Data;
using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// Stage A movement. The GameObject's position is the outfielder's FEET on the ground
    /// plane; the sprite is a child offset upward. Control is switched on/off by the flow
    /// controller, so this component never needs to know the game state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutfielderController : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private GameplayTuning tuning;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Glove position relative to the feet when facing right (matches the catch frames).")]
        [SerializeField] private Vector2 gloveOffset = new Vector2(0.45f, 1.1f);
        [Tooltip("Throwing hand at release, relative to the feet when facing right (matches the throw frames).")]
        [SerializeField] private Vector2 throwReleaseOffset = new Vector2(0.45f, 0.85f);

        private Vector2 _velocity;
        private bool _controlEnabled;
        private bool _facingRight = true;

        /// <summary>Feet position on the ground plane.</summary>
        public Vector2 Position => transform.position;

        public Vector2 Velocity => _velocity;

        public bool ControlEnabled => _controlEnabled;

        public Vector2 GlovePosition =>
            Position + new Vector2(_facingRight ? gloveOffset.x : -gloveOffset.x, gloveOffset.y);

        public Vector2 ThrowReleasePosition =>
            Position + new Vector2(_facingRight ? throwReleaseOffset.x : -throwReleaseOffset.x, throwReleaseOffset.y);

        public bool FacingRight => _facingRight;

        private void Awake()
        {
            if (input == null || tuning == null || body == null)
            {
                Debug.LogError($"[{nameof(OutfielderController)}] Missing reference " +
                               $"(input: {input != null}, tuning: {tuning != null}, body: {body != null}).", this);
                enabled = false;
            }
        }

        public void ResetToStart()
        {
            _velocity = Vector2.zero;
            _controlEnabled = false;
            transform.position = tuning.playerStartPosition;
            Face(true);
        }

        public void SetControlEnabled(bool value)
        {
            _controlEnabled = value;
            if (!value) _velocity = Vector2.zero;
        }

        /// <summary>Turn toward the infield for the throw (the spin itself is drawn by OutfielderAnimator).</summary>
        public void FaceTowards(Vector2 worldPoint) => Face(worldPoint.x >= Position.x);

        /// <summary>Test hook: replaces the input direction (null = use real input). Batch-mode
        /// test runs have no window focus, so simulated device input is dropped.</summary>
        internal Vector2? MoveOverride { get; set; }

        /// <summary>Test / tooling hook: place the feet directly.</summary>
        internal void TeleportTo(Vector2 position)
        {
            _velocity = Vector2.zero;
            transform.position = ClampToBounds(position);
        }

        private void Update()
        {
            if (!_controlEnabled) return;

            Vector2 move = MoveOverride ?? input.Move;
            if (move.sqrMagnitude > 1f) move.Normalize();
            Vector2 targetVelocity = move * tuning.playerRunSpeed;
            _velocity = Vector2.MoveTowards(_velocity, targetVelocity, tuning.playerAcceleration * Time.deltaTime);
            if (_velocity.sqrMagnitude < 0.000001f) return;

            Vector2 next = ClampToBounds(Position + _velocity * Time.deltaTime);
            // Kill velocity into a wall so the player doesn't "stick" to the bounds.
            if (!Mathf.Approximately(next.x, Position.x + _velocity.x * Time.deltaTime)) _velocity.x = 0f;
            if (!Mathf.Approximately(next.y, Position.y + _velocity.y * Time.deltaTime)) _velocity.y = 0f;
            transform.position = next;

            if (Mathf.Abs(move.x) > 0.01f) Face(move.x > 0f);
        }

        private Vector2 ClampToBounds(Vector2 p)
        {
            Rect b = tuning.playerBounds;
            return new Vector2(Mathf.Clamp(p.x, b.xMin, b.xMax), Mathf.Clamp(p.y, b.yMin, b.yMax));
        }

        private void Face(bool right)
        {
            _facingRight = right;
            body.flipX = !right;
        }
    }
}
