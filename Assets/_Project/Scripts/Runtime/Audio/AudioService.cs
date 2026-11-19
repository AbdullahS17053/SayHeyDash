using System;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;

namespace SayHeyDash.Audio
{
    /// <summary>
    /// Plays the game's sounds in response to flow and mechanic events (event-driven, no
    /// polling). Three sources: one-shot SFX, music/jingles, and the crowd bed. Mute silences
    /// all three. Mute is per session in Milestone 3; Milestone 4's SettingsService persists it.
    /// UI controls call <see cref="Play"/> directly for menu sounds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioCueLibrary library;
        [SerializeField] private GameFlowController flow;
        [SerializeField] private CatchSystem catchSystem;
        [SerializeField] private ThrowSystem throwSystem;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;

        public event Action<bool> MuteChanged;

        public bool Muted { get; private set; }

        /// <summary>Last cue requested (tests / debugging).</summary>
        public AudioCue LastCue { get; private set; }
        public int PlayCount { get; private set; }
        public AudioCue CurrentMusic { get; private set; }
        public bool AmbiencePlaying => ambienceSource != null && ambienceSource.isPlaying;

        private void Awake()
        {
            if (library == null || flow == null || catchSystem == null || throwSystem == null ||
                sfxSource == null || musicSource == null || ambienceSource == null)
            {
                Debug.LogError($"[{nameof(AudioService)}] Missing reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            flow.StateChanged += OnStateChanged;
            flow.PauseChanged += OnPauseChanged;
            catchSystem.CueChanged += OnCueChanged;
            catchSystem.CatchResolved += OnCatchResolved;
            throwSystem.ThrowResolved += OnThrowResolved;
        }

        private void OnDisable()
        {
            flow.StateChanged -= OnStateChanged;
            flow.PauseChanged -= OnPauseChanged;
            catchSystem.CueChanged -= OnCueChanged;
            catchSystem.CatchResolved -= OnCatchResolved;
            throwSystem.ThrowResolved -= OnThrowResolved;
        }

        // ------------------------------------------------------------------ public API

        public void ToggleMute() => SetMuted(!Muted);

        public void SetMuted(bool value)
        {
            if (value == Muted) return;
            Muted = value;
            sfxSource.mute = value;
            musicSource.mute = value;
            ambienceSource.mute = value;
            MuteChanged?.Invoke(value);
        }

        /// <summary>One-shot sound effect.</summary>
        public void Play(AudioCue cue)
        {
            LastCue = cue;
            PlayCount++;
            if (isActiveAndEnabled && library.TryGet(cue, out AudioCueLibrary.Entry e))
                sfxSource.PlayOneShot(e.clip, e.volume);
        }

        // ------------------------------------------------------------------ events

        private void OnStateChanged(GameState previous, GameState next)
        {
            switch (next)
            {
                case GameState.Title:
                case GameState.Replay:
                    StopAmbience();
                    PlayMusic(AudioCue.TitleMusic, true);
                    break;
                case GameState.MissionBriefing:
                    PlayMusic(AudioCue.TitleMusic, true);
                    break;
                case GameState.TrackBall:
                    StopMusic();
                    StartAmbience();
                    Play(AudioCue.BatCrack);
                    break;
                case GameState.CatchSequence:
                    Play(AudioCue.Catch);
                    Play(AudioCue.CrowdCheer);
                    break;
                case GameState.MissionFailed:
                    PlayMusic(AudioCue.FailJingle, false);
                    break;
                case GameState.MissionComplete:
                    PlayMusic(AudioCue.SuccessJingle, false);
                    Play(AudioCue.CrowdCheer);
                    break;
            }
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused)
            {
                musicSource.Pause();
                ambienceSource.Pause();
            }
            else
            {
                musicSource.UnPause();
                ambienceSource.UnPause();
            }
        }

        private void OnCueChanged(CatchCue cue)
        {
            if (cue == CatchCue.Ready) Play(AudioCue.CatchReady);
            else if (cue == CatchCue.Now) Play(AudioCue.CatchNow);
        }

        private void OnCatchResolved(CatchResult result)
        {
            if (!result.Success) Play(AudioCue.Whiff);
        }

        private void OnThrowResolved(ThrowResult result)
        {
            if (!result.TimedOut) Play(AudioCue.Throw);
        }

        // ------------------------------------------------------------------ sources

        private void PlayMusic(AudioCue cue, bool loop)
        {
            if (loop && CurrentMusic == cue && musicSource.isPlaying) return;
            CurrentMusic = cue;
            LastCue = cue;
            PlayCount++;
            musicSource.Stop();
            if (!library.TryGet(cue, out AudioCueLibrary.Entry e)) return;
            musicSource.clip = e.clip;
            musicSource.volume = e.volume;
            musicSource.loop = loop;
            musicSource.Play();
        }

        private void StopMusic()
        {
            CurrentMusic = AudioCue.None;
            musicSource.Stop();
        }

        private void StartAmbience()
        {
            if (ambienceSource.isPlaying) return;
            if (!library.TryGet(AudioCue.CrowdLoop, out AudioCueLibrary.Entry e)) return;
            ambienceSource.clip = e.clip;
            ambienceSource.volume = e.volume;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        private void StopAmbience() => ambienceSource.Stop();
    }
}
