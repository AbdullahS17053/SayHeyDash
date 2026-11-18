namespace SayHeyDash.Gameplay
{
    /// <summary>How a catch attempt ended. Every non-<see cref="Caught"/> value shows BALL MISSED.</summary>
    public enum CatchOutcome
    {
        Caught = 0,
        TooEarly = 1,
        TooLate = 2,
        OutOfZone = 3,
    }
}
