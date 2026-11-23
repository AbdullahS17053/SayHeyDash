using System;
using SayHeyDash.Audio;
using SayHeyDash.Flow;
using SayHeyDash.Platform;
using SayHeyDash.Scoring;
using UnityEngine;

namespace SayHeyDash.Settings
{
    /// <summary>
    /// Remembers the player's options (mute, music / effects volume, reduced motion, assisted
    /// mode) and the best score between visits, best-effort (Docs/04 F6). The flow controller and
    /// the audio service stay the runtime owners of those values: this component loads them at
    /// start-up, pushes them in, and saves whenever they change. First visit: reduced motion
    /// follows the browser's "reduce motion" preference.
    /// Runs before the screens (execution order) so they open with the saved values.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SettingsService : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private AudioService audioService;
        [Tooltip("Editor only: save settings in the Editor's PlayerPrefs too. Off = every Play starts from the defaults " +
                 "(keeps tests and reviews repeatable). Builds always save.")]
        [SerializeField] private bool persistInEditor;

        private ISettingsStore _store;
        private SettingsData _data;
        private bool _applying;

        /// <summary>Raised after any saved value changes (options or best score).</summary>
        public event Action Changed;

        public SettingsData Data => _data;
        public bool IsPersistent => _store != null && _store.IsPersistent;
        /// <summary>True when the browser refused storage (not when the Editor simply isn't saving).</summary>
        public bool StorageBlocked => _store is PlayerPrefsSettingsStore && !_store.IsPersistent;
        public int BestScore => _data.BestScore;
        /// <summary>True when the last completed mission set a new best score.</summary>
        public bool LastScoreWasNewBest { get; private set; }

        private void Awake()
        {
            if (flow == null || audioService == null)
            {
                Debug.LogError($"[{nameof(SettingsService)}] Missing reference.", this);
                enabled = false;
                return;
            }
            bool persist = !Application.isEditor || persistInEditor;
            UseStore(persist ? new PlayerPrefsSettingsStore() : new MemorySettingsStore());
        }

        private void OnEnable()
        {
            flow.AssistedModeChanged += OnAssistedChanged;
            flow.ReducedMotionChanged += OnReducedMotionChanged;
            flow.MissionCompleted += OnMissionCompleted;
            audioService.MuteChanged += OnMuteChanged;
            audioService.VolumesChanged += OnVolumesChanged;
        }

        private void OnDisable()
        {
            flow.AssistedModeChanged -= OnAssistedChanged;
            flow.ReducedMotionChanged -= OnReducedMotionChanged;
            flow.MissionCompleted -= OnMissionCompleted;
            audioService.MuteChanged -= OnMuteChanged;
            audioService.VolumesChanged -= OnVolumesChanged;
        }

        // ------------------------------------------------------------------ menu commands

        public void CycleMusicVolume() =>
            audioService.SetVolumes(SettingsData.LevelToVolume(SettingsData.NextLevelDown(_data.MusicLevel)),
                audioService.EffectsVolume);

        public void CycleEffectsVolume() =>
            audioService.SetVolumes(audioService.MusicVolume,
                SettingsData.LevelToVolume(SettingsData.NextLevelDown(_data.EffectsLevel)));

        // ------------------------------------------------------------------ test hooks

        /// <summary>Swaps the store and re-applies what it holds (tests use a memory store).</summary>
        internal void UseStore(ISettingsStore store)
        {
            _store = store;
            _data = SettingsSerializer.Load(_store, BrowserBridge.PrefersReducedMotion);
            Apply();
        }

        // ------------------------------------------------------------------ internals

        private void Apply()
        {
            _applying = true;
            flow.SetReducedMotion(_data.ReducedMotion);
            flow.SetAssistedMode(_data.AssistedMode);
            audioService.SetMuted(_data.Muted);
            audioService.SetVolumes(SettingsData.LevelToVolume(_data.MusicLevel),
                SettingsData.LevelToVolume(_data.EffectsLevel));
            _applying = false;
            Changed?.Invoke();
        }

        private void OnAssistedChanged(bool value) => SetAndSave(ref _data.AssistedMode, value);
        private void OnReducedMotionChanged(bool value) => SetAndSave(ref _data.ReducedMotion, value);
        private void OnMuteChanged(bool value) => SetAndSave(ref _data.Muted, value);

        private void OnVolumesChanged()
        {
            _data.MusicLevel = ToLevel(audioService.MusicVolume);
            _data.EffectsLevel = ToLevel(audioService.EffectsVolume);
            Save();
        }

        private void OnMissionCompleted(ScoreBreakdown score)
        {
            LastScoreWasNewBest = score.Total > _data.BestScore;
            if (!LastScoreWasNewBest) return;
            _data.BestScore = score.Total;
            Save();
        }

        private void SetAndSave(ref bool field, bool value)
        {
            field = value;
            Save();
        }

        private void Save()
        {
            if (!_applying) SettingsSerializer.Save(_store, _data);
            Changed?.Invoke();
        }

        private static int ToLevel(float volume) =>
            SettingsData.ClampLevel(Mathf.RoundToInt(volume * SettingsData.VolumeSteps));
    }
}
