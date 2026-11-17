using System.Collections;
using SayHeyDash.Controls;
using UnityEngine;

namespace SayHeyDash.Prototype
{
    /// <summary>
    /// PROTOTYPE (Milestone 1 tech PoC).
    /// On the Action button: plays a procedurally generated retro blip and tints the
    /// placeholder sprite once. Verifies (a) the shared catch/throw button on every device and
    /// (b) that WebGL audio unlocks after the first user gesture.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class PrototypeActionFeedback : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Color flashColor = new Color32(0xF6, 0xCE, 0x4B, 0xFF);
        [SerializeField, Min(0.02f)] private float flashSeconds = 0.12f;

        private AudioSource _audio;
        private AudioClip _blip;
        private WaitForSeconds _flashWait;
        private Coroutine _flashRoutine;

        public int ActionCount { get; private set; }

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            _blip = CreateBlip();
            _flashWait = new WaitForSeconds(flashSeconds);
        }

        private void OnEnable()
        {
            if (input != null) input.ActionPressed += HandleAction;
        }

        private void OnDisable()
        {
            if (input != null) input.ActionPressed -= HandleAction;
        }

        private void OnDestroy()
        {
            if (_blip != null) Destroy(_blip);
        }

        private void HandleAction()
        {
            ActionCount++;
            _audio.PlayOneShot(_blip);

            if (target == null) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            target.color = flashColor;
            yield return _flashWait;
            target.color = Color.white;
            _flashRoutine = null;
        }

        /// <summary>Square-wave blip with a pitch drop - 1990s arcade style, ~90 ms.</summary>
        private static AudioClip CreateBlip()
        {
            const int sampleRate = 44100;
            const float duration = 0.09f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float freq = Mathf.Lerp(880f, 440f, t);
                phase += freq / sampleRate;
                float square = (phase % 1f) < 0.5f ? 1f : -1f;
                data[i] = square * 0.25f * (1f - t);
            }

            AudioClip clip = AudioClip.Create("PrototypeBlip", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
