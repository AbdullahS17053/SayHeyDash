using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SayHeyDash.UI
{
    /// <summary>Keyboard / gamepad focus helper shared by the screen views.</summary>
    public static class UiSelection
    {
        /// <summary>
        /// Focuses a control without its "move" sound (the screen just opened). Passing null
        /// clears focus, so during live gameplay Space never clicks a button.
        /// </summary>
        public static void Select(Selectable target)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                es.SetSelectedGameObject(null);
                return;
            }
            if (target.TryGetComponent(out PixelButton pixel)) pixel.SuppressNextSelectSound();
            es.SetSelectedGameObject(target.gameObject);
        }
    }
}
