namespace SayHeyDash.Flow
{
    /// <summary>Pure transition table for <see cref="GameFlowController"/> (unit-tested).</summary>
    public static class GameFlowRules
    {
        /// <summary>States in which the field is live and the game can be paused.</summary>
        public static bool IsGameplay(GameState s) =>
            s == GameState.TrackBall || s == GameState.CatchWindow ||
            s == GameState.CatchSequence || s == GameState.Throw;

        public static bool CanPause(GameState s) => IsGameplay(s);

        public static bool IsAllowed(GameState from, GameState to)
        {
            // Restart from the pause menu: any live state back to the start of the attempt.
            if (to == GameState.TrackBall && IsGameplay(from)) return true;

            switch (from)
            {
                case GameState.None: return to == GameState.Title;
                case GameState.Title: return to == GameState.MissionBriefing;
                case GameState.MissionBriefing: return to == GameState.TrackBall;
                case GameState.TrackBall: return to == GameState.CatchWindow || to == GameState.MissionFailed;
                case GameState.CatchWindow: return to == GameState.CatchSequence || to == GameState.MissionFailed;
                case GameState.CatchSequence: return to == GameState.Throw;
                case GameState.Throw: return to == GameState.MissionComplete || to == GameState.MissionFailed;
                case GameState.MissionFailed: return to == GameState.TrackBall;
                case GameState.MissionComplete: return to == GameState.TrackBall || to == GameState.Replay;
                case GameState.Replay: return to == GameState.Title;
                default: return false;
            }
        }
    }
}
