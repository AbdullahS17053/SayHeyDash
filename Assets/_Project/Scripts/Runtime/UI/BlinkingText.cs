using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Slow on/off blink for prompts like PRESS START (1 Hz by default - well below the
    /// 3 Hz photosensitivity limit). Uses unscaled time so it keeps blinking while paused.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class BlinkingText : MonoBehaviour
    {
        [Min(0.1f)] [SerializeField] private float cyclesPerSecond = 1f;
        [Tooltip("Reduced motion: keep the prompt steady.")]
        [SerializeField] private bool steady;

        private Graphic _graphic;

        public bool Steady
        {
            get => steady;
            set { steady = value; if (value && _graphic != null) _graphic.enabled = true; }
        }

        private void Awake() => _graphic = GetComponent<Graphic>();

        private void OnDisable()
        {
            if (_graphic != null) _graphic.enabled = true;
        }

        private void Update()
        {
            if (steady) return;
            _graphic.enabled = Mathf.Repeat(Time.unscaledTime * cyclesPerSecond, 1f) < 0.6f;
        }
    }
}
