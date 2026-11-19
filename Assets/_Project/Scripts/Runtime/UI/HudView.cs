using SayHeyDash.Audio;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screens 03/04 - the gameplay HUD: game-state prompts, catch cues (GET READY / NOW),
    /// in-zone label, the game situation chip, the throw meter with word feedback, the
    /// "OVER THE SHOULDER!" catch-sequence banner, and the pause and sound buttons.
    /// Prompts change words AND colour so the cue never relies on colour alone.
    /// </summary>
    public sealed class HudView : ScreenView
    {
        [SerializeField] private CatchSystem catchSystem;
        [SerializeField] private ThrowSystem throwSystem;
        [SerializeField] private AudioService audioService;

        [Header("Prompts")]
        [SerializeField] private Text promptText;
        [SerializeField] private Text subText;
        [SerializeField] private Text situationText;
        [SerializeField] private Text modeText;
        [SerializeField] private Color promptColor = new Color32(0xF5, 0xF1, 0xD9, 0xFF);
        [SerializeField] private Color readyColor = new Color32(0xF6, 0xCE, 0x4B, 0xFF);
        [SerializeField] private Color nowColor = new Color32(0xF3, 0x8B, 0x37, 0xFF);

        [Header("Catch sequence (04)")]
        [SerializeField] private GameObject catchBanner;
        [SerializeField] private Text catchHeadlineText;
        [SerializeField] private Text skipHintText;

        [Header("Throw")]
        [SerializeField] private ThrowMeterView throwMeter;
        [SerializeField] private Text throwLabelText;
        [SerializeField] private Text throwFeedbackText;

        [Header("Buttons")]
        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        protected override bool Validate() =>
            catchSystem != null && throwSystem != null && audioService != null && promptText != null &&
            subText != null && situationText != null && modeText != null && catchBanner != null &&
            catchHeadlineText != null && skipHintText != null && throwMeter != null && throwLabelText != null &&
            throwFeedbackText != null && soundIcon != null && soundOnSprite != null && soundOffSprite != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            catchSystem.CueChanged += OnCueChanged;
            catchSystem.InZoneChanged += OnInZoneChanged;
            throwSystem.ThrowResolved += OnThrowResolved;
            audioService.MuteChanged += OnMuteChanged;
            OnMuteChanged(audioService.Muted);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (catchSystem != null)
            {
                catchSystem.CueChanged -= OnCueChanged;
                catchSystem.InZoneChanged -= OnInZoneChanged;
            }
            if (throwSystem != null) throwSystem.ThrowResolved -= OnThrowResolved;
            if (audioService != null) audioService.MuteChanged -= OnMuteChanged;
        }

        protected override bool IsVisibleIn(GameState state) => GameFlowRules.IsGameplay(state);

        protected override void ApplyContent()
        {
            SetText(situationText, content.hudSituation);
            SetText(catchHeadlineText, content.catchSequenceHeadline);
            SetText(skipHintText, content.skipHint);
            SetText(throwLabelText, content.throwMeterLabel);
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            modeText.text = flow.AssistedMode ? content.assistedOnLabel : string.Empty;
            catchBanner.SetActive(state == GameState.CatchSequence);
            // The meter stays up after the press (needle frozen) so the feedback reads against it.
            if (state == GameState.Throw)
            {
                throwMeter.Show();
                throwFeedbackText.text = string.Empty;
            }
            else
            {
                throwMeter.Hide();
            }
            throwLabelText.enabled = state == GameState.Throw;

            switch (state)
            {
                case GameState.TrackBall:
                    SetPrompt(content.trackPrompt, promptColor);
                    subText.text = catchSystem.InZone ? content.inZoneLabel : content.outOfZoneLabel;
                    throwFeedbackText.text = string.Empty;
                    break;
                case GameState.CatchWindow:
                    break; // cue events drive the prompt
                case GameState.CatchSequence:
                    SetPrompt(string.Empty, promptColor);
                    subText.text = string.Empty;
                    break;
                case GameState.Throw:
                    SetPrompt(content.throwPrompt, readyColor);
                    subText.text = content.throwTargetLabel;
                    break;
            }
        }

        // ------------------------------------------------------------------ buttons

        public void OnPauseClicked() => flow.TogglePause();

        public void OnSoundClicked() => audioService.ToggleMute();

        // ------------------------------------------------------------------ events

        private void OnCueChanged(CatchCue cue)
        {
            if (flow.State != GameState.TrackBall && flow.State != GameState.CatchWindow) return;
            switch (cue)
            {
                case CatchCue.Ready: SetPrompt(content.readyPrompt, readyColor); break;
                case CatchCue.Now: SetPrompt(content.nowPrompt, nowColor); break;
                default: SetPrompt(content.trackPrompt, promptColor); break;
            }
        }

        private void OnInZoneChanged(bool inZone)
        {
            if (flow.State == GameState.TrackBall || flow.State == GameState.CatchWindow)
                subText.text = inZone ? content.inZoneLabel : content.outOfZoneLabel;
        }

        private void OnThrowResolved(ThrowResult result)
        {
            throwFeedbackText.text = content.ThrowFeedback(result.Success, result.TimedOut, result.Quality);
            SetPrompt(string.Empty, promptColor);
            subText.text = string.Empty;
        }

        private void OnMuteChanged(bool muted) => soundIcon.sprite = muted ? soundOffSprite : soundOnSprite;

        private void SetPrompt(string text, Color color)
        {
            promptText.text = text;
            promptText.color = color;
        }
    }
}
