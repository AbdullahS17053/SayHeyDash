using SayHeyDash.Data;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using SayHeyDash.Platform;
using SayHeyDash.Scoring;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// MILESTONE 2 PLACEHOLDER UI. One view that shows/hides a plain panel per state and
    /// fills it from <see cref="MissionContent"/>. It contains no game logic: it listens to
    /// flow/mechanic events and its buttons call <see cref="GameFlowController"/> commands.
    /// Replaced by one styled view per screen in Milestone 3.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaceholderFlowView : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private CatchSystem catchSystem;
        [SerializeField] private MissionContent content;

        [Header("Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject briefingPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject failedPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private GameObject replayPanel;
        [SerializeField] private ThrowMeterView throwMeter;

        [Header("01 Title")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text pressStartText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button assistedButton;

        [Header("02 Briefing")]
        [SerializeField] private Text briefingText;
        [SerializeField] private Button continueButton;

        [Header("03/04 HUD")]
        [SerializeField] private Text promptText;
        [SerializeField] private Text subText;
        [SerializeField] private Text modeText;
        [SerializeField] private Button pauseButton;

        [Header("Pause")]
        [SerializeField] private Text pauseTitleText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;

        [Header("05 Failed")]
        [SerializeField] private Text failedText;
        [SerializeField] private Button retryButton;

        [Header("06 Complete")]
        [SerializeField] private Text completeText;
        [SerializeField] private Button replayMissionButton;
        [SerializeField] private Button replayModeButton;

        [Header("07 Replay")]
        [SerializeField] private Text replayText;
        [SerializeField] private Button museumButton;
        [SerializeField] private Button backToTitleButton;

        private void Awake()
        {
            if (flow == null || catchSystem == null || content == null || throwMeter == null)
            {
                Debug.LogError($"[{nameof(PlaceholderFlowView)}] Missing flow / catch system / content / meter.", this);
                enabled = false;
                return;
            }
            ApplyStaticContent();
        }

        private void OnEnable()
        {
            flow.StateChanged += OnStateChanged;
            flow.PauseChanged += OnPauseChanged;
            flow.AssistedModeChanged += OnAssistedModeChanged;
            catchSystem.CueChanged += OnCueChanged;
            catchSystem.InZoneChanged += OnInZoneChanged;
            ShowState(flow.State);
        }

        private void OnDisable()
        {
            flow.StateChanged -= OnStateChanged;
            flow.PauseChanged -= OnPauseChanged;
            flow.AssistedModeChanged -= OnAssistedModeChanged;
            catchSystem.CueChanged -= OnCueChanged;
            catchSystem.InZoneChanged -= OnInZoneChanged;
        }

        // ------------------------------------------------------------------ button hooks
        // (wired as persistent listeners by the scene builder)

        public void OnStartClicked() => flow.StartMission();
        public void OnAssistedClicked() => flow.ToggleAssistedMode();
        public void OnContinueClicked() => flow.BeginMission();
        public void OnPauseClicked() => flow.TogglePause();
        public void OnResumeClicked() => flow.Resume();
        public void OnRestartClicked() => flow.RestartAttempt();
        public void OnRetryClicked() => flow.Retry();
        public void OnReplayMissionClicked() => flow.ReplayMission();
        public void OnReplayModeClicked() => flow.ShowReplayMode();
        public void OnBackToTitleClicked() => flow.BackToTitle();
        public void OnMuseumClicked()
        {
            if (!string.IsNullOrEmpty(content.museumUrl)) BrowserBridge.OpenUrl(content.museumUrl);
        }

        // ------------------------------------------------------------------ events

        private void OnStateChanged(GameState previous, GameState next) => ShowState(next);

        private void OnPauseChanged(bool paused)
        {
            pausePanel.SetActive(paused);
            Select(paused ? resumeButton : null);
        }

        private void OnAssistedModeChanged(bool _) => RefreshModeLabels();

        private void OnCueChanged(CatchCue cue)
        {
            if (flow.State != GameState.TrackBall && flow.State != GameState.CatchWindow) return;
            switch (cue)
            {
                case CatchCue.Ready: promptText.text = content.readyPrompt; break;
                case CatchCue.Now: promptText.text = content.nowPrompt; break;
                default: promptText.text = content.trackPrompt; break;
            }
        }

        private void OnInZoneChanged(bool inZone)
        {
            if (flow.State == GameState.TrackBall || flow.State == GameState.CatchWindow)
                subText.text = inZone ? content.inZoneLabel : content.outOfZoneLabel;
        }

        // ------------------------------------------------------------------ rendering

        private void ShowState(GameState state)
        {
            bool gameplay = GameFlowRules.IsGameplay(state);
            titlePanel.SetActive(state == GameState.Title);
            briefingPanel.SetActive(state == GameState.MissionBriefing);
            hudPanel.SetActive(gameplay);
            pausePanel.SetActive(flow.IsPaused);
            failedPanel.SetActive(state == GameState.MissionFailed);
            completePanel.SetActive(state == GameState.MissionComplete);
            replayPanel.SetActive(state == GameState.Replay);
            if (state == GameState.Throw) throwMeter.Show(); else throwMeter.Hide();

            switch (state)
            {
                case GameState.Title:
                    RefreshModeLabels();
                    Select(startButton);
                    break;
                case GameState.MissionBriefing:
                    Select(continueButton);
                    break;
                case GameState.TrackBall:
                    promptText.text = content.trackPrompt;
                    subText.text = catchSystem.InZone ? content.inZoneLabel : content.outOfZoneLabel;
                    Select(null);
                    break;
                case GameState.CatchWindow:
                    break; // cue events drive the prompt
                case GameState.CatchSequence:
                    promptText.text = content.catchSequenceHeadline;
                    subText.text = content.skipHint;
                    break;
                case GameState.Throw:
                    promptText.text = content.throwPrompt;
                    subText.text = content.throwTargetLabel;
                    break;
                case GameState.MissionFailed:
                    failedText.text = BuildFailedText();
                    Select(retryButton);
                    break;
                case GameState.MissionComplete:
                    completeText.text = BuildCompleteText(flow.LastScore);
                    Select(replayMissionButton);
                    break;
                case GameState.Replay:
                    Select(backToTitleButton);
                    break;
            }
        }

        private string BuildFailedText()
        {
            bool ball = flow.LastFailReason == FailReason.BallMissed;
            string hint = !ball ? content.hintThrow :
                flow.LastCatchOutcome == CatchOutcome.TooEarly ? content.hintTooEarly :
                flow.LastCatchOutcome == CatchOutcome.OutOfZone ? content.hintOutOfZone : content.hintTooLate;
            return $"{content.tryAgainTitle}\n\n{(ball ? content.ballMissedLine : content.throwMissedLine)}\n\n{hint}\n\n{content.retryPrompt}";
        }

        private string BuildCompleteText(ScoreBreakdown s)
        {
            return $"{content.missionCompleteTitle}\n{content.missionCompleteSubtitle}\n\n" +
                   $"{content.positioningLabel} {s.Positioning}   {content.timingLabel} {s.Timing}   {content.throwLabel} {s.Throw}\n" +
                   $"{content.scoreLabel} {s.Total}   {content.RankText(s.Rank)}\n\n{content.runnerOutcome}";
        }

        private void ApplyStaticContent()
        {
            titleText.text = $"{content.presentsLine}\n{content.arcadeModeLine}\n\n{content.gameTitle}\n\n" +
                             $"{content.playerLine}\n{content.seriesLine}\n{content.levelLine}";
            pressStartText.text = content.pressStart;
            SetLabel(startButton, content.startButton);

            briefingText.text = $"{content.briefingHeader}\n\n{content.briefingFacts}\n\n{content.objectiveLine}\n\n" +
                                $"{content.historicalSetup}\n\n{content.controlsHelp}";
            SetLabel(continueButton, content.continueButton);

            SetLabel(pauseButton, content.pauseButton);
            pauseTitleText.text = content.pausedTitle;
            SetLabel(resumeButton, content.resumeButton);
            SetLabel(restartButton, content.restartButton);

            SetLabel(retryButton, content.retryButton);
            SetLabel(replayMissionButton, content.replayMissionButton);
            SetLabel(replayModeButton, content.replayModeButton);

            replayText.text = $"{content.replayHeader}\n\n{content.historicalRecap}";
            SetLabel(museumButton, content.museumButton);
            museumButton.gameObject.SetActive(!string.IsNullOrEmpty(content.museumUrl));
            SetLabel(backToTitleButton, content.backToTitleButton);
            RefreshModeLabels();
        }

        private void RefreshModeLabels()
        {
            string label = flow.AssistedMode ? content.assistedOnLabel : content.assistedOffLabel;
            SetLabel(assistedButton, label);
            modeText.text = flow.AssistedMode ? label : string.Empty;
        }

        private static void SetLabel(Button button, string label)
        {
            if (button == null) return;
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
        }

        /// <summary>Keyboard/gamepad focus for menus; cleared in gameplay so Space never clicks a button.</summary>
        private static void Select(Selectable target)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;
            es.SetSelectedGameObject(target != null && target.gameObject.activeInHierarchy ? target.gameObject : null);
        }
    }
}
