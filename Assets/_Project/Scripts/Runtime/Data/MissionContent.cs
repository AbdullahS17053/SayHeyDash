using SayHeyDash.Scoring;
using UnityEngine;

namespace SayHeyDash.Data
{
    /// <summary>
    /// Every on-screen string, in one editable asset (GDD: text adjustable without code changes).
    /// Names and historical copy are PLACEHOLDERS pending client approval (Docs/01 Q1, AC-11);
    /// for the generic variant, edit the player/level lines here.
    /// </summary>
    [CreateAssetMenu(menuName = "Say Hey Dash/Mission Content", fileName = "MissionContent")]
    public sealed class MissionContent : ScriptableObject
    {
        [Header("01 Title")]
        public string presentsLine = "FASTBALL FUEL COFFEE COMPANY PRESENTS";
        public string arcadeModeLine = "[ARCADE MODE]";
        public string gameTitle = "THE CATCH";
        public string playerLine = "PLAYER ONE: WILLIE MAYS";
        public string seriesLine = "WORLD SERIES: 1954";
        public string levelLine = "LEVEL 01: POLO GROUNDS";
        public string pressStart = "PRESS START";
        public string startButton = "START";
        public string assistedOnLabel = "ASSISTED MODE: ON";
        public string assistedOffLabel = "ASSISTED MODE: OFF";

        [Header("02 Mission objective")]
        public string briefingHeader = "MISSION OBJECTIVE";
        [TextArea(3, 8)]
        public string briefingFacts = "YEAR: 1954\nLOCATION: POLO GROUNDS\nINNING: TOP 8\nSCORE: 2-2\nOUTS: 0";
        public string objectiveLine = "OBJECTIVE: MAKE THE CATCH. MAKE THE THROW.";
        [TextArea(2, 6)]
        public string historicalSetup = "RUNNERS ON FIRST AND SECOND. A DEEP FLY BALL HEADS FOR CENTER FIELD...";
        [TextArea(2, 6)]
        public string controlsHelp = "MOVE: ARROWS / WASD / STICK    CATCH + THROW: SPACE / (A)    PAUSE: ESC";
        public string continueButton = "CONTINUE";

        [Header("03 Gameplay prompts")]
        public string trackPrompt = "TRACK THE BALL! GET INSIDE THE RING";
        public string readyPrompt = "GET READY...";
        public string nowPrompt = "NOW! [SPACE]";
        public string inZoneLabel = "IN ZONE [OK]";
        public string outOfZoneLabel = "OUT OF ZONE [X]";
        public string missPrompt = "MISSED!";
        public string throwPrompt = "SPIN AND THROW! PRESS SPACE IN THE ZONE";
        public string throwTargetLabel = "TARGET: INFIELD CUTOFF";
        public string pauseButton = "II";

        [Header("04 Catch sequence")]
        public string catchSequenceHeadline = "OVER THE SHOULDER!";
        public string skipHint = "PRESS SPACE TO SKIP";

        [Header("Pause")]
        public string pausedTitle = "PAUSED";
        public string resumeButton = "RESUME";
        public string restartButton = "RESTART";

        [Header("05 Mission failed (retry only - never an alternative history)")]
        public string tryAgainTitle = "TRY AGAIN";
        public string ballMissedLine = "BALL MISSED";
        public string throwMissedLine = "THROW MISSED";
        public string retryPrompt = "RETRY MISSION?";
        public string retryButton = "RETRY";
        public string hintTooEarly = "HINT: WAIT FOR \"NOW!\" BEFORE YOU PRESS.";
        public string hintTooLate = "HINT: PRESS AS THE BALL DROPS INTO YOUR GLOVE.";
        public string hintOutOfZone = "HINT: FOLLOW THE SHADOW - BE INSIDE THE RING BEFORE YOU PRESS.";
        public string hintThrow = "HINT: PRESS WHILE THE NEEDLE IS BETWEEN THE NOTCHES.";

        [Header("06 Mission complete")]
        public string missionCompleteTitle = "MISSION COMPLETE";
        public string missionCompleteSubtitle = "THE CATCH\nSEPTEMBER 29, 1954";
        public string runnerOutcome = "LEAD RUNNER HOLDS AT THIRD - NO RUN SCORES";
        public string positioningLabel = "POSITION";
        public string timingLabel = "TIMING";
        public string throwLabel = "THROW";
        public string scoreLabel = "SCORE";
        public string rankRookie = "ROOKIE";
        public string rankAllStar = "ALL-STAR";
        public string rankLegendary = "LEGENDARY";
        public string replayMissionButton = "REPLAY";
        public string replayModeButton = "REPLAY MODE";

        [Header("07 Replay / history (full content in Milestone 4)")]
        public string replayHeader = "THE REAL CATCH";
        [TextArea(3, 10)]
        public string historicalRecap = "POLO GROUNDS, NEW YORK\nSEPTEMBER 29, 1954\n\n[CLIENT-APPROVED RECAP TEXT - MILESTONE 4]";
        public string museumButton = "MUSEUM / STORY MODE";
        [Tooltip("Pending client answer Q5. Empty = button hidden.")]
        public string museumUrl = "";
        public string backToTitleButton = "BACK TO TITLE";

        /// <summary>Display text for a rank, with a non-colour star marker (accessibility).</summary>
        public string RankText(Rank rank)
        {
            switch (rank)
            {
                case Rank.Rookie: return "* " + rankRookie;
                case Rank.AllStar: return "** " + rankAllStar;
                case Rank.Legendary: return "*** " + rankLegendary;
                default: return string.Empty;
            }
        }
    }
}
