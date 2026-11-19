using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 05 - Mission Failed: TRY AGAIN, BALL MISSED / THROW MISSED, a hint that matches
    /// the mistake, RETRY MISSION? and the RETRY button. A failed attempt is only ever a
    /// retry - never shown as the historical outcome (GDD).
    /// </summary>
    public sealed class MissionFailedView : ScreenView
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text reasonText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text promptText;
        [SerializeField] private PixelButton retryButton;

        protected override bool Validate() =>
            titleText != null && reasonText != null && hintText != null && promptText != null && retryButton != null;

        protected override bool IsVisibleIn(GameState state) => state == GameState.MissionFailed;

        protected override void ApplyContent()
        {
            SetText(titleText, content.tryAgainTitle);
            SetText(promptText, content.retryPrompt);
            retryButton.Label = content.retryButton;
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            bool ball = flow.LastFailReason == FailReason.BallMissed;
            reasonText.text = ball ? content.ballMissedLine : content.throwMissedLine;
            hintText.text = !ball ? content.hintThrow :
                flow.LastCatchOutcome == CatchOutcome.TooEarly ? content.hintTooEarly :
                flow.LastCatchOutcome == CatchOutcome.OutOfZone ? content.hintOutOfZone : content.hintTooLate;
        }

        public void OnRetryClicked() => flow.Retry();
    }
}
