using SayHeyDash.Data;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;

namespace SayHeyDash.Visuals
{
    /// <summary>
    /// Presentation only: picks the outfielder's animation from the game state and the
    /// mechanics' events. It never changes gameplay - movement, facing during play and
    /// judging stay in <see cref="OutfielderController"/> and the Catch/Throw systems.
    /// <list type="bullet">
    /// <item>Track / catch window: idle or run (glove up, looking back once presses count).</item>
    /// <item>Catch sequence (Screen 04): over-the-shoulder catch, then the spin toward the infield.</item>
    /// <item>Throw: set pose while the meter runs, then the throw and follow-through.</item>
    /// <item>Miss: the whiff, held until the TRY AGAIN screen.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutfielderAnimator : MonoBehaviour
    {
        private enum Mode { Hold, Locomotion, OneShot }

        [SerializeField] private GameFlowController flow;
        [SerializeField] private OutfielderController outfielder;
        [SerializeField] private CatchSystem catchSystem;
        [SerializeField] private ThrowSystem throwSystem;
        [SerializeField] private GameplayTuning tuning;
        [SerializeField] private OutfielderAnimationSet clips;
        [SerializeField] private SpriteAnimator body;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("Speed (units/s) above which the run cycle plays.")]
        [SerializeField] private float runThreshold = 0.25f;

        private Mode _mode;
        private SpriteAnimationClip _next;

        /// <summary>The clip on screen (tests / debugging).</summary>
        public SpriteAnimationClip CurrentClip => body != null ? body.CurrentClip : null;

        private void Awake()
        {
            if (flow == null || outfielder == null || catchSystem == null || throwSystem == null ||
                tuning == null || clips == null || body == null || bodyRenderer == null)
            {
                Debug.LogError($"[{nameof(OutfielderAnimator)}] Missing reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            flow.StateChanged += OnStateChanged;
            catchSystem.CatchResolved += OnCatchResolved;
            throwSystem.ThrowResolved += OnThrowResolved;
            Hold(clips.idle);
        }

        private void OnDisable()
        {
            flow.StateChanged -= OnStateChanged;
            catchSystem.CatchResolved -= OnCatchResolved;
            throwSystem.ThrowResolved -= OnThrowResolved;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            switch (next)
            {
                case GameState.TrackBall:
                    _mode = Mode.Locomotion;
                    break;
                case GameState.CatchSequence:
                    // The ball is in the glove: finish the catch, then spin to face the infield.
                    PlayOneShot(clips.catchBall, clips.spin);
                    break;
                case GameState.Throw:
                    Hold(clips.throwReady);
                    break;
                case GameState.CatchWindow:
                case GameState.MissionFailed:
                    break; // keep running / keep the whiff pose
                default:
                    Hold(clips.idle);
                    break;
            }
        }

        private void OnCatchResolved(CatchResult result)
        {
            if (!result.Success) PlayOneShot(clips.whiff, null);
        }

        private void OnThrowResolved(ThrowResult result)
        {
            if (result.TimedOut) Hold(clips.idle);
            else PlayOneShot(clips.throwRelease, null);
        }

        private void LateUpdate()
        {
            body.Speed = 1f;
            switch (_mode)
            {
                case Mode.Locomotion:
                    UpdateLocomotion();
                    break;
                case Mode.OneShot:
                    if (!body.IsFinished) break;
                    if (_next != null)
                    {
                        SpriteAnimationClip next = _next;
                        _next = null;
                        if (next == clips.spin) FaceForSpin();
                        body.Play(next, true);
                    }
                    else _mode = Mode.Hold;
                    break;
            }
        }

        private void UpdateLocomotion()
        {
            float speed = outfielder.Velocity.magnitude;
            bool reaching = catchSystem.IsActive && catchSystem.Cue != CatchCue.None;
            if (speed > runThreshold)
            {
                body.Play(reaching ? clips.runReach : clips.run);
                body.Speed = Mathf.Clamp(speed / Mathf.Max(0.1f, tuning.playerRunSpeed), 0.6f, 1.2f);
            }
            else
            {
                body.Play(reaching ? clips.reachIdle : clips.idle);
            }
        }

        /// <summary>
        /// The spin is drawn ending facing left, so it is flipped when the infield is to the
        /// right. The flow then faces the outfielder toward the target for the throw.
        /// </summary>
        private void FaceForSpin()
        {
            bool targetIsLeft = tuning.throwTargetPoint.x < outfielder.Position.x;
            bodyRenderer.flipX = !targetIsLeft;
        }

        private void PlayOneShot(SpriteAnimationClip clip, SpriteAnimationClip next)
        {
            _mode = Mode.OneShot;
            _next = next;
            body.Play(clip, true);
        }

        private void Hold(SpriteAnimationClip clip)
        {
            _mode = Mode.Hold;
            _next = null;
            body.Play(clip);
        }
    }
}
