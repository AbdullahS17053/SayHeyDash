using System;
using System.Collections;
using SayHeyDash.Controls;
using SayHeyDash.Data;
using SayHeyDash.Gameplay;
using SayHeyDash.Scoring;
using UnityEngine;

namespace SayHeyDash.Flow
{
    /// <summary>
    /// Composition root and state machine for the single gameplay scene. It decides WHICH
    /// phase the game is in and routes the Action button to the active mechanic; the
    /// mechanics (<see cref="CatchSystem"/>, <see cref="ThrowSystem"/>) decide HOW they work
    /// and report back through events. Retrying resets objects in place - no scene reload -
    /// so the mission can be replayed indefinitely (AC-08).
    /// UI buttons call the public command methods; each one is a no-op in the wrong state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private GameInput input;
        [SerializeField] private OutfielderController outfielder;
        [SerializeField] private BallFlight ball;
        [SerializeField] private CatchSystem catchSystem;
        [SerializeField] private ThrowSystem throwSystem;

        [Header("Data")]
        [SerializeField] private GameplayTuning tuning;
        [SerializeField] private ScoringConfig scoring;
        [SerializeField] private DifficultyProfile defaultDifficulty;
        [SerializeField] private DifficultyProfile assistedDifficulty;

        [Header("Options (start-up values; the Settings menu changes them and SettingsService saves them)")]
        [Tooltip("Shortens the catch sequence (GDD: skippable or shortened in reduced-motion mode) and freezes " +
                 "decorative animation: crowd, flags, blinking prompt, score count-up, replay tweening.")]
        [SerializeField] private bool reducedMotion;
        [Tooltip("Log every state change to the console.")]
        [SerializeField] private bool logStateChanges;

        private int _inputBlockedFrame = -1;
        private bool _skipRequested;
        private CatchResult _lastCatch;
        private ThrowResult _lastThrow;

        /// <summary>(previous, next)</summary>
        public event Action<GameState, GameState> StateChanged;
        public event Action<bool> PauseChanged;
        public event Action<bool> AssistedModeChanged;
        public event Action<bool> ReducedMotionChanged;
        /// <summary>Raised with the final score just before MissionComplete is entered.</summary>
        public event Action<ScoreBreakdown> MissionCompleted;

        public GameState State { get; private set; } = GameState.None;
        public bool IsPaused { get; private set; }
        public bool AssistedMode { get; private set; }
        public bool ReducedMotion => reducedMotion;
        public DifficultyProfile ActiveDifficulty => AssistedMode ? assistedDifficulty : defaultDifficulty;
        public FailReason LastFailReason { get; private set; }
        public CatchOutcome LastCatchOutcome => _lastCatch.Outcome;
        public ThrowResult LastThrow => _lastThrow;
        public ScoreBreakdown LastScore { get; private set; }
        /// <summary>The scoring parameters in use (ScoringConfig asset).</summary>
        public ScoringRules ScoringRules => scoring != null ? scoring.ToRules() : Scoring.ScoringRules.Default;
        /// <summary>Attempts started this session (each retry / replay counts).</summary>
        public int AttemptCount { get; private set; }
        public int CompletedCount { get; private set; }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (input == null || outfielder == null || ball == null || catchSystem == null || throwSystem == null ||
                tuning == null || scoring == null || defaultDifficulty == null || assistedDifficulty == null)
            {
                Debug.LogError($"[{nameof(GameFlowController)}] Missing reference(s) - check the Inspector. " +
                               "Rebuild with Say Hey Dash > Build Main Scene.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            input.ActionPressed += OnActionPressed;
            input.PausePressed += OnPausePressed;
            catchSystem.CatchWindowOpened += OnCatchWindowOpened;
            catchSystem.CatchResolved += OnCatchResolved;
            throwSystem.ThrowResolved += OnThrowResolved;
        }

        private void OnDisable()
        {
            input.ActionPressed -= OnActionPressed;
            input.PausePressed -= OnPausePressed;
            catchSystem.CatchWindowOpened -= OnCatchWindowOpened;
            catchSystem.CatchResolved -= OnCatchResolved;
            throwSystem.ThrowResolved -= OnThrowResolved;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            ResetField();
            ChangeState(GameState.Title);
        }

        // ------------------------------------------------------------------ commands (UI buttons)

        /// <summary>01 Title: START.</summary>
        public void StartMission()
        {
            if (State == GameState.Title) ChangeState(GameState.MissionBriefing);
        }

        /// <summary>02 Mission objective: CONTINUE / skip intro.</summary>
        public void BeginMission()
        {
            if (State == GameState.MissionBriefing) StartAttempt();
        }

        /// <summary>05 Mission failed: RETRY.</summary>
        public void Retry()
        {
            if (State == GameState.MissionFailed) StartAttempt();
        }

        /// <summary>06 Mission complete: REPLAY (play the mission again).</summary>
        public void ReplayMission()
        {
            if (State == GameState.MissionComplete) StartAttempt();
        }

        /// <summary>06 Mission complete: continue to 07 Replay mode.</summary>
        public void ShowReplayMode()
        {
            if (State == GameState.MissionComplete) ChangeState(GameState.Replay);
        }

        /// <summary>07 Replay: back to 01 Title.</summary>
        public void BackToTitle()
        {
            if (State != GameState.Replay) return;
            ResetField();
            ChangeState(GameState.Title);
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (GameFlowRules.CanPause(State)) SetPaused(true);
        }

        public void Resume() => SetPaused(false);

        /// <summary>Pause menu: RESTART the current attempt.</summary>
        public void RestartAttempt()
        {
            if (GameFlowRules.IsGameplay(State)) StartAttempt();
        }

        public void ToggleAssistedMode() => SetAssistedMode(!AssistedMode);

        /// <summary>Assisted mode can only change outside an attempt (it applies to the next one).</summary>
        public bool CanChangeAssistedMode => !GameFlowRules.IsGameplay(State);

        public void ToggleReducedMotion() => SetReducedMotion(!reducedMotion);

        /// <summary>Takes effect immediately (decorative animation freezes / resumes).</summary>
        public void SetReducedMotion(bool value)
        {
            if (value == reducedMotion) return;
            reducedMotion = value;
            ReducedMotionChanged?.Invoke(value);
        }

        /// <summary>Takes effect from the next attempt; ignored mid-attempt.</summary>
        public void SetAssistedMode(bool value)
        {
            if (value == AssistedMode || GameFlowRules.IsGameplay(State)) return;
            AssistedMode = value;
            AssistedModeChanged?.Invoke(value);
        }

        // ------------------------------------------------------------------ test hooks

        internal void SimulateActionPress() => OnActionPressed();

        // ------------------------------------------------------------------ input

        private void OnActionPressed()
        {
            // One logical press per frame: Space fires Action and Submit (UI) together, and a
            // press that just changed state must not also act in the new state.
            if (IsPaused || Time.frameCount <= _inputBlockedFrame) return;

            bool consumed = false;
            switch (State)
            {
                case GameState.CatchWindow: consumed = catchSystem.TryCatch(); break;
                case GameState.CatchSequence: _skipRequested = true; consumed = true; break;
                case GameState.Throw: consumed = throwSystem.TryThrow(); break;
            }
            if (consumed) _inputBlockedFrame = Time.frameCount;
        }

        private void OnPausePressed()
        {
            if (GameFlowRules.CanPause(State)) TogglePause();
        }

        // ------------------------------------------------------------------ mechanics events

        private void OnCatchWindowOpened()
        {
            if (State == GameState.TrackBall) ChangeState(GameState.CatchWindow);
        }

        private void OnCatchResolved(CatchResult result)
        {
            _lastCatch = result;
            outfielder.SetControlEnabled(false);
            if (logStateChanges) Debug.Log($"[Flow] Catch: {result}");

            if (result.Success)
            {
                ball.HoldAt(outfielder.transform, outfielder.GlovePosition);
                ChangeState(GameState.CatchSequence);
                StartCoroutine(CatchSequenceRoutine());
            }
            else
            {
                if (State == GameState.TrackBall) ChangeState(GameState.CatchWindow);
                StartCoroutine(FailAfter(FailReason.BallMissed, tuning.missRevealSeconds));
            }
        }

        private void OnThrowResolved(ThrowResult result)
        {
            _lastThrow = result;
            if (logStateChanges) Debug.Log($"[Flow] Throw: {result}");
            // The ball leaves from the throwing hand, not from where it was caught.
            if (!result.TimedOut) ball.HoldAt(outfielder.transform, outfielder.ThrowReleasePosition);

            if (result.Success)
            {
                ball.Throw(tuning.throwTargetPoint, tuning.throwFlightSeconds);
                StartCoroutine(CompleteAfter(tuning.throwFlightSeconds));
            }
            else
            {
                if (!result.TimedOut)
                {
                    // Sails wide of the cut-off man: left or right of the target depending on the miss.
                    float side = result.NeedlePosition < throwSystem.ZoneCenter ? -1f : 1f;
                    ball.Throw(tuning.throwTargetPoint + new Vector2(7f * side, 2f), tuning.throwFlightSeconds);
                }
                StartCoroutine(FailAfter(FailReason.ThrowMissed, Mathf.Max(tuning.throwFlightSeconds, tuning.missRevealSeconds)));
            }
        }

        // ------------------------------------------------------------------ sequences

        private IEnumerator CatchSequenceRoutine()
        {
            _skipRequested = false;
            float duration = reducedMotion ? tuning.reducedMotionCatchSequenceSeconds : tuning.catchSequenceSeconds;
            for (float t = 0f; t < duration && !_skipRequested; t += Time.deltaTime)
                yield return null;

            outfielder.FaceTowards(tuning.throwTargetPoint);
            // Begin before announcing the state so listeners (the meter view) see the real zone.
            throwSystem.Begin(ActiveDifficulty);
            ChangeState(GameState.Throw);
        }

        private IEnumerator FailAfter(FailReason reason, float delay)
        {
            for (float t = 0f; t < delay; t += Time.deltaTime) yield return null;
            LastFailReason = reason;
            ChangeState(GameState.MissionFailed);
        }

        private IEnumerator CompleteAfter(float delay)
        {
            for (float t = 0f; t < delay; t += Time.deltaTime) yield return null;
            LastFailReason = FailReason.None;
            LastScore = ScoreCalculator.Calculate(scoring.ToRules(),
                _lastCatch.PositioningQuality, _lastCatch.TimingQuality, _lastThrow.Quality);
            CompletedCount++;
            if (logStateChanges) Debug.Log($"[Flow] Score: {LastScore}");
            MissionCompleted?.Invoke(LastScore);
            ChangeState(GameState.MissionComplete);
        }

        // ------------------------------------------------------------------ internals

        private void StartAttempt()
        {
            SetPaused(false);
            ResetField();
            AttemptCount++;
            LastFailReason = FailReason.None;
            outfielder.SetControlEnabled(true);
            ball.Launch();
            catchSystem.Begin(ActiveDifficulty);
            ChangeState(GameState.TrackBall);
        }

        private void ResetField()
        {
            StopAllCoroutines();
            _skipRequested = false;
            catchSystem.Cancel();
            throwSystem.Cancel();
            ball.ResetBall();
            outfielder.ResetToStart();
        }

        private void SetPaused(bool value)
        {
            if (IsPaused == value) return;
            IsPaused = value;
            Time.timeScale = value ? 0f : 1f;
            PauseChanged?.Invoke(value);
        }

        private void ChangeState(GameState next)
        {
            // Re-entering TrackBall is a restart and must still notify listeners.
            if (next == State && next != GameState.TrackBall) return;
            if (!GameFlowRules.IsAllowed(State, next))
            {
                Debug.LogError($"[{nameof(GameFlowController)}] Illegal transition {State} -> {next}.", this);
                return;
            }

            GameState previous = State;
            State = next;
            _inputBlockedFrame = Time.frameCount;
            if (logStateChanges) Debug.Log($"[Flow] {previous} -> {next}");
            StateChanged?.Invoke(previous, next);
        }
    }
}
