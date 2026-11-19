using SayHeyDash.Audio;
using SayHeyDash.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 01 - Title / Press Start: the presents line, pixel-art title treatment, the
    /// mission lines, a blinking PRESS START, and START / SOUND / ASSISTED MODE / CREDITS.
    /// Credits open as an overlay inside this screen (not a game state).
    /// </summary>
    public sealed class TitleScreenView : ScreenView
    {
        [SerializeField] private AudioService audioService;

        [Header("Text")]
        [SerializeField] private Text presentsText;
        [SerializeField] private Text arcadeModeText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text pressStartText;

        [Header("Buttons")]
        [SerializeField] private PixelButton startButton;
        [SerializeField] private PixelButton soundButton;
        [SerializeField] private PixelButton assistedButton;
        [SerializeField] private PixelButton creditsButton;

        [Header("Credits overlay")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Text creditsTitleText;
        [SerializeField] private Text creditsBodyText;
        [SerializeField] private PixelButton creditsBackButton;

        public bool CreditsOpen => creditsPanel != null && creditsPanel.activeSelf;

        protected override bool Validate() =>
            audioService != null && presentsText != null && arcadeModeText != null && infoText != null &&
            pressStartText != null && startButton != null && soundButton != null && assistedButton != null &&
            creditsButton != null && creditsPanel != null && creditsTitleText != null && creditsBodyText != null &&
            creditsBackButton != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!enabled) return;
            flow.AssistedModeChanged += OnAssistedModeChanged;
            audioService.MuteChanged += OnMuteChanged;
            RefreshToggles();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (flow != null) flow.AssistedModeChanged -= OnAssistedModeChanged;
            if (audioService != null) audioService.MuteChanged -= OnMuteChanged;
        }

        protected override bool IsVisibleIn(GameState state) => state == GameState.Title;

        protected override void ApplyContent()
        {
            SetText(presentsText, content.presentsLine);
            SetText(arcadeModeText, content.arcadeModeLine);
            SetText(infoText, $"{content.playerLine}\n{content.seriesLine}\n{content.levelLine}");
            SetText(pressStartText, content.pressStart);
            startButton.Label = content.startButton;
            creditsButton.Label = content.creditsButton;
            SetText(creditsTitleText, content.creditsTitle);
            SetText(creditsBodyText, content.creditsText);
            creditsBackButton.Label = content.closeButton;
            creditsPanel.SetActive(false);
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            if (!justShown) return;
            creditsPanel.SetActive(false);
            RefreshToggles();
            if (pressStartText.TryGetComponent(out BlinkingText blink)) blink.Steady = flow.ReducedMotion;
        }

        // ------------------------------------------------------------------ buttons
        // (wired as persistent listeners by the scene builder)

        public void OnStartClicked() => flow.StartMission();

        public void OnSoundClicked() => audioService.ToggleMute();

        public void OnAssistedClicked() => flow.ToggleAssistedMode();

        public void OnCreditsClicked()
        {
            creditsPanel.SetActive(true);
            UiSelection.Select(creditsBackButton.Button);
        }

        public void OnCreditsBackClicked()
        {
            creditsPanel.SetActive(false);
            UiSelection.Select(creditsButton.Button);
        }

        // ------------------------------------------------------------------ toggles

        private void OnAssistedModeChanged(bool _) => RefreshToggles();

        private void OnMuteChanged(bool _) => RefreshToggles();

        private void RefreshToggles()
        {
            soundButton.Label = audioService.Muted ? content.soundOffLabel : content.soundOnLabel;
            assistedButton.Label = flow.AssistedMode ? content.assistedOnLabel : content.assistedOffLabel;
        }
    }
}
