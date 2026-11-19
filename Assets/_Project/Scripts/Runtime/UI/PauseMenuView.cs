using SayHeyDash.Audio;
using SayHeyDash.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>Pause overlay (not one of the seven states): RESUME, RESTART and SOUND.</summary>
    public sealed class PauseMenuView : ScreenView
    {
        [SerializeField] private AudioService audioService;
        [SerializeField] private Text titleText;
        [SerializeField] private PixelButton resumeButton;
        [SerializeField] private PixelButton restartButton;
        [SerializeField] private PixelButton soundButton;

        protected override bool Validate() =>
            audioService != null && titleText != null && resumeButton != null && restartButton != null && soundButton != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            flow.PauseChanged += OnPauseChanged;
            audioService.MuteChanged += OnMuteChanged;
            OnMuteChanged(audioService.Muted);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (flow != null) flow.PauseChanged -= OnPauseChanged;
            if (audioService != null) audioService.MuteChanged -= OnMuteChanged;
        }

        protected override bool IsVisibleIn(GameState state) => flow.IsPaused;

        protected override void ApplyContent()
        {
            SetText(titleText, content.pausedTitle);
            resumeButton.Label = content.resumeButton;
            restartButton.Label = content.restartButton;
        }

        protected override void OnHidden() => UiSelection.Select(null); // back to play: nothing focused

        public void OnResumeClicked() => flow.Resume();

        public void OnRestartClicked() => flow.RestartAttempt();

        public void OnSoundClicked() => audioService.ToggleMute();

        private void OnPauseChanged(bool _) => Refresh(flow.State);

        private void OnMuteChanged(bool muted) =>
            soundButton.Label = muted ? content.soundOffLabel : content.soundOnLabel;
    }
}
