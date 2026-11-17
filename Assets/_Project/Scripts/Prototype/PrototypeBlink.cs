using System.Collections;
using UnityEngine;

namespace SayHeyDash.Prototype
{
    /// <summary>
    /// PROTOTYPE (Milestone 1 tech PoC).
    /// Slow (1 Hz) blink for a "PRESS START"-style prompt. 1 Hz is well under the 3 Hz
    /// photosensitivity threshold. The production version (Milestone 3) will honour the
    /// reduced-motion setting.
    /// </summary>
    public sealed class PrototypeBlink : MonoBehaviour
    {
        [SerializeField] private Behaviour target;
        [SerializeField, Min(0.2f)] private float halfPeriodSeconds = 0.5f;

        private void OnEnable()
        {
            if (target != null) StartCoroutine(BlinkLoop());
        }

        private void OnDisable()
        {
            if (target != null) target.enabled = true;
        }

        private IEnumerator BlinkLoop()
        {
            var wait = new WaitForSecondsRealtime(halfPeriodSeconds);
            while (true)
            {
                yield return wait;
                target.enabled = !target.enabled;
            }
        }
    }
}
