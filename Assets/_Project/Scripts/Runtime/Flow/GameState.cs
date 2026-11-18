namespace SayHeyDash.Flow
{
    /// <summary>
    /// The game's states. Screen mapping (GDD): 01 Title, 02 MissionBriefing,
    /// 03 TrackBall / CatchWindow / Throw, 04 CatchSequence, 05 MissionFailed,
    /// 06 MissionComplete, 07 Replay. Pause is an overlay flag, not a state.
    /// </summary>
    public enum GameState
    {
        None = 0,
        Title = 1,
        MissionBriefing = 2,
        TrackBall = 3,
        CatchWindow = 4,
        CatchSequence = 5,
        Throw = 6,
        MissionFailed = 7,
        MissionComplete = 8,
        Replay = 9,
    }
}
