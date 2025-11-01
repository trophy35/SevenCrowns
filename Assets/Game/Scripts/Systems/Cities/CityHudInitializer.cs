using UnityEngine;
using SevenCrowns.Map.Resources;
using SevenCrowns.Systems;
using SevenCrowns.UI.Cities;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Ensures the City scene HUD has the required backing services available.
    /// - IResourceWallet (ResourceWalletService)
    /// - IWorldTimeService (WorldTimeService)
    /// - IPopulationService (PopulationService)
    /// HUD views (ResourceAmountHudView, PeopleAmountHudView, WorldTimeHudView) auto-discover these.
    /// Place one instance in the City scene on a Core object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityHudInitializer : MonoBehaviour, ICityNameKeyProvider
    {
        [Header("Auto-Create")]
        [SerializeField] private bool _createWalletIfMissing = true;
        [SerializeField] private bool _createTimeIfMissing = true;
        [SerializeField] private bool _createPopulationIfMissing = true;
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private void Awake()
        {
            EnsureWallet();
            EnsureTimeService();
            EnsurePopulation();
            ApplyTransferIfAvailable();
            if (_debugLogs)
            {
                if (CityEnterTransfer.TryPeekCityContext(out var cid, out var fid))
                {
                    Debug.Log($"[CityHudInit] Peek city context: cityId='{cid}' factionId='{fid}'.", this);
                }
                else
                {
                    Debug.Log("[CityHudInit] No city context available.", this);
                }
                if (CityEnterTransfer.TryPeekCityNameKey(out var nameKey))
                {
                    Debug.Log($"[CityHudInit] Peek city name key: '{nameKey}'.", this);
                }
                else
                {
                    Debug.Log("[CityHudInit] No city name key available.", this);
                }
            }
        }

        private void EnsureWallet()
        {
            if (!_createWalletIfMissing) return;

            // Try explicit search first
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IResourceWallet)
                    return; // found
            }

            // Create a wallet on this GameObject
            if (GetComponent<ResourceWalletService>() == null)
            {
                gameObject.AddComponent<ResourceWalletService>();
                if (_debugLogs)
                    Debug.Log("[CityHudInit] Created ResourceWalletService.", this);
            }
        }

        private void EnsureTimeService()
        {
            if (!_createTimeIfMissing) return;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldTimeService)
                    return; // found
            }

            if (GetComponent<WorldTimeService>() == null)
            {
                gameObject.AddComponent<WorldTimeService>();
                if (_debugLogs)
                    Debug.Log("[CityHudInit] Created WorldTimeService.", this);
            }
        }

        private void EnsurePopulation()
        {
            if (!_createPopulationIfMissing) return;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPopulationService)
                    return; // found
            }

            if (GetComponent<PopulationService>() == null)
            {
                gameObject.AddComponent<PopulationService>();
                if (_debugLogs)
                    Debug.Log("[CityHudInit] Created PopulationService.", this);
            }
        }

        private void ApplyTransferIfAvailable()
        {
            // Apply wallet snapshot
            if (CityEnterTransfer.TryConsumeWallet(out var amounts) && amounts != null)
            {
                // Find an IResourceWallet
                IResourceWallet wallet = null;
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && wallet == null; i++)
                {
                    if (behaviours[i] is IResourceWallet w) wallet = w;
                }
                if (wallet != null)
                {
                    if (_debugLogs)
                        Debug.Log($"[CityHudInit] Applying wallet snapshot: entries={amounts.Count}", this);
                    foreach (var kv in amounts)
                    {
                        var id = kv.Key;
                        int desired = kv.Value;
                        int current = wallet.GetAmount(id);
                        int delta = desired - current;
                        if (delta != 0)
                        {
                            wallet.Add(id, delta);
                            if (_debugLogs)
                                Debug.Log($"[CityHudInit] Wallet reconcile: {id} current={current} desired={desired} delta={delta}", this);
                        }
                    }
                }
                else if (_debugLogs)
                {
                    Debug.Log("[CityHudInit] No IResourceWallet found to apply snapshot.", this);
                }
            }

            // Apply population
            if (CityEnterTransfer.TryConsumePopulation(out var population))
            {
                IPopulationService pop = null;
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && pop == null; i++)
                {
                    if (behaviours[i] is IPopulationService p) pop = p;
                }
                if (pop != null)
                {
                    pop.ResetTo(Mathf.Max(0, population));
                    if (_debugLogs)
                        Debug.Log($"[CityHudInit] Applied population: {population}", this);
                }
                else if (_debugLogs)
                {
                    Debug.Log("[CityHudInit] No IPopulationService found to apply population.", this);
                }
            }

            // Apply world time
            if (CityEnterTransfer.TryConsumeWorldDate(out var date))
            {
                IWorldTimeService time = null;
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && time == null; i++)
                {
                    if (behaviours[i] is IWorldTimeService t) time = t;
                }
                if (time != null)
                {
                    time.ResetTo(date);
                    if (_debugLogs)
                        Debug.Log($"[CityHudInit] Applied world date: {date}", this);
                }
                else if (_debugLogs)
                {
                    Debug.Log("[CityHudInit] No IWorldTimeService found to apply date.", this);
                }
            }
        }

        // ICityNameKeyProvider
        public bool TryGetCityNameKey(out string cityNameKey)
        {
            var ok = SevenCrowns.Systems.Cities.CityEnterTransfer.TryPeekCityNameKey(out cityNameKey);
            if (_debugLogs)
                Debug.Log($"[CityHudInit] TryGetCityNameKey -> {ok} key='{cityNameKey}'.", this);
            return ok;
        }

        public bool TryGetCityId(out string cityId)
        {
            if (SevenCrowns.Systems.Cities.CityEnterTransfer.TryPeekCityContext(out var id, out _))
            {
                cityId = id;
                if (_debugLogs)
                    Debug.Log($"[CityHudInit] TryGetCityId -> true id='{cityId}'.", this);
                return true;
            }
            cityId = null;
            if (_debugLogs)
                Debug.Log("[CityHudInit] TryGetCityId -> false (no context).", this);
            return false;
        }
    }
}
