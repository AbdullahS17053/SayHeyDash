using SayHeyDash.Platform;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SayHeyDash.Prototype
{
    /// <summary>
    /// PROTOTYPE (Milestone 1 tech PoC).
    /// Tests opening an external page from inside the iframe (needed later for the
    /// "Return to Fastball Fuel museum / STORY MODE" button on Screen 07).
    /// Uses pointer-DOWN so the browser's pointer-up gesture can open the popup.
    /// </summary>
    public sealed class PrototypeLinkButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private string url = "https://example.com/";

        public void OnPointerDown(PointerEventData eventData) => BrowserBridge.OpenUrl(url);
    }
}
