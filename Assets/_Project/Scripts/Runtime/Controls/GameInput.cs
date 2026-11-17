using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SayHeyDash.Controls
{
    /// <summary>
    /// Single entry point for player input. Wraps the "Gameplay" map of the project's
    /// InputActionAsset and exposes device-agnostic values and events.
    ///
    /// Keyboard, gamepad and the mobile on-screen controls (OnScreenStick / OnScreenButton,
    /// which emulate a virtual gamepad) all feed the same actions, so gameplay code never
    /// needs to know which device the player is using.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameInput : MonoBehaviour
    {
        public const string GameplayMapName = "Gameplay";
        public const string MoveActionName = "Move";
        public const string ActionActionName = "Action";
        public const string SubmitActionName = "Submit";
        public const string PauseActionName = "Pause";

        [Tooltip("SayHeyDashControls.inputactions")]
        [SerializeField] private InputActionAsset actions;

        private InputActionMap _gameplayMap;
        private InputAction _move;
        private InputAction _action;
        private InputAction _submit;
        private InputAction _pause;

        /// <summary>Catch / throw button (Space, gamepad South, mobile action button).</summary>
        public event Action ActionPressed;

        /// <summary>Start / continue (Enter, Space, gamepad Start/South).</summary>
        public event Action SubmitPressed;

        /// <summary>Pause (Escape, gamepad Start).</summary>
        public event Action PausePressed;

        /// <summary>Normalised movement direction. Zero when input is unavailable.</summary>
        public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"[{nameof(GameInput)}] No InputActionAsset assigned.", this);
                enabled = false;
                return;
            }

            _gameplayMap = actions.FindActionMap(GameplayMapName);
            if (_gameplayMap == null)
            {
                Debug.LogError($"[{nameof(GameInput)}] Action map '{GameplayMapName}' not found in '{actions.name}'.", this);
                enabled = false;
                return;
            }

            _move = FindAction(MoveActionName);
            _action = FindAction(ActionActionName);
            _submit = FindAction(SubmitActionName);
            _pause = FindAction(PauseActionName);
        }

        private void OnEnable()
        {
            if (_gameplayMap == null) return;

            Subscribe(_action, OnAction);
            Subscribe(_submit, OnSubmit);
            Subscribe(_pause, OnPause);
            _gameplayMap.Enable();
        }

        private void OnDisable()
        {
            if (_gameplayMap == null) return;

            Unsubscribe(_action, OnAction);
            Unsubscribe(_submit, OnSubmit);
            Unsubscribe(_pause, OnPause);
            _gameplayMap.Disable();
        }

        private InputAction FindAction(string actionName)
        {
            InputAction action = _gameplayMap.FindAction(actionName);
            if (action == null)
                Debug.LogError($"[{nameof(GameInput)}] Action '{actionName}' not found in map '{GameplayMapName}'.", this);
            return action;
        }

        private static void Subscribe(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action != null) action.performed += handler;
        }

        private static void Unsubscribe(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action != null) action.performed -= handler;
        }

        private void OnAction(InputAction.CallbackContext _) => ActionPressed?.Invoke();
        private void OnSubmit(InputAction.CallbackContext _) => SubmitPressed?.Invoke();
        private void OnPause(InputAction.CallbackContext _) => PausePressed?.Invoke();
    }
}
