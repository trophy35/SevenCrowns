using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map.Farms;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Computes weekly population from owned farms and resets the population pool at the start of each week.
    /// Does not grant population on capture; only on week rollover.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FarmProductionService : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private MonoBehaviour _timeServiceBehaviour;   // Optional; must implement IWorldTimeService
        [SerializeField] private MonoBehaviour _farmProviderBehaviour;   // Optional; must implement IFarmNodeProvider
        [SerializeField] private MonoBehaviour _populationBehaviour;     // Optional; must implement IPopulationService
        [SerializeField, Tooltip("When enabled and no IPopulationService is found in scene, one will be added to this GameObject at runtime.")]
        private bool _autoCreatePopulationService = true;

        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private IWorldTimeService _time;
        private IFarmNodeProvider _farms;
        private IPopulationService _population;

        private readonly List<FarmNodeDescriptor> _buffer = new(16);
        private WorldDate _lastProcessedDate;
        private bool _weekInitialized;

        private void Awake()
        {
            ResolveTimeService();
            ResolveFarmProvider();
            ResolvePopulation();
            if (_debugLogs)
            {
                Debug.Log($"[FarmProduction] Awake. autoCreatePopulation={_autoCreatePopulationService}. time={( _time!=null)} farms={(_farms!=null)} pop={(_population!=null)}", this);
            }
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
                _time.DateChanged += OnDateChanged;
            }
            _weekInitialized = false;
            SubscribeFarmEvents();
        }

        private void OnEnable()
        {
            ResolveTimeService();
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
                _time.DateChanged += OnDateChanged;
            }
            SubscribeFarmEvents();
            if (_debugLogs)
            {
                Debug.Log("[FarmProduction] OnEnable. Subscribed to DateChanged and farm events.", this);
            }
        }

        public void BindServices(IWorldTimeService time, IFarmNodeProvider farms, IPopulationService population)
        {
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
            }
            _time = time;
            _farms = farms;
            _population = population;
            if (_time != null)
            {
                _time.DateChanged += OnDateChanged;
            }
        }

        private void OnDisable()
        {
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
            }
            UnsubscribeFarmEvents();
        }

        private void Update()
        {
            if (_time == null) return;
            if (!_weekInitialized)
            {
                if (_debugLogs)
                {
                    Debug.Log("[FarmProduction] Update tick: week not initialized yet. Trying to apply weekly.", this);
                }
                TryApplyWeekly(_time.CurrentDate);
            }
        }

        private void OnDateChanged(WorldDate date)
        {
            // New day could also mean new week; always try to apply for this week
            // Reset the week-initialized flag when week changes
            if (_lastProcessedDate.Week != date.Week || _lastProcessedDate.Month != date.Month)
            {
                _weekInitialized = false;
                if (_debugLogs)
                {
                    Debug.Log($"[FarmProduction] Week changed → resetting init flag. New date={date}", this);
                }
            }
            TryApplyWeekly(date);
        }

        private void TryApplyWeekly(WorldDate date)
        {
            if (_farms == null || _population == null)
            {
                ResolveFarmProvider();
                ResolvePopulation();
                if (_farms == null || _population == null)
                {
                    if (_debugLogs)
                    {
                        Debug.Log($"[FarmProduction] Dependencies not ready. farms={( _farms!=null)} pop={( _population!=null)}. Will retry.", this);
                    }
                    return;
                }
            }

            if (_weekInitialized)
            {
                if (_debugLogs)
                {
                    Debug.Log("[FarmProduction] Week already initialized. Skipping.", this);
                }
                return;
            }

            var nodes = _farms.Nodes;
            // Delay initialization if no farms have been registered yet (common during scene startup)
            if (nodes == null || nodes.Count == 0)
            {
                if (_debugLogs)
                {
                    Debug.Log("[FarmProduction] FarmNodeService nodes list is empty at init time.", this);
                }
                return;
            }

            _buffer.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                _buffer.Add(nodes[i]);
            }

            int totalWeekly = 0;
            int ownedCount = 0;
            int contributing = 0;
            for (int i = 0; i < _buffer.Count; i++)
            {
                var farm = _buffer[i];
                if (!farm.IsOwned) continue;
                ownedCount++;
                if (farm.WeeklyPopulationYield <= 0) continue;
                contributing++;
                totalWeekly += farm.WeeklyPopulationYield;
            }

            _population.ResetTo(totalWeekly);
            _weekInitialized = true;
            _lastProcessedDate = date;

            if (_debugLogs)
            {
                Debug.Log($"[FarmProduction] Applied weekly population = {totalWeekly} for {date}. nodes={nodes.Count} owned={ownedCount} contributing={contributing}", this);
            }
        }

        private void SubscribeFarmEvents()
        {
            if (_farms == null) return;
            _farms.NodeRegistered -= OnFarmNodeChanged;
            _farms.NodeRegistered += OnFarmNodeChanged;
            _farms.NodeUpdated -= OnFarmNodeChanged;
            _farms.NodeUpdated += OnFarmNodeChanged;
            if (_debugLogs)
            {
                Debug.Log("[FarmProduction] Subscribed to FarmNodeService events.", this);
            }
        }

        private void UnsubscribeFarmEvents()
        {
            if (_farms == null) return;
            _farms.NodeRegistered -= OnFarmNodeChanged;
            _farms.NodeUpdated -= OnFarmNodeChanged;
        }

        private void OnFarmNodeChanged(FarmNodeDescriptor _)
        {
            if (_time != null)
            {
                if (_debugLogs)
                {
                    var count = _farms != null && _farms.Nodes != null ? _farms.Nodes.Count : -1;
                    Debug.Log($"[FarmProduction] Farm node changed. nodes={count}. Trying to apply weekly if not initialized.", this);
                }
                TryApplyWeekly(_time.CurrentDate);
            }
        }

        private void ResolveTimeService()
        {
            if (_time != null) return;
            if (_timeServiceBehaviour != null && _timeServiceBehaviour is IWorldTimeService t) { _time = t; return; }
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _time == null; i++)
            {
                if (behaviours[i] is IWorldTimeService candidate) _time = candidate;
            }
        }

        private void ResolveFarmProvider()
        {
            if (_farms != null) return;
            if (_farmProviderBehaviour != null && _farmProviderBehaviour is IFarmNodeProvider fp) { _farms = fp; SubscribeFarmEvents(); return; }
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _farms == null; i++)
            {
                if (behaviours[i] is IFarmNodeProvider candidate) { _farms = candidate; SubscribeFarmEvents(); }
            }
            if (_debugLogs)
            {
                Debug.Log($"[FarmProduction] ResolveFarmProvider result: farmsServicePresent={( _farms!=null)}", this);
            }
        }

        private void ResolvePopulation()
        {
            if (_population != null) return;
            if (_populationBehaviour != null && _populationBehaviour is IPopulationService ps) { _population = ps; if (_debugLogs) Debug.Log("[FarmProduction] Bound explicit IPopulationService.", this); return; }
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _population == null; i++)
            {
                if (behaviours[i] is IPopulationService candidate) { _population = candidate; if (_debugLogs) Debug.Log("[FarmProduction] Auto-discovered IPopulationService in scene.", this); }
            }
            if (_population == null && _autoCreatePopulationService)
            {
                _population = gameObject.AddComponent<PopulationService>();
                if (_debugLogs)
                {
                    Debug.Log("[FarmProduction] Auto-created local PopulationService.", this);
                }
            }
            else if (_population == null && !_autoCreatePopulationService)
            {
                Debug.LogWarning("[FarmProduction] No IPopulationService found and auto-create is disabled. Weekly population will not be applied.", this);
            }
        }
    }
}
