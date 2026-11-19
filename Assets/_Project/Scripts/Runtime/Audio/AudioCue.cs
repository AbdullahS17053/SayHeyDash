namespace SayHeyDash.Audio
{
    /// <summary>Every sound the game can play. Clips are assigned in the <see cref="AudioCueLibrary"/> asset.</summary>
    public enum AudioCue
    {
        None = 0,
        UiMove = 1,
        UiConfirm = 2,
        UiBack = 3,
        BatCrack = 10,
        CrowdLoop = 11,
        CrowdCheer = 12,
        CatchReady = 13,
        CatchNow = 14,
        Catch = 15,
        Whiff = 16,
        Throw = 17,
        SuccessJingle = 20,
        FailJingle = 21,
        TitleMusic = 30,
    }
}
