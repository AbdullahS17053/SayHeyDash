using SayHeyDash.Data;
using SayHeyDash.Flow;
using SayHeyDash.Platform;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Mobile controls (GDD: virtual directional control + catch/throw button). The on-screen
    /// stick and action button (Input System On-Screen Controls = a virtual gamepad, so gameplay
    /// code has no touch-specific path) are shown only on touch devices - detected from the
    /// browser at start-up, or switched on by the first finger on the screen (tablets with a
    /// keyboard, touch laptops). The word under the button says what it does right now:
    /// CATCH, then THROW. They live in the HUD, so they only appear during play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TouchControls : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private MissionContent content;
        [SerializeField] private GameObject stick;
        [SerializeField] private GameObject actionButton;
        [SerializeField] private Text actionLabel;

        private bool _touchMode;
        private bool _valid;

        /// <summary>True when the touch controls are in use on this device.</summary>
        public bool TouchMode => _touchMode;

        private void Awake()
        {
            _valid = flow != null && content != null && stick != null && actionButton != null && actionLabel != null;
            if (!_valid)
            {
                Debug.LogError($"[{nameof(TouchControls)}] Missing reference.", this);
                enabled = false;
                return;
            }
            _touchMode = BrowserBridge.IsTouchDevice;
            Apply();
        }

        private void OnEnable()
        {
            if (!_valid) return;
            flow.StateChanged += OnStateChanged;
            UpdateLabel(flow.State);
        }

        private void OnDisable()
        {
            if (flow != null) flow.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (_touchMode) return;
            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) SetTouchMode(true);
        }

        /// <summary>Test hook / manual override.</summary>
        internal void SetTouchMode(bool value)
        {
            if (_touchMode == value) return;
            _touchMode = value;
            Apply();
        }

        private void Apply()
        {
            stick.SetActive(_touchMode);
            actionButton.SetActive(_touchMode);
            actionLabel.gameObject.SetActive(_touchMode);
        }

        private void OnStateChanged(GameState previous, GameState next) => UpdateLabel(next);

        private void UpdateLabel(GameState state) =>
            actionLabel.text = state == GameState.Throw ? content.touchThrowLabel : content.touchCatchLabel;
    }
}
