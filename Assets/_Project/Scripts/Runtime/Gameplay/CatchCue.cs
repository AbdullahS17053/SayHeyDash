namespace SayHeyDash.Gameplay
{
    /// <summary>On-screen timing cue during Stage B.</summary>
    public enum CatchCue
    {
        /// <summary>Ball still far away - presses are ignored.</summary>
        None = 0,
        /// <summary>Presses count from now on, but it is still too early ("GET READY...").</summary>
        Ready = 1,
        /// <summary>Inside the timing window ("NOW!").</summary>
        Now = 2,
    }
}
