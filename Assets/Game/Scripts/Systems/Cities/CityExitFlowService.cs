using UnityEngine;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Handles exiting the City scene back to the WorldMap using the central SceneFlowController.
    /// Attach this to a City scene object and wire <see cref="ExitToWorldMap"/> to a UI Button onClick.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityExitFlowService : MonoBehaviour
    {
        [SerializeField] private string _worldMapSceneName = "WorldMap";
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        /// <summary>
        /// UI hook: returns to the WorldMap scene with a fade transition.
        /// </summary>
        public void ExitToWorldMap()
        {
            // In Edit Mode tests (not playing), avoid driving SceneFlow/scene loads.
            // This keeps unit tests engine-light and prevents DontDestroyOnLoad/LoadScene errors.
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (_debugLogs)
                {
                    Debug.Log("[CityExitFlow] Ignoring ExitToWorldMap in Edit Mode (not playing).", this);
                }
                return;
#endif
            }

            if (_debugLogs)
            {
                Debug.Log($"[CityExitFlow] Returning to scene '{_worldMapSceneName}'.", this);
            }

            SevenCrowns.SceneFlow.SceneFlowController.GoToBySceneName(_worldMapSceneName);
        }
    }
}
