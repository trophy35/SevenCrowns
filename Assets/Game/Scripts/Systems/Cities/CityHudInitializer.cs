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
    [DefaultExecutionOrder(-200)]
    public sealed class CityHudInitializer : MonoBehaviour, ICityNameKeyProvider, SevenCrowns.UI.Cities.ICityFactionIdProvider, SevenCrowns.UI.Cities.ICityOccupantHeroProvider, SevenCrowns.UI.Cities.ICityWalletProvider
    {
        [Header("Auto-Create")]
        [SerializeField] private bool _createWalletIfMissing = true;
        [SerializeField] private bool _createTimeIfMissing = true;
        [SerializeField] private bool _createPopulationIfMissing = true;
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;
        private IResourceWallet _walletRef;

        private void Awake()
        {
            EnsureWallet();
            EnsureTimeService();
            EnsurePopulation();
            ApplyTransferIfAvailable();
            // Capture occupant hero info from transfer for UI
            ApplyOccupantFromTransfer();
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

        private string _occupantHeroId;
        private string _occupantPortraitKey;

        private void ApplyOccupantFromTransfer()
        {
            if (CityEnterTransfer.TryConsumeOccupantHero(out var heroId, out var portraitKey))
            {
                _occupantHeroId = string.IsNullOrWhiteSpace(heroId) ? string.Empty : heroId.Trim();
                _occupantPortraitKey = string.IsNullOrWhiteSpace(portraitKey) ? string.Empty : portraitKey.Trim();
                if (_debugLogs)
                    Debug.Log($"[CityHudInit] Consumed occupant hero: id='{_occupantHeroId}' key='{_occupantPortraitKey}'", this);
            }
            else if (CityEnterTransfer.TryPeekOccupantHero(out var heroId2, out var portraitKey2))
            {
                _occupantHeroId = string.IsNullOrWhiteSpace(heroId2) ? string.Empty : heroId2.Trim();
                _occupantPortraitKey = string.IsNullOrWhiteSpace(portraitKey2) ? string.Empty : portraitKey2.Trim();
                if (_debugLogs)
                    Debug.Log($"[CityHudInit] Peek occupant hero: id='{_occupantHeroId}' key='{_occupantPortraitKey}'", this);
            }
            else if (_debugLogs)
            {
                Debug.Log("[CityHudInit] No occupant hero info available in transfer.", this);
            }
        }

        public bool TryGetOccupantHero(out string heroId, out string portraitKey)
        {
            heroId = _occupantHeroId;
            portraitKey = _occupantPortraitKey;
            return !string.IsNullOrEmpty(heroId);
        }

        private void EnsureWallet()
        {
            // Always prefer a wallet on this GameObject for City scene determinism
            var selfWallet = GetComponent<ResourceWalletService>();
            if (selfWallet != null)
            {
                _walletRef = selfWallet;
                if (_debugLogs) Debug.Log("[CityHudInit] Using existing ResourceWalletService on self.", this);
                return;
            }

            if (_createWalletIfMissing)
            {
                var created = gameObject.AddComponent<ResourceWalletService>();
                _walletRef = created;
                if (_debugLogs) Debug.Log("[CityHudInit] Created ResourceWalletService on self.", this);
                return;
            }

            // Fallback: discover any wallet in scene (tests / special cases)
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _walletRef == null; i++)
            {
                if (behaviours[i] is IResourceWallet w) _walletRef = w;
            }
            if (_debugLogs)
            {
                Debug.Log(_walletRef != null
                    ? "[CityHudInit] Bound to first discovered IResourceWallet."
                    : "[CityHudInit] No IResourceWallet found.", this);
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
                // Use authoritative wallet on this object; fallback to discovery
                var wallet = _walletRef != null ? _walletRef : GetComponent<ResourceWalletService>();
                if (wallet == null)
                {
                    var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                    for (int i = 0; i < behaviours.Length && wallet == null; i++)
                    {
                        if (behaviours[i] is IResourceWallet w) wallet = w;
                    }
                }
                if (wallet != null)
                {
                    _walletRef = wallet;
                    if (_debugLogs)
                    {
                        var comp = wallet as Component;
                        var goName = comp != null ? comp.gameObject.name : "(unknown)";
                        Debug.Log($"[CityHudInit] Applying wallet snapshot: entries={amounts.Count} to='{goName}'", this);
                    }
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
                            {
                                int after = wallet.GetAmount(id);
                                Debug.Log($"[CityHudInit] Wallet reconcile: {id} current={current} desired={desired} delta={delta} after={after}", this);
                            }
                        }
                        else if (_debugLogs)
                        {
                            Debug.Log($"[CityHudInit] Wallet already at desired value: {id}={current}", this);
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

        public bool TryGetFactionId(out string factionId)
        {
            if (SevenCrowns.Systems.Cities.CityEnterTransfer.TryPeekCityContext(out _, out var fid))
            {
                factionId = fid;
                if (_debugLogs)
                    Debug.Log($"[CityHudInit] TryGetFactionId -> true id='{factionId}'.", this);
                return true;
            }
            factionId = null;
            if (_debugLogs)
                Debug.Log("[CityHudInit] TryGetFactionId -> false (no context).", this);
            return false;
        }

        // ICityWalletProvider
        public bool TryGetWallet(out IResourceWallet wallet)
        {
            if (_walletRef != null)
            {
                wallet = _walletRef;
                return true;
            }
            wallet = null;
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && wallet == null; i++)
            {
                if (behaviours[i] is IResourceWallet w) wallet = w;
            }
            return wallet != null;
        }
    }
}
