using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map.Cities;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Awards daily gold income from owned cities when the in-game day advances.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityProductionService : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private MonoBehaviour _timeServiceBehaviour;   // Optional; must implement IWorldTimeService
        [SerializeField] private MonoBehaviour _cityProviderBehaviour;   // Optional; must implement ICityNodeProvider
        [SerializeField] private MonoBehaviour _walletBehaviour;        // Optional; must implement IResourceWallet

        [Header("Config")]
        [SerializeField] private string _goldResourceId = "resource.gold";

        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private IWorldTimeService _time;
        private ICityNodeProvider _cities;
        private IResourceWallet _wallet;

        private readonly List<CityNodeDescriptor> _buffer = new(16);
        private WorldDate _lastProcessedDate;
        private bool _hasProcessedDate;

        private void Awake()
        {
            _goldResourceId = NormalizeResourceId(_goldResourceId);
            ResolveTimeService();
            ResolveCityProvider();
            ResolveWallet();
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
                _time.DateChanged += OnDateChanged;
            }
            _hasProcessedDate = false;
        }

        private void OnEnable()
        {
            ResolveTimeService();
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
                _time.DateChanged += OnDateChanged;
            }
        }

        public void BindServices(IWorldTimeService time, ICityNodeProvider cities, IResourceWallet wallet, string goldResourceId = "resource.gold")
        {
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
            }
            _time = time;
            _cities = cities;
            _wallet = wallet;
            _goldResourceId = NormalizeResourceId(goldResourceId);
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
        }

        private void Update()
        {
            if (_time == null)
                return;
            var current = _time.CurrentDate;
            if (!_hasProcessedDate || !_lastProcessedDate.Equals(current))
            {
                ApplyProduction(current);
            }
        }

        private void OnDateChanged(WorldDate date)
        {
            ApplyProduction(date);
        }

        private void ApplyProduction(WorldDate date)
        {
            if (_cities == null || _wallet == null)
            {
                ResolveCityProvider();
                ResolveWallet();
                if (_cities == null || _wallet == null)
                    return;
            }

            _buffer.Clear();
            var nodes = _cities.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                _buffer.Add(nodes[i]);
            }

            int totalCities = 0;
            int totalGold = 0;
            for (int i = 0; i < _buffer.Count; i++)
            {
                var city = _buffer[i];
                if (!city.IsOwned)
                    continue;

                int yield = city.DailyGoldYield;
                if (yield <= 0)
                    continue;

                _wallet.Add(_goldResourceId, yield);
                totalCities++;
                totalGold += yield;
            }

            if (_debugLogs)
            {
                Debug.Log($"[CityProduction] Applied daily gold {totalGold} from {totalCities} owned cities for {date}.", this);
            }

            _lastProcessedDate = date;
            _hasProcessedDate = true;
        }

        private void ResolveTimeService()
        {
            if (_time != null)
                return;

            if (_timeServiceBehaviour != null && _timeServiceBehaviour is IWorldTimeService t)
            {
                _time = t;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldTimeService candidate)
                {
                    _time = candidate;
                    return;
                }
            }
        }

        private void ResolveCityProvider()
        {
            if (_cities != null)
                return;

            if (_cityProviderBehaviour != null && _cityProviderBehaviour is ICityNodeProvider p)
            {
                _cities = p;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICityNodeProvider candidate)
                {
                    _cities = candidate;
                    return;
                }
            }
        }

        private void ResolveWallet()
        {
            if (_wallet != null)
                return;

            if (_walletBehaviour != null && _walletBehaviour is IResourceWallet w)
            {
                _wallet = w;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IResourceWallet candidate)
                {
                    _wallet = candidate;
                    return;
                }
            }
        }

        private static string NormalizeResourceId(string resourceId)
        {
            return string.IsNullOrWhiteSpace(resourceId) ? string.Empty : resourceId.Trim();
        }
    }
}

