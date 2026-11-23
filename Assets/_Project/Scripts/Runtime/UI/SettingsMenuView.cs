using SayHeyDash.Audio;
using SayHeyDash.Flow;
using SayHeyDash.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Settings overlay (not one of the seven states), opened from the title screen or the pause
    /// menu: SOUND on/off, MUSIC and EFFECTS volume (one press = one step down, wraps to 100%),
    /// REDUCED MOTION, ASSISTED MODE (between attempts only) and BACK. Every row is a labelled
    /// button showing its current value in words. A note says whether settings are saved on this
    /// device or (storage blocked) last for this visit only. Closes itself if the game state
    /// changes underneath it (e.g. Esc resumes play).
    /// </summary>
    public sealed class SettingsMenuView : ScreenView
    {
        [SerializeField] private SettingsService settings;
        [SerializeField] private AudioService audioService;
        [SerializeField] private Text titleText;
        [SerializeField] private Text noteText;
        [SerializeField] private PixelButton soundButton;
        [SerializeField] private PixelButton musicButton;
        [SerializeField] private PixelButton effectsButton;
        [SerializeField] private PixelButton reducedMotionButton;
        [SerializeField] private PixelButton assistedButton;
        [SerializeField] private PixelButton backButton;

        private bool _open;
        private Selectable _returnFocus;

        public bool IsOpen => _open;

        protected override bool Validate() =>
            settings != null && audioService != null && titleText != null && noteText != null && soundButton != null &&
            musicButton != null && effectsButton != null && reducedMotionButton != null && assistedButton != null &&
            backButton != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!enabled) return;
            settings.Changed += RefreshLabels;
            flow.PauseChanged += OnPauseChanged;
            flow.StateChanged += OnStateChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (settings != null) settings.Changed -= RefreshLabels;
            if (flow != null)
            {
                flow.PauseChanged -= OnPauseChanged;
                flow.StateChanged -= OnStateChanged;
            }
        }

        protected override bool IsVisibleIn(GameState state) => _open;

        protected override void ApplyContent()
        {
            SetText(titleText, content.settingsTitle);
            backButton.Label = content.closeButton;
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            if (justShown) RefreshLabels();
        }

        // ------------------------------------------------------------------ open / close

        /// <summary>Opens the menu; focus returns to <paramref name="returnFocus"/> when it closes.</summary>
        public void Open(Selectable returnFocus)
        {
            _returnFocus = returnFocus;
            _open = true;
            Refresh(flow.State);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            Refresh(flow.State);
            UiSelection.Select(_returnFocus);
            _returnFocus = null;
        }

        // ------------------------------------------------------------------ buttons

        public void OnSoundClicked() => audioService.ToggleMute();

        public void OnMusicClicked() => settings.CycleMusicVolume();

        public void OnEffectsClicked() => settings.CycleEffectsVolume();

        public void OnReducedMotionClicked() => flow.ToggleReducedMotion();

        public void OnAssistedClicked() => flow.ToggleAssistedMode();

        public void OnBackClicked() => Close();

        // ------------------------------------------------------------------ internals

        private void OnPauseChanged(bool paused)
        {
            if (!paused) Close();
        }

        private void OnStateChanged(GameState previous, GameState next) => Close();

        private void RefreshLabels()
        {
            if (!_open) return;
            SettingsData d = settings.Data;
            soundButton.Label = d.Muted ? content.soundOffLabel : content.soundOnLabel;
            musicButton.Label = string.Format(content.musicVolumeFormat, SettingsData.Percent(d.MusicLevel));
            effectsButton.Label = string.Format(content.effectsVolumeFormat, SettingsData.Percent(d.EffectsLevel));
            reducedMotionButton.Label = d.ReducedMotion ? content.reducedMotionOnLabel : content.reducedMotionOffLabel;
            assistedButton.Label = d.AssistedMode ? content.assistedOnLabel : content.assistedOffLabel;
            bool canAssist = flow.CanChangeAssistedMode;
            assistedButton.Button.interactable = canAssist;
            if (!canAssist) SetText(noteText, content.assistedLockedNote);
            else SetText(noteText, settings.StorageBlocked ? content.settingsSessionNote : content.settingsSavedNote);
        }
    }
}
