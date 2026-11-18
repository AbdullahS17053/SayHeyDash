using System;
using SayHeyDash.Data;
using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// Stage C: a needle sweeps an accuracy meter; pressing Action while it is inside the
    /// target zone is a good throw. Running out of time counts as THROW MISSED.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThrowSystem : MonoBehaviour
    {
        [SerializeField] private GameplayTuning tuning;

        private bool _active;
        private float _elapsed;
        private float _needle;
        private float _zoneWidth;

        public event Action ThrowStarted;
        public event Action<ThrowResult> ThrowResolved;

        public bool IsActive => _active;
        /// <summary>0 = left end of the meter, 1 = right end.</summary>
        public float Needle => _needle;
        public float ZoneCenter => tuning.throwTargetZoneCenter;
        public float ZoneWidth => _zoneWidth;
        public float TimeLimit => tuning.throwTimeLimitSeconds;
        public float Elapsed => _elapsed;

        private void Awake()
        {
            if (tuning == null)
            {
                Debug.LogError($"[{nameof(ThrowSystem)}] Missing tuning reference.", this);
                enabled = false;
            }
        }

        public void Begin(DifficultyProfile difficulty)
        {
            _zoneWidth = Mathf.Clamp01(tuning.throwTargetZoneWidth * difficulty.throwZoneMultiplier);
            _elapsed = 0f;
            _needle = 0f;
            _active = true;
            ThrowStarted?.Invoke();
        }

        public void Cancel() => _active = false;

        /// <summary>Action pressed during Stage C.</summary>
        public bool TryThrow()
        {
            if (!_active) return false;
            Resolve(ThrowJudge.Judge(_needle, ZoneCenter, _zoneWidth));
            return true;
        }

        private void Update()
        {
            if (!_active) return;
            _elapsed += Time.deltaTime;
            _needle = ThrowJudge.NeedlePosition(_elapsed, tuning.throwMeterSpeed);
            if (tuning.throwTimeLimitSeconds > 0f && _elapsed >= tuning.throwTimeLimitSeconds)
                Resolve(ThrowJudge.TimedOut());
        }

        private void Resolve(ThrowResult result)
        {
            _active = false;
            ThrowResolved?.Invoke(result);
        }
    }
}
