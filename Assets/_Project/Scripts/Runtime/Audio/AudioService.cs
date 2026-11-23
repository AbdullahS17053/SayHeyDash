using System;
using System.Collections;
using SayHeyDash.Flow;
using SayHeyDash.Gameplay;
using UnityEngine;

namespace SayHeyDash.Audio
{
    /// <summary>
    /// Plays the game's sounds in response to flow and mechanic events (event-driven, no
    /// polling). Three sources: one-shot SFX, music/jingles, and the crowd bed.
    /// Mixing (Milestone 4): every cue has a loudness-matched level in the <see cref="AudioCueLibrary"/>;
    /// on top of that two player volumes - MUSIC (title loop + jingles) and EFFECTS (SFX + crowd) -
    /// and mute. The crowd bed ducks under the result jingles and the title music fades out when
    /// play starts. No AudioMixer: plain source volumes behave the same on every WebGL browser.
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

        [Header("Mix")]
        [Tooltip("Crowd bed level (x its cue volume) while a result jingle plays.")]
        [Range(0f, 1f)] [SerializeField] private float ambienceDuckLevel = 0.4f;
        [Tooltip("How long the crowd stays ducked for a result jingle (the jingles are about 1.25 s). A fixed time: on " +
                 "WebGL a clip's length is not known until the browser has decoded it.")]
        [Min(0f)] [SerializeField] private float jingleDuckSeconds = 1.5f;
        [Tooltip("Seconds the title music takes to fade out when the ball is hit.")]
        [Min(0f)] [SerializeField] private float musicFadeOutSeconds = 0.25f;

        private float _musicCueVolume = 1f;
        private float _ambienceCueVolume = 1f;
        private float _musicFade = 1f;
        private float _ambienceDuck = 1f;
        private Coroutine _fadeRoutine;
        private Coroutine _duckRoutine;

        public event Action<bool> MuteChanged;
        /// <summary>Music or effects volume changed (read <see cref="MusicVolume"/> / <see cref="EffectsVolume"/>).</summary>
        public event Action VolumesChanged;

        public bool Muted { get; private set; }
        /// <summary>Player MUSIC volume, 0..1 (title loop and jingles).</summary>
        public float MusicVolume { get; private set; } = 1f;
        /// <summary>Player EFFECTS volume, 0..1 (sound effects, menu sounds and the crowd).</summary>
        public float EffectsVolume { get; private set; } = 1f;

        /// <summary>Last cue requested (tests / debugging).</summary>
        public AudioCue LastCue { get; private set; }
        public int PlayCount { get; private set; }
        public AudioCue CurrentMusic { get; private set; }
        public bool AmbiencePlaying => ambienceSource != null && ambienceSource.isPlaying;
        public bool AmbienceDucked => _ambienceDuck < 1f;

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

        /// <summary>Player volumes, 0..1 each. Applied at once, also to sounds already playing.</summary>
        public void SetVolumes(float music, float effects)
        {
            music = Mathf.Clamp01(music);
            effects = Mathf.Clamp01(effects);
            if (Mathf.Approximately(music, MusicVolume) && Mathf.Approximately(effects, EffectsVolume)) return;
            MusicVolume = music;
            EffectsVolume = effects;
            ApplyVolumes();
            VolumesChanged?.Invoke();
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
                    FadeOutMusic();
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

        private void ApplyVolumes()
        {
            sfxSource.volume = EffectsVolume;
            musicSource.volume = _musicCueVolume * MusicVolume * _musicFade;
            ambienceSource.volume = _ambienceCueVolume * EffectsVolume * _ambienceDuck;
        }

        private void PlayMusic(AudioCue cue, bool loop)
        {
            if (loop && CurrentMusic == cue && musicSource.isPlaying && _fadeRoutine == null) return;
            StopFade();
            CurrentMusic = cue;
            LastCue = cue;
            PlayCount++;
            musicSource.Stop();
            if (!library.TryGet(cue, out AudioCueLibrary.Entry e)) return;
            musicSource.clip = e.clip;
            musicSource.loop = loop;
            _musicCueVolume = e.volume;
            ApplyVolumes();
            musicSource.Play();
            // Jingles: the crowd steps back so the result reads clearly.
            if (!loop && isActiveAndEnabled) Duck(jingleDuckSeconds);
        }

        private void FadeOutMusic()
        {
            CurrentMusic = AudioCue.None;
            StopFade();
            if (!musicSource.isPlaying || musicFadeOutSeconds <= 0f || !isActiveAndEnabled)
            {
                musicSource.Stop();
                return;
            }
            _fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            for (float t = 0f; t < musicFadeOutSeconds; t += Time.unscaledDeltaTime)
            {
                _musicFade = 1f - t / musicFadeOutSeconds;
                ApplyVolumes();
                yield return null;
            }
            musicSource.Stop();
            _fadeRoutine = null;
            _musicFade = 1f;
            ApplyVolumes();
        }

        private void StopFade()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
            _musicFade = 1f;
        }

        private void Duck(float seconds)
        {
            if (_duckRoutine != null) StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(DuckRoutine(seconds));
        }

        private IEnumerator DuckRoutine(float seconds)
        {
            _ambienceDuck = ambienceDuckLevel;
            ApplyVolumes();
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
            _ambienceDuck = 1f;
            ApplyVolumes();
            _duckRoutine = null;
        }

        private void StartAmbience()
        {
            if (ambienceSource.isPlaying) return;
            if (!library.TryGet(AudioCue.CrowdLoop, out AudioCueLibrary.Entry e)) return;
            ambienceSource.clip = e.clip;
            ambienceSource.loop = true;
            _ambienceCueVolume = e.volume;
            ApplyVolumes();
            ambienceSource.Play();
        }

        private void StopAmbience() => ambienceSource.Stop();
    }
}
