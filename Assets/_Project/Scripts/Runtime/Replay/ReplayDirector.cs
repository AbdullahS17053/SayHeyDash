using System;
using SayHeyDash.Audio;
using SayHeyDash.Data;
using SayHeyDash.Flow;
using UnityEngine;

namespace SayHeyDash.Replay
{
    /// <summary>
    /// Plays the simplified animated replay of the real play on Screen 07 (GDD): markers move
    /// over a top-down field diagram, step by step, following <see cref="ReplayTimeline"/>.
    /// Step lengths and words come from <see cref="MissionContent.replayBeats"/>; the last step
    /// holds. Reduced motion: markers jump straight to where each step ends (no gliding), the
    /// steps still advance. Sounds mark the hit, the catch, the throw and the result.
    /// Only runs while the replay screen is open; uses unscaled time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReplayDirector : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private MissionContent content;
        [SerializeField] private AudioService audioService;
        [Tooltip("The field diagram image; markers are its children, anchored to its top-left corner.")]
        [SerializeField] private RectTransform diagram;
        [SerializeField] private RectTransform ball;
        [SerializeField] private RectTransform fielder;
        [SerializeField] private RectTransform leadRunner;
        [SerializeField] private RectTransform trailRunner;
        [SerializeField] private RectTransform batter;
        [Tooltip("Dots left behind by the fly ball (optional, any number).")]
        [SerializeField] private RectTransform[] trail = Array.Empty<RectTransform>();
        [Tooltip("Ball size in diagram pixels when on the ground.")]
        [Min(1f)] [SerializeField] private float ballSize = 2f;

        private int _step;
        private float _stepTime;
        private bool _playing;

        /// <summary>Raised when a new step starts (step index).</summary>
        public event Action<int> StepChanged;

        public int Step => _step;
        public bool IsPlaying => _playing;
        public bool IsAtLastStep => _step >= ReplayTimeline.StepCount - 1;
        public float StepProgress => Mathf.Clamp01(StepSeconds(_step) > 0f ? _stepTime / StepSeconds(_step) : 1f);

        private void Awake()
        {
            if (flow == null || content == null || audioService == null || diagram == null || ball == null ||
                fielder == null || leadRunner == null || trailRunner == null || batter == null)
            {
                Debug.LogError($"[{nameof(ReplayDirector)}] Missing reference.", this);
                enabled = false;
                return;
            }
            if (content.replayBeats == null || content.replayBeats.Length != ReplayTimeline.StepCount)
                Debug.LogWarning($"[{nameof(ReplayDirector)}] MissionContent.replayBeats should have " +
                                 $"{ReplayTimeline.StepCount} entries (one per replay step).", this);
        }

        // ------------------------------------------------------------------ commands

        /// <summary>Starts (or restarts) the replay from the first step.</summary>
        public void Play()
        {
            if (!enabled) return;
            _playing = true;
            GoTo(0, false);
        }

        public void Stop() => _playing = false;

        /// <summary>Skips to the next step (GDD: the historical introduction is skippable).</summary>
        public void Next()
        {
            if (!enabled || IsAtLastStep) return;
            _playing = true;
            GoTo(_step + 1, true);
        }

        /// <summary>Beat words for a step (empty when the content asset has fewer entries).</summary>
        public MissionContent.ReplayBeat Beat(int step) =>
            content.replayBeats != null && step >= 0 && step < content.replayBeats.Length
                ? content.replayBeats[step]
                : default;

        // ------------------------------------------------------------------ playback

        private void Update()
        {
            if (!_playing) return;
            _stepTime += Time.unscaledDeltaTime;
            float seconds = StepSeconds(_step);
            if (seconds > 0f && _stepTime >= seconds && !IsAtLastStep)
            {
                GoTo(_step + 1, true);
                return;
            }
            Render();
        }

        private void GoTo(int step, bool withSound)
        {
            _step = Mathf.Clamp(step, 0, ReplayTimeline.StepCount - 1);
            _stepTime = 0f;
            if (withSound) PlayStepSound(_step);
            Render();
            StepChanged?.Invoke(_step);
        }

        private float StepSeconds(int step)
        {
            if (IsLast(step)) return 0f;
            MissionContent.ReplayBeat beat = Beat(step);
            return beat.seconds > 0f ? beat.seconds : 3f;
        }

        private static bool IsLast(int step) => step >= ReplayTimeline.StepCount - 1;

        private void Render()
        {
            float t = flow.ReducedMotion ? 1f : StepProgress;
            Place(fielder, ReplayActor.Fielder, t);
            Place(leadRunner, ReplayActor.LeadRunner, t);
            Place(trailRunner, ReplayActor.TrailRunner, t);
            Place(batter, ReplayActor.Batter, t);
            Place(ball, ReplayActor.Ball, t);
            float size = ballSize * ReplayTimeline.BallScale(_step, flow.ReducedMotion ? 0f : t) * PixelScale;
            ball.sizeDelta = new Vector2(Mathf.Round(size / PixelScale) * PixelScale, Mathf.Round(size / PixelScale) * PixelScale);
            RenderTrail(t);
        }

        /// <summary>Canvas units per diagram pixel.</summary>
        private float PixelScale => diagram.rect.width / ReplayTimeline.DiagramWidth;

        private void Place(RectTransform marker, ReplayActor actor, float t)
        {
            bool visible = ReplayTimeline.IsVisible(actor, _step, t);
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
            if (!visible) return;
            marker.anchoredPosition = ToCanvas(ReplayTimeline.Position(actor, _step, t));
        }

        /// <summary>The fly ball's path stays drawn as dots once the ball has passed them.</summary>
        private void RenderTrail(float t)
        {
            float progress = _step < ReplayTimeline.Drive ? 0f : (_step == ReplayTimeline.Drive ? t : 1f);
            for (int i = 0; i < trail.Length; i++)
            {
                if (trail[i] == null) continue;
                float at = (i + 1f) / (trail.Length + 1f);
                bool on = at <= progress;
                if (trail[i].gameObject.activeSelf != on) trail[i].gameObject.SetActive(on);
                if (on) trail[i].anchoredPosition = ToCanvas(ReplayTimeline.Position(ReplayActor.Ball, ReplayTimeline.Drive, at));
            }
        }

        private Vector2 ToCanvas(Vector2 diagramPx)
        {
            float k = PixelScale;
            // Snap to whole diagram pixels so markers stay on the pixel grid.
            return new Vector2(Mathf.Round(diagramPx.x) * k, -Mathf.Round(diagramPx.y) * k);
        }

        private void PlayStepSound(int step)
        {
            switch (step)
            {
                case ReplayTimeline.Drive: audioService.Play(AudioCue.BatCrack); break;
                case ReplayTimeline.Catch: audioService.Play(AudioCue.Catch); break;
                case ReplayTimeline.Throw: audioService.Play(AudioCue.Throw); break;
                case ReplayTimeline.Result: audioService.Play(AudioCue.CrowdCheer); break;
            }
        }
    }
}
