using SayHeyDash.Gameplay;
using UnityEngine;

namespace SayHeyDash.UI
{
    /// <summary>
    /// Draws the Stage C accuracy meter: the target zone (with notched edges - a shape cue,
    /// not colour alone) and the sweeping needle. Only updates while the meter is visible.
    /// The track is drawn as two pieces either side of the zone, so no background image sits
    /// underneath the zone (in WebGL builds a fill drawn over a same-material background was
    /// not visible).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThrowMeterView : MonoBehaviour
    {
        [SerializeField] private ThrowSystem throwSystem;
        [Tooltip("Inner area of the meter; all parts below are its children, anchored left-middle.")]
        [SerializeField] private RectTransform track;
        [SerializeField] private RectTransform trackLeft;
        [SerializeField] private RectTransform zone;
        [SerializeField] private RectTransform trackRight;
        [Tooltip("Horizontal anchor follows the needle.")]
        [SerializeField] private RectTransform needle;

        private void Awake()
        {
            if (throwSystem == null || track == null || trackLeft == null || zone == null ||
                trackRight == null || needle == null)
            {
                Debug.LogError($"[{nameof(ThrowMeterView)}] Missing reference.", this);
                enabled = false;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            float width = track.rect.width;
            float height = track.rect.height;
            float left = Mathf.Clamp01(throwSystem.ZoneCenter - throwSystem.ZoneWidth * 0.5f) * width;
            float right = Mathf.Clamp01(throwSystem.ZoneCenter + throwSystem.ZoneWidth * 0.5f) * width;
            Place(trackLeft, 0f, left, height);
            Place(zone, left, right, height);
            Place(trackRight, right, width, height);
            SetNeedle(throwSystem.Needle);
        }

        public void Hide() => gameObject.SetActive(false);

        private void LateUpdate() => SetNeedle(throwSystem.Needle);

        private static void Place(RectTransform rt, float fromX, float toX, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(fromX, 0f);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, toX - fromX), height);
        }

        private void SetNeedle(float x)
        {
            needle.anchorMin = new Vector2(x, needle.anchorMin.y);
            needle.anchorMax = new Vector2(x, needle.anchorMax.y);
        }
    }
}
