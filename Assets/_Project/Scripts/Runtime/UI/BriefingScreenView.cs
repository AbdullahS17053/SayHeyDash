using SayHeyDash.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 02 - Mission Objective: the game situation on a scoreboard board, the objective,
    /// a short historical setup and the controls. CONTINUE skips the intro (GDD: skippable).
    /// Touch devices see touch instructions instead of keyboard ones.
    /// </summary>
    public sealed class BriefingScreenView : ScreenView
    {
        [SerializeField] private Text headerText;
        [SerializeField] private Text factsText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text setupText;
        [SerializeField] private Text controlsText;
        [SerializeField] private PixelButton continueButton;
        [SerializeField] private TouchControls touchControls;

        protected override bool Validate() =>
            headerText != null && factsText != null && objectiveText != null && setupText != null &&
            controlsText != null && continueButton != null && touchControls != null;

        protected override bool IsVisibleIn(GameState state) => state == GameState.MissionBriefing;

        protected override void ApplyContent()
        {
            SetText(headerText, content.briefingHeader);
            SetText(factsText, content.briefingFacts);
            SetText(objectiveText, content.objectiveLine);
            SetText(setupText, content.historicalSetup);
            continueButton.Label = content.continueButton;
        }

        protected override void OnStateEntered(GameState state, bool justShown)
        {
            if (justShown)
                SetText(controlsText, touchControls.TouchMode
                    ? content.controlsHelpTouch
                    : content.controlsHelp);
        }

        public void OnContinueClicked() => flow.BeginMission();
    }
}
