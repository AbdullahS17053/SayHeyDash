using SayHeyDash.Flow;
using SayHeyDash.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Screen 07 - Replay Mode / Historical Context. Styled in Milestone 3; the client-approved
    /// recap and the animated replay arrive in Milestone 4. The museum button is hidden until
    /// its URL (Docs/01 Q5) is set in MissionContent.
    /// </summary>
    public sealed class ReplayScreenView : ScreenView
    {
        [SerializeField] private Text headerText;
        [SerializeField] private Text recapText;
        [SerializeField] private PixelButton museumButton;
        [SerializeField] private PixelButton backButton;

        protected override bool Validate() =>
            headerText != null && recapText != null && museumButton != null && backButton != null;

        protected override bool IsVisibleIn(GameState state) => state == GameState.Replay;

        protected override void ApplyContent()
        {
            SetText(headerText, content.replayHeader);
            SetText(recapText, content.historicalRecap);
            museumButton.Label = content.museumButton;
            backButton.Label = content.backToTitleButton;
            bool hasMuseum = !string.IsNullOrEmpty(content.museumUrl);
            museumButton.gameObject.SetActive(hasMuseum);
            if (!hasMuseum && backButton.transform is RectTransform rt)
                rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y); // centre the only button
        }

        public void OnMuseumClicked()
        {
            if (!string.IsNullOrEmpty(content.museumUrl)) BrowserBridge.OpenUrl(content.museumUrl);
        }

        public void OnBackClicked() => flow.BackToTitle();
    }
}
