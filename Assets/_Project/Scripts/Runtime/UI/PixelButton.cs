using SayHeyDash.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Arcade-style menu button on top of uGUI's <see cref="Button"/> (which swaps the
    /// sprites). Adds what the art direction asks for: a cursor arrow on the focused button
    /// (a non-colour focus cue), label colour that stays readable on every fill, hover =
    /// focus so mouse, keyboard and gamepad all show the same state, and menu sounds.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class PixelButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private Graphic cursor;
        [SerializeField] private Color labelColor = new Color32(0x1B, 0x10, 0x30, 0xFF);
        [SerializeField] private Color selectedLabelColor = new Color32(0x1B, 0x10, 0x30, 0xFF);
        [Tooltip("Optional: plays UI move / confirm sounds.")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private AudioCue clickCue = AudioCue.UiConfirm;
        [Tooltip("Off for in-game HUD buttons: during play nothing may hold focus, or Space would press it.")]
        [SerializeField] private bool selectOnHover = true;

        private Button _button;
        private bool _silentSelect;

        public Button Button => _button != null ? _button : (_button = GetComponent<Button>());

        public string Label
        {
            get => label != null ? label.text : string.Empty;
            set { if (label != null) label.text = value; }
        }

        public bool IsSelected => EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
            ApplyState(false);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnEnable() => ApplyState(IsSelected);

        private void OnDisable() => ApplyState(false);

        internal void SuppressNextSelectSound() => _silentSelect = true;

        public void OnSelect(BaseEventData eventData)
        {
            ApplyState(true);
            if (!_silentSelect && audioService != null) audioService.Play(AudioCue.UiMove);
            _silentSelect = false;
        }

        public void OnDeselect(BaseEventData eventData) => ApplyState(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectOnHover && Button.IsInteractable() && EventSystem.current != null && !IsSelected)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void OnClicked()
        {
            if (audioService != null && clickCue != AudioCue.None) audioService.Play(clickCue);
        }

        private void ApplyState(bool selected)
        {
            if (label != null) label.color = selected ? selectedLabelColor : labelColor;
            if (cursor != null) cursor.enabled = selected;
        }
    }
}
