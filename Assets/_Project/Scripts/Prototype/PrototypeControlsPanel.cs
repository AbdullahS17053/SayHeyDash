using UnityEngine;

namespace SayHeyDash.Prototype
{
    /// <summary>
    /// PROTOTYPE (Milestone 1 tech PoC).
    /// Button targets for the PoC UI: mute toggle and fullscreen request. Fullscreen is a
    /// deliberate iframe test - it only works when the embedding iframe has allow="fullscreen".
    /// The real settings/audio systems arrive in Milestones 3-4.
    /// </summary>
    public sealed class PrototypeControlsPanel : MonoBehaviour
    {
        public void ToggleMute()
        {
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
        }

        public void ToggleFullscreen()
        {
            // On WebGL Unity defers the request to the next user gesture, as browsers require.
            Screen.fullScreen = !Screen.fullScreen;
        }
    }
}
