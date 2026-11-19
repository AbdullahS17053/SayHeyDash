using SayHeyDash.Flow;
using SayHeyDash.Scoring;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 06 - Mission Complete: title and date, the score breakdown counting up, the
    /// final score, the rank as a badge + stars + word (never colour alone), the documented
    /// runner outcome, and REPLAY / REPLAY MODE. Reduced motion shows the numbers at once.
    /// </summary>
    public sealed class MissionCompleteView : ScreenView
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text labelsText;
        [SerializeField] private Text valuesText;
        [SerializeField] private Text totalLabelText;
        [SerializeField] private Text totalValueText;
        [SerializeField] private Text rankText;
        [SerializeField] private Image rankBadge;
        [SerializeField] private Sprite rookieBadge;
        [SerializeField] private Sprite allStarBadge;
        [SerializeField] private Sprite legendaryBadge;
        [SerializeField] private Text outcomeText;
        [SerializeField] private PixelButton replayButton;
        [SerializeField] private PixelButton replayModeButton;
        [Min(0f)] [SerializeField] private float tallySeconds = 1.2f;

        private ScoreBreakdown _score;
        private float _tallyStart = -1f;

        /// <summary>True once the numbers on screen are the final ones.</summary>
        public bool TallyFinished => _tallyStart < 0f;

        protected override bool Validate() =>
            titleText != null && subtitleText != null && labelsText != null && valuesText != null &&
            totalLabelText != null && totalValueText != null && rankText != null && rankBadge != null &&
            rookieBadge != null && allStarBadge != null && legendaryBadge != null && outcomeText != null &&
            replayButton != null && replayModeButton != null;

        protected override bool IsVisibleIn(GameState state) => state == GameState.MissionComplete;

        protected override void ApplyContent()
        {
            SetText(titleText, content.missionCompleteTitle);
            SetText(subtitleText, content.missionCompleteSubtitle);
            SetText(labelsText, $"{content.positioningLabel}\n{content.timingLabel}\n{content.throwLabel}");
            SetText(totalLabelText, content.scoreLabel);
            SetText(outcomeText, content.runnerOutcome);
            replayButton.Label = content.replayMissionButton;
            replayModeButton.Label = content.replayModeButton;
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            if (!justShown) return;
            _score = flow.LastScore;
            rankText.text = content.RankText(_score.Rank);
            rankBadge.sprite = _score.Rank == Rank.Legendary ? legendaryBadge :
                _score.Rank == Rank.AllStar ? allStarBadge : rookieBadge;
            rankBadge.enabled = _score.Rank != Rank.None;
            if (flow.ReducedMotion || tallySeconds <= 0f) ShowNumbers(1f);
            else
            {
                _tallyStart = Time.unscaledTime;
                ShowNumbers(0f);
            }
        }

        protected override void OnHidden() => _tallyStart = -1f;

        private void Update()
        {
            if (_tallyStart < 0f || !IsShown) return;
            float t = Mathf.Clamp01((Time.unscaledTime - _tallyStart) / tallySeconds);
            ShowNumbers(t);
            if (t >= 1f) _tallyStart = -1f;
        }

        private void ShowNumbers(float t)
        {
            valuesText.text = $"{Count(_score.Positioning, t)}\n{Count(_score.Timing, t)}\n{Count(_score.Throw, t)}";
            totalValueText.text = Count(_score.Total, t).ToString();
            bool done = t >= 1f;
            rankText.enabled = done;
            rankBadge.gameObject.SetActive(done && _score.Rank != Rank.None);
        }

        private static int Count(int value, float t) => Mathf.RoundToInt(value * t);

        public void OnReplayClicked() => flow.ReplayMission();

        public void OnReplayModeClicked() => flow.ShowReplayMode();
    }
}
