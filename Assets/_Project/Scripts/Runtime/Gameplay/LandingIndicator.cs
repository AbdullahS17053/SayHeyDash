using UnityEngine;

namespace SayHeyDash.Gameplay
{
    /// <summary>
    /// The ring drawn at the landing point. Dashed while the outfielder is outside the zone,
    /// solid when inside - a shape cue, not colour alone (GDD accessibility).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LandingIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private Sprite dashedSprite;
        [SerializeField] private Sprite solidSprite;
        [Tooltip("Radius in world units that the ring sprite represents at scale 1.")]
        [SerializeField] private float spriteRadius = 1f;

        private void Awake()
        {
            if (ring == null || dashedSprite == null || solidSprite == null)
            {
                Debug.LogError($"[{nameof(LandingIndicator)}] Missing ring renderer or sprites.", this);
                enabled = false;
            }
        }

        public void Show(Vector2 center, float radius)
        {
            transform.position = center;
            float s = radius / Mathf.Max(0.01f, spriteRadius);
            transform.localScale = new Vector3(s, s, 1f);
            ring.sprite = dashedSprite;
            ring.enabled = true;
        }

        public void SetInZone(bool inZone) => ring.sprite = inZone ? solidSprite : dashedSprite;

        public void Hide() => ring.enabled = false;
    }
}
