using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Shared helpers for UI pointer detection to avoid duplicate raycast logic.
    /// </summary>
    public static class UiPointerUtility
    {
        private static readonly List<RaycastResult> s_RaycastResults = new List<RaycastResult>(16);

        /// <summary>
        /// Determines whether the given pointer position overlaps any UI elements.
        /// </summary>
        /// <param name="pointerPosition">Screen-space pointer position.</param>
        /// <param name="eventSystem">Optional event system (defaults to <see cref="EventSystem.current"/>).</param>
        /// <returns><c>true</c> if the pointer is over UI.</returns>
        public static bool IsPointerOverUI(Vector2 pointerPosition, EventSystem eventSystem = null)
        {
            var es = eventSystem ?? EventSystem.current;
            if (es == null) return false;

            if (es.IsPointerOverGameObject()) return true;

            var ped = new PointerEventData(es)
            {
                position = pointerPosition
            };

            s_RaycastResults.Clear();
            es.RaycastAll(ped, s_RaycastResults);
            return s_RaycastResults.Count > 0;
        }
    }
}
