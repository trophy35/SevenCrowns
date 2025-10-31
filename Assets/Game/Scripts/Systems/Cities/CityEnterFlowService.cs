using UnityEngine;
using SevenCrowns.Map;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Core-side bridge implementing ICityEnterFlow. Uses SceneFlowController to transition to the City scene.
    /// Place one instance in the world scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityEnterFlowService : MonoBehaviour, ICityEnterFlow
    {
        [SerializeField] private string _citySceneName = "City";

        public void EnterCity(string cityId, string heroId)
        {
            // Delegate to the central scene flow controller for proper fades
            SevenCrowns.SceneFlow.SceneFlowController.GoToBySceneName(_citySceneName);
        }
    }
}
