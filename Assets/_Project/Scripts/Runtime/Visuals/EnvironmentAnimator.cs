using SayHeyDash.Flow;
using UnityEngine;

namespace SayHeyDash.Visuals
{
    /// <summary>
    /// Decorative stadium life: the crowd shuffles, cheers after the catch and on mission
    /// complete, and the clubhouse flags wave. Reduced motion freezes all of it
    /// (GDD: reduced-motion option for decorative animations). No gameplay meaning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentAnimator : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private SpriteAnimator crowd;
        [SerializeField] private SpriteAnimator flags;
        [SerializeField] private SpriteAnimationClip crowdIdle = new SpriteAnimationClip { name = "crowdIdle", framesPerSecond = 2f };
        [SerializeField] private SpriteAnimationClip crowdCheer = new SpriteAnimationClip { name = "crowdCheer", framesPerSecond = 6f };
        [SerializeField] private SpriteAnimationClip flagsWave = new SpriteAnimationClip { name = "flags", framesPerSecond = 3f };
        [Min(0f)] [SerializeField] private float cheerSeconds = 2.5f;

        private float _cheerUntil = -1f;

        public bool IsCheering => Time.time < _cheerUntil;

        private void Awake()
        {
            if (flow == null || crowd == null || flags == null)
            {
                Debug.LogError($"[{nameof(EnvironmentAnimator)}] Missing reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            flow.StateChanged += OnStateChanged;
            crowd.Play(crowdIdle);
            flags.Play(flagsWave);
        }

        private void OnDisable() => flow.StateChanged -= OnStateChanged;

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.CatchSequence || next == GameState.MissionComplete)
                _cheerUntil = Time.time + cheerSeconds;
            else if (next == GameState.TrackBall || next == GameState.Title || next == GameState.MissionFailed)
                _cheerUntil = -1f;
        }

        private void Update()
        {
            bool still = flow.ReducedMotion;
            crowd.Paused = still;
            flags.Paused = still;
            crowd.Play(IsCheering ? crowdCheer : crowdIdle);
        }
    }
}
