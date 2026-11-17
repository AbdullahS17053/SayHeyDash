using SayHeyDash.Controls;
using UnityEngine;

namespace SayHeyDash.Prototype
{
    /// <summary>
    /// PROTOTYPE (Milestone 1 tech PoC) - not production gameplay code.
    /// Moves a placeholder sprite from GameInput to prove input latency, pixel-perfect
    /// rendering and touch/keyboard parity in the browser. Replaced by the real
    /// outfielder controller in Milestone 2.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypeMover : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField, Min(0f)] private float speedUnitsPerSecond = 6f;
        [Tooltip("Playable area in world units (x, y = bottom-left corner; width, height).")]
        [SerializeField] private Rect bounds = new Rect(-9f, -4.6f, 18f, 3.6f);

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (input == null)
            {
                Debug.LogError($"[{nameof(PrototypeMover)}] GameInput reference missing.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Vector2 move = input.Move;
            if (move.sqrMagnitude < 0.0001f) return;

            Vector3 p = transform.position + (Vector3)(move * (speedUnitsPerSecond * Time.deltaTime));
            p.x = Mathf.Clamp(p.x, bounds.xMin, bounds.xMax);
            p.y = Mathf.Clamp(p.y, bounds.yMin, bounds.yMax);
            transform.position = p;

            if (Mathf.Abs(move.x) > 0.01f) _renderer.flipX = move.x < 0f;
        }
    }
}
