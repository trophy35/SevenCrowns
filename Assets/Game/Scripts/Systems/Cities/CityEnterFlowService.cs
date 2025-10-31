using UnityEngine;
using System.Collections.Generic;
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
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        public void EnterCity(string cityId, string heroId)
        {
            // Capture current wallet, time, and population to seed the City scene HUD
            try
            {
                // Determine city faction id (optional)
                string factionId = string.Empty;
                if (!string.IsNullOrWhiteSpace(cityId))
                {
                    if (SevenCrowns.Map.Cities.CityAuthoring.TryGetNode(cityId, out var authoring) && authoring != null)
                    {
                        factionId = authoring.FactionId;
                    }
                    else
                    {
                        // Fallback: try provider -> authoring lookup by coord (best-effort)
                        SevenCrowns.Map.Cities.ICityNodeProvider cities = null;
                        var behavioursForCity = FindObjectsOfType<MonoBehaviour>(true);
                        for (int i = 0; i < behavioursForCity.Length && cities == null; i++)
                        {
                            if (behavioursForCity[i] is SevenCrowns.Map.Cities.ICityNodeProvider p) cities = p;
                        }
                        if (cities != null && cities.TryGetById(cityId, out var desc) && SevenCrowns.Map.Cities.CityAuthoring.TryGetNode(desc.NodeId, out var auth2) && auth2 != null)
                        {
                            factionId = auth2.FactionId;
                        }
                    }
                }
                CityEnterTransfer.SetCityContext(cityId ?? string.Empty, factionId ?? string.Empty);

                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                // Wallet snapshot
                SevenCrowns.Systems.Save.IResourceWalletSnapshotProvider walletSnap = null;
                for (int i = 0; i < behaviours.Length && walletSnap == null; i++)
                {
                    if (behaviours[i] is SevenCrowns.Systems.Save.IResourceWalletSnapshotProvider sp) walletSnap = sp;
                }
                if (walletSnap != null)
                {
                    var dict = new Dictionary<string, int>(walletSnap.GetAllAmountsSnapshot());
                    CityEnterTransfer.SetWalletSnapshot(dict);
                    if (_debugLogs)
                    {
                        int totalKeys = dict.Count;
                        int totalSum = 0;
                        foreach (var kv in dict) totalSum += kv.Value;
                        Debug.Log($"[CityEnterFlow] Captured wallet snapshot: entries={totalKeys} sum={totalSum}", this);
                    }
                }
                else if (_debugLogs)
                {
                    Debug.Log("[CityEnterFlow] No IResourceWalletSnapshotProvider found when entering city.", this);
                }

                // Population
                SevenCrowns.Systems.IPopulationService pop = null;
                for (int i = 0; i < behaviours.Length && pop == null; i++)
                {
                    if (behaviours[i] is SevenCrowns.Systems.IPopulationService p) pop = p;
                }
                if (pop != null)
                {
                    CityEnterTransfer.SetPopulation(pop.GetAvailable());
                    if (_debugLogs)
                        Debug.Log($"[CityEnterFlow] Captured population: available={pop.GetAvailable()}", this);
                }
                else if (_debugLogs)
                {
                    Debug.Log("[CityEnterFlow] No IPopulationService found when entering city.", this);
                }

                // World time
                SevenCrowns.Systems.IWorldTimeService time = null;
                for (int i = 0; i < behaviours.Length && time == null; i++)
                {
                    if (behaviours[i] is SevenCrowns.Systems.IWorldTimeService t) time = t;
                }
                if (time != null)
                {
                    CityEnterTransfer.SetWorldDate(time.CurrentDate);
                    if (_debugLogs)
                        Debug.Log($"[CityEnterFlow] Captured world date: {time.CurrentDate}", this);
                }
                else if (_debugLogs)
                {
                    Debug.Log("[CityEnterFlow] No IWorldTimeService found when entering city.", this);
                }
            }
            catch { /* non-fatal: we can still enter City */ }

            // Delegate to the central scene flow controller for proper fades
            SevenCrowns.SceneFlow.SceneFlowController.GoToBySceneName(_citySceneName);
        }
    }
}
