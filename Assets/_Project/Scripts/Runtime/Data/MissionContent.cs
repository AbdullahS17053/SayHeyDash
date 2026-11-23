using System;
using SayHeyDash.Scoring;
using UnityEngine;

namespace SayHeyDash.Data
{
    /// <summary>
    /// Every on-screen string, in one editable asset (GDD: text adjustable without code changes).
    /// Names and historical copy are DRAFTS pending client approval (Docs/01 Q1, AC-11);
    /// for the generic variant, edit the player/level lines and the replay beats here.
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
        public string soundOnLabel = "SOUND: ON";
        public string soundOffLabel = "SOUND: OFF";
        public string creditsButton = "CREDITS";
        public string settingsButton = "SETTINGS";
        public string creditsTitle = "CREDITS";
        [TextArea(4, 14)]
        public string creditsText =
            "THE SAY HEY DASH\nA FASTBALL FUEL COFFEE COMPANY ARCADE GAME\n\n" +
            "GAME DESIGN, CODE, PIXEL ART AND SOUND:\n[DEVELOPER CREDIT - TO CONFIRM]\n\n" +
            "TYPEFACE: SAY HEY PIXEL (ORIGINAL)\nMADE WITH UNITY\n\n" +
            "INSPIRED BY GAME 1 OF THE 1954 WORLD SERIES.\nALL ARTWORK, MUSIC AND SOUND ARE ORIGINAL.";
        public string closeButton = "BACK";

        [Header("02 Mission objective")]
        public string briefingHeader = "MISSION OBJECTIVE";
        [TextArea(3, 8)]
        public string briefingFacts = "YEAR: 1954\nLOCATION: POLO GROUNDS\nINNING: TOP 8\nSCORE: 2-2\nOUTS: 0";
        public string objectiveLine = "OBJECTIVE: MAKE THE CATCH. MAKE THE THROW.";
        [TextArea(2, 6)]
        public string historicalSetup = "RUNNERS ON FIRST AND SECOND. A DEEP FLY BALL HEADS FOR CENTER FIELD...";
        [TextArea(2, 6)]
        public string controlsHelp = "MOVE: ARROWS / WASD / STICK\nCATCH + THROW: SPACE / (A)\nPAUSE: ESC / II BUTTON";
        [Tooltip("Shown instead of controlsHelp on touch devices.")]
        [TextArea(2, 6)]
        public string controlsHelpTouch = "MOVE: DRAG THE STICK (LEFT)\nCATCH + THROW: TAP THE (A) BUTTON\nPAUSE: II BUTTON";
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
        [Tooltip("Game situation chip in the gameplay HUD.")]
        public string hudSituation = "TOP 8   2-2   0 OUT";
        public string throwMeterLabel = "THROW";
        [Tooltip("Shown after the throw. Words, not just colour (accessibility).")]
        public string throwPerfectLabel = "PERFECT THROW!";
        public string throwGoodLabel = "GOOD THROW!";
        public string throwWideLabel = "WIDE!";
        public string throwTooSlowLabel = "TOO SLOW!";
        [Range(0f, 1f)] public float throwPerfectQuality = 0.8f;
        [Tooltip("Word under the on-screen action button (touch devices).")]
        public string touchCatchLabel = "CATCH";
        public string touchThrowLabel = "THROW";

        [Header("04 Catch sequence")]
        public string catchSequenceHeadline = "OVER THE SHOULDER!";
        public string skipHint = "PRESS SPACE TO SKIP";

        [Header("Pause")]
        public string pausedTitle = "PAUSED";
        public string resumeButton = "RESUME";
        public string restartButton = "RESTART";

        [Header("Settings menu (from the title screen and the pause menu)")]
        public string settingsTitle = "SETTINGS";
        [Tooltip("{0} = 0-100")]
        public string musicVolumeFormat = "MUSIC: {0}%";
        [Tooltip("{0} = 0-100")]
        public string effectsVolumeFormat = "EFFECTS: {0}%";
        public string reducedMotionOnLabel = "REDUCED MOTION: ON";
        public string reducedMotionOffLabel = "REDUCED MOTION: OFF";
        public string settingsSavedNote = "SETTINGS ARE SAVED ON THIS DEVICE.";
        public string settingsSessionNote = "THIS BROWSER BLOCKS SAVING.\nSETTINGS LAST FOR THIS VISIT.";
        public string assistedLockedNote = "ASSISTED MODE CHANGES\nBETWEEN ATTEMPTS.";

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
        public string rankLabel = "RANK";
        public string rankRookie = "ROOKIE";
        public string rankAllStar = "ALL-STAR";
        public string rankLegendary = "LEGENDARY";
        public string replayMissionButton = "REPLAY";
        public string replayModeButton = "REPLAY MODE";
        public string bestScoreLabel = "BEST";
        public string newBestLabel = "NEW BEST!";
        [Tooltip("{0} = next rank name, {1} = points needed for it.")]
        public string nextRankFormat = "{0} AT {1}";
        public string topRankLabel = "TOP RANK!";

        [Header("07 Replay / history (DRAFT copy - needs client approval, AC-11)")]
        public string replayHeader = "THE REAL CATCH";
        [Tooltip("One entry per step of the animated replay, in order: the setting, the drive, the catch, " +
                 "the throw, the result, why it matters. Seconds = how long the step plays (the last one holds).")]
        public ReplayBeat[] replayBeats =
        {
            new ReplayBeat
            {
                title = "THE SETTING", seconds = 3.5f,
                narration = "SEPTEMBER 29, 1954. GAME 1 OF THE WORLD SERIES AT THE POLO GROUNDS, NEW YORK.\n\n" +
                            "TOP OF THE 8TH, TIED 2-2. CLEVELAND HAS RUNNERS ON FIRST AND SECOND. NOBODY OUT.",
            },
            new ReplayBeat
            {
                title = "THE DRIVE", seconds = 3.5f,
                narration = "VIC WERTZ HITS A HUGE FLY BALL TO DEEP CENTER FIELD.\n\n" +
                            "WILLIE MAYS TURNS HIS BACK TO HOME PLATE AND SPRINTS TOWARD THE WALL.",
            },
            new ReplayBeat
            {
                title = "THE CATCH", seconds = 3f,
                narration = "AT FULL SPEED, FAR FROM HOME PLATE, MAYS CATCHES THE BALL OVER HIS SHOULDER.\n\n" +
                            "WERTZ IS OUT. THE RUNNERS HURRY BACK TO TAG UP.",
            },
            new ReplayBeat
            {
                title = "THE THROW", seconds = 2.5f,
                narration = "MAYS SPINS AND FIRES THE BALL BACK TO THE INFIELD BEFORE THE RUNNERS CAN GO FAR.",
            },
            new ReplayBeat
            {
                title = "THE RESULT", seconds = 3.5f,
                narration = "LARRY DOBY TAGS UP AND REACHES THIRD - BUT HE DOES NOT SCORE. THE RUNNER ON FIRST " +
                            "GOES BACK.\n\nTHE GAME STAYS TIED.",
            },
            new ReplayBeat
            {
                title = "WHY IT MATTERS", seconds = 0f,
                narration = "NEW YORK KEPT CLEVELAND FROM TAKING THE LEAD, WON GAME 1 IN THE 10TH INNING AND " +
                            "WENT ON TO SWEEP THE SERIES.\n\nTODAY THE PLAY IS SIMPLY CALLED \"THE CATCH\".",
            },
        };
        [Tooltip("Under the replay diagram. {0} = step number, {1} = number of steps, {2} = step title.")]
        public string replayStepFormat = "{0}/{1}  {2}";
        [Tooltip("Legend under the replay diagram: round marker = the center fielder, diamond = runners.")]
        public string replayLegendFielder = "MAYS";
        public string replayLegendRunners = "RUNNERS";
        public string watchAgainButton = "WATCH AGAIN";
        public string nextStepButton = "NEXT >";
        public string museumButton = "MUSEUM / STORY MODE";
        [Tooltip("Pending client answer Q5. Empty = button hidden.")]
        public string museumUrl = "";
        public string backToTitleButton = "BACK TO TITLE";

        /// <summary>One step of the Screen 07 replay (title under the diagram, narration beside it).</summary>
        [Serializable]
        public struct ReplayBeat
        {
            public string title;
            [TextArea(3, 8)] public string narration;
            [Min(0f)] public float seconds;
        }

        public string RankName(Rank rank)
        {
            switch (rank)
            {
                case Rank.Rookie: return rankRookie;
                case Rank.AllStar: return rankAllStar;
                case Rank.Legendary: return rankLegendary;
                default: return string.Empty;
            }
        }

        /// <summary>Text after the throw: PERFECT / GOOD / WIDE / TOO SLOW.</summary>
        public string ThrowFeedback(bool success, bool timedOut, float quality)
        {
            if (timedOut) return throwTooSlowLabel;
            if (!success) return throwWideLabel;
            return quality >= throwPerfectQuality ? throwPerfectLabel : throwGoodLabel;
        }

        /// <summary>Display text for a rank, with a non-colour star marker (accessibility).
        /// '*' is drawn as a star by the Say Hey Pixel font.</summary>
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
