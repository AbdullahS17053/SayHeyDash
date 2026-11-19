using SayHeyDash.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 02 - Mission Objective: the game situation on a scoreboard board, the objective,
    /// a short historical setup and the controls. CONTINUE skips the intro (GDD: skippable).
    /// </summary>
    public sealed class BriefingScreenView : ScreenView
    {
        [SerializeField] private Text headerText;
        [SerializeField] private Text factsText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text setupText;
        [SerializeField] private Text controlsText;
        [SerializeField] private PixelButton continueButton;

        protected override bool Validate() =>
            headerText != null && factsText != null && objectiveText != null && setupText != null &&
            controlsText != null && continueButton != null;

        protected override bool IsVisibleIn(GameState state) => state == GameState.MissionBriefing;

        protected override void ApplyContent()
        {
            SetText(headerText, content.briefingHeader);
            SetText(factsText, content.briefingFacts);
            SetText(objectiveText, content.objectiveLine);
            SetText(setupText, content.historicalSetup);
            SetText(controlsText, content.controlsHelp);
            continueButton.Label = content.continueButton;
        }

        public void OnContinueClicked() => flow.BeginMission();
    }
}
