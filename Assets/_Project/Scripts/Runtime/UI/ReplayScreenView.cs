using SayHeyDash.Flow;
using SayHeyDash.Platform;
using SayHeyDash.Replay;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 07 - Replay Mode / Historical Context: a simplified animated replay of the real play
    /// on a field diagram (<see cref="ReplayDirector"/>) with a short narration per step, then why
    /// the catch matters. NEXT skips ahead, WATCH AGAIN restarts, the museum / STORY MODE button
    /// appears once its URL (Docs/01 Q5) is set in MissionContent, BACK TO TITLE returns.
    /// All copy is a draft for client approval (AC-11) and lives in MissionContent.
    /// </summary>
    public sealed class ReplayScreenView : ScreenView
    {
        [SerializeField] private ReplayDirector director;
        [SerializeField] private Text headerText;
        [SerializeField] private Text stepText;
        [SerializeField] private Text narrationText;
        [SerializeField] private Text legendFielderText;
        [SerializeField] private Text legendRunnerText;
        [SerializeField] private PixelButton watchAgainButton;
        [SerializeField] private PixelButton nextButton;
        [SerializeField] private PixelButton museumButton;
        [SerializeField] private PixelButton backButton;

        protected override bool Validate() =>
            director != null && headerText != null && stepText != null && narrationText != null &&
            legendFielderText != null && legendRunnerText != null &&
            watchAgainButton != null && nextButton != null && museumButton != null && backButton != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!enabled) return;
            director.StepChanged += OnStepChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (director != null) director.StepChanged -= OnStepChanged;
        }

        protected override bool IsVisibleIn(GameState state) => state == GameState.Replay;

        protected override void ApplyContent()
        {
            SetText(headerText, content.replayHeader);
            SetText(legendFielderText, content.replayLegendFielder);
            SetText(legendRunnerText, content.replayLegendRunners);
            watchAgainButton.Label = content.watchAgainButton;
            nextButton.Label = content.nextStepButton;
            museumButton.Label = content.museumButton;
            backButton.Label = content.backToTitleButton;
            museumButton.gameObject.SetActive(!string.IsNullOrEmpty(content.museumUrl));
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            if (justShown) director.Play();
        }

        protected override void OnHidden() => director.Stop();

        // ------------------------------------------------------------------ buttons

        public void OnWatchAgainClicked()
        {
            director.Play();
            UiSelection.Select(nextButton.Button);
        }

        public void OnNextClicked() => director.Next();

        public void OnMuseumClicked()
        {
            if (!string.IsNullOrEmpty(content.museumUrl)) BrowserBridge.OpenUrl(content.museumUrl);
        }

        public void OnBackClicked() => flow.BackToTitle();

        // ------------------------------------------------------------------ steps

        private void OnStepChanged(int step)
        {
            var beat = director.Beat(step);
            SetText(stepText, string.Format(content.replayStepFormat, step + 1, ReplayTimeline.StepCount, beat.title));
            SetText(narrationText, beat.narration);
            bool last = director.IsAtLastStep;
            bool nextHadFocus = nextButton.IsSelected;
            nextButton.Button.interactable = !last;
            // A disabled button cannot keep keyboard focus: hand it to BACK TO TITLE.
            if (last && nextHadFocus) UiSelection.Select(backButton.Button);
        }
    }
}
