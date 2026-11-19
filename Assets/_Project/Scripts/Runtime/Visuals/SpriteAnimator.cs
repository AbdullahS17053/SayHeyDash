using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.Visuals
{
    /// <summary>
    /// Plays <see cref="SpriteAnimationClip"/>s on a SpriteRenderer (world) or an Image (UI).
    /// Deliberately tiny instead of Unity's Animator: no controller assets, no per-frame
    /// allocations, and the clip in use is visible to tests. Uses scaled time by default,
    /// so the game's pause freezes it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Image image;
        [Tooltip("Optional clip that starts automatically (decorative loops).")]
        [SerializeField] private SpriteAnimationClip startClip;
        [SerializeField] private bool useUnscaledTime;

        private SpriteAnimationClip _clip;
        private float _time;
        private int _frame = -1;

        public SpriteAnimationClip CurrentClip => _clip;
        public int CurrentFrame => _frame;
        public bool IsFinished => _clip != null &&
                                  SpriteAnimationClip.IsFinishedAt(_clip.FrameCount, _clip.framesPerSecond, _time, _clip.loop);
        /// <summary>Playback rate multiplier (e.g. run cycle follows running speed).</summary>
        public float Speed { get; set; } = 1f;
        /// <summary>Holds the current frame (reduced motion).</summary>
        public bool Paused { get; set; }

        private void Awake()
        {
            if (spriteRenderer == null && image == null)
            {
                Debug.LogError($"[{nameof(SpriteAnimator)}] Needs a SpriteRenderer or an Image.", this);
                enabled = false;
                return;
            }
            if (startClip != null && startClip.IsValid) Play(startClip);
        }

        /// <summary>Starts a clip. Playing the clip that is already running does nothing unless <paramref name="restart"/>.</summary>
        public void Play(SpriteAnimationClip clip, bool restart = false)
        {
            if (clip == null || !clip.IsValid) return;
            if (clip == _clip && !restart) return;
            _clip = clip;
            _time = 0f;
            _frame = -1;
            Apply(0);
        }

        private void Update()
        {
            if (_clip == null || Paused) return;
            _time += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * Speed;
            Apply(SpriteAnimationClip.FrameAt(_clip.FrameCount, _clip.framesPerSecond, _time, _clip.loop));
        }

        private void Apply(int frame)
        {
            if (frame == _frame || frame < 0) return;
            _frame = frame;
            Sprite sprite = _clip.frames[frame];
            if (spriteRenderer != null) spriteRenderer.sprite = sprite;
            if (image != null) image.sprite = sprite;
        }
    }
}
