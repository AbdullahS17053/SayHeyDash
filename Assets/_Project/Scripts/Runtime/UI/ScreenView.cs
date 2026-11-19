using SayHeyDash.Data;
using SayHeyDash.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Base for the per-screen views (Docs/01 section 4). A view sits on an always-active
    /// GameObject and shows/hides its <c>panel</c> child when the flow state changes.
    /// Views contain no game logic: they read state, show content and call flow commands.
    /// </summary>
    public abstract class ScreenView : MonoBehaviour
    {
        [SerializeField] protected GameFlowController flow;
        [SerializeField] protected MissionContent content;
        [Tooltip("The screen's root; toggled on/off.")]
        [SerializeField] protected GameObject panel;
        [Tooltip("Focused when the screen opens (keyboard / gamepad). Empty = nothing focused.")]
        [SerializeField] protected Selectable firstSelected;

        private bool _valid;

        public bool IsShown => panel != null && panel.activeSelf;

        protected virtual void Awake()
        {
            _valid = flow != null && content != null && panel != null && Validate();
            if (!_valid)
            {
                Debug.LogError($"[{GetType().Name}] Missing reference(s) - rebuild with Say Hey Dash > Build Main Scene.", this);
                enabled = false;
                return;
            }
            ApplyContent();
        }

        protected virtual void OnEnable()
        {
            if (!_valid) return;
            flow.StateChanged += HandleStateChanged;
            Refresh(flow.State, true);
        }

        protected virtual void OnDisable()
        {
            if (flow != null) flow.StateChanged -= HandleStateChanged;
        }

        /// <summary>True in the states (or overlay condition) where this screen is on.</summary>
        protected abstract bool IsVisibleIn(GameState state);

        /// <summary>Check the subclass's own references.</summary>
        protected virtual bool Validate() => true;

        /// <summary>Fill static text from <see cref="MissionContent"/> once.</summary>
        protected virtual void ApplyContent() { }

        /// <summary>Called when the screen opens, and for every state change while it stays open.</summary>
        protected virtual void OnStateEntered(GameState state, bool justShown) { }

        protected virtual void OnHidden() { }

        /// <summary>Re-evaluates visibility (subclasses with overlay conditions call this).</summary>
        protected void Refresh(GameState state, bool force = false)
        {
            bool visible = IsVisibleIn(state);
            bool wasVisible = panel.activeSelf;
            if (visible != wasVisible || force) panel.SetActive(visible);
            if (visible)
            {
                bool justShown = !wasVisible || force;
                OnStateEntered(state, justShown);
                if (justShown) UiSelection.Select(firstSelected);
            }
            else if (wasVisible)
            {
                OnHidden();
            }
        }

        private void HandleStateChanged(GameState previous, GameState next) => Refresh(next);

        protected static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
