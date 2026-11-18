using System;
using SayHeyDash.Data;
using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// Stage B: watches the ball and the outfielder, raises timing cues, and judges the
    /// catch press via <see cref="CatchJudge"/>. Presses before the input-open time are
    /// ignored (so a stray press while running never fails the attempt); presses after it
    /// are judged; no press by the end of the window is a miss.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CatchSystem : MonoBehaviour
    {
        [SerializeField] private GameplayTuning tuning;
        [SerializeField] private BallFlight ball;
        [SerializeField] private OutfielderController outfielder;
        [SerializeField] private LandingIndicator landingIndicator;

        private bool _active;
        private bool _inZone;
        private CatchCue _cue;
        private float _windowSeconds;
        private float _zoneRadius;
        private float _inputOpenTime;

        /// <summary>Presses now count (the flow moves from TrackBall to CatchWindow).</summary>
        public event Action CatchWindowOpened;
        public event Action<CatchCue> CueChanged;
        public event Action<bool> InZoneChanged;
        public event Action<CatchResult> CatchResolved;

        public bool IsActive => _active;
        public bool InZone => _inZone;
        public CatchCue Cue => _cue;
        public float WindowSeconds => _windowSeconds;
        public float ZoneRadius => _zoneRadius;
        public float InputOpenTime => _inputOpenTime;
        public float PerfectTime => ball.PerfectCatchTime;
        public float DistanceToLanding => Vector2.Distance(outfielder.Position, tuning.landingPoint);

        private void Awake()
        {
            if (tuning == null || ball == null || outfielder == null || landingIndicator == null)
            {
                Debug.LogError($"[{nameof(CatchSystem)}] Missing reference.", this);
                enabled = false;
            }
        }

        /// <summary>Call right after <see cref="BallFlight.Launch"/>.</summary>
        public void Begin(DifficultyProfile difficulty)
        {
            _windowSeconds = tuning.catchWindowSeconds * difficulty.catchWindowMultiplier;
            _zoneRadius = tuning.landingZoneRadius * difficulty.landingZoneMultiplier;
            _inputOpenTime = CatchJudge.InputOpenTime(ball.PerfectCatchTime, _windowSeconds, tuning.catchInputOpenSeconds);
            _active = true;
            _inZone = false;
            SetCue(CatchCue.None);
            landingIndicator.Show(tuning.landingPoint, _zoneRadius);
            InZoneChanged?.Invoke(false);
        }

        public void Cancel()
        {
            _active = false;
            _inZone = false;
            _cue = CatchCue.None;
            landingIndicator.Hide();
        }

        /// <summary>Action pressed. Returns false if the press was ignored (too far ahead of the window).</summary>
        public bool TryCatch()
        {
            if (!_active || ball.Elapsed < _inputOpenTime) return false;
            Resolve(CatchJudge.Judge(ball.Elapsed, ball.PerfectCatchTime, _windowSeconds, DistanceToLanding, _zoneRadius));
            return true;
        }

        private void Update()
        {
            if (!_active) return;

            bool inZone = DistanceToLanding <= _zoneRadius;
            if (inZone != _inZone)
            {
                _inZone = inZone;
                landingIndicator.SetInZone(inZone);
                InZoneChanged?.Invoke(inZone);
            }

            float t = ball.Elapsed;
            float half = _windowSeconds * 0.5f;
            if (_cue == CatchCue.None && t >= _inputOpenTime)
            {
                SetCue(CatchCue.Ready);
                CatchWindowOpened?.Invoke();
            }
            if (_cue == CatchCue.Ready && t >= ball.PerfectCatchTime - half)
                SetCue(CatchCue.Now);
            if (t > ball.PerfectCatchTime + half)
                Resolve(CatchJudge.NoPress(DistanceToLanding, _zoneRadius, half));
        }

        private void Resolve(CatchResult result)
        {
            _active = false;
            SetCue(CatchCue.None);
            landingIndicator.Hide();
            CatchResolved?.Invoke(result);
        }

        private void SetCue(CatchCue cue)
        {
            if (_cue == cue) return;
            _cue = cue;
            CueChanged?.Invoke(cue);
        }
    }
}
