using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map.Mines;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Awards daily resource income from owned mines to the player's resource wallet
    /// when the in-game day advances.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MineProductionService : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private MonoBehaviour _timeServiceBehaviour;   // Optional; must implement IWorldTimeService
        [SerializeField] private MonoBehaviour _mineProviderBehaviour;   // Optional; must implement IMineNodeProvider
        [SerializeField] private MonoBehaviour _walletBehaviour;        // Optional; must implement IResourceWallet

        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private IWorldTimeService _time;
        private IMineNodeProvider _mines;
        private IResourceWallet _wallet;

        private readonly List<MineNodeDescriptor> _buffer = new(16);
        private WorldDate _lastProcessedDate;
        private bool _hasProcessedDate;

        private void Awake()
        {
            ResolveTimeService();
            ResolveMineProvider();
            ResolveWallet();
            // In EditMode tests, OnEnable may not fire as expected; subscribe here defensively.
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

        /// <summary>
        /// Explicitly binds dependencies. Useful for tests or manual wiring.
        /// </summary>
        public void BindServices(IWorldTimeService time, IMineNodeProvider mines, IResourceWallet wallet)
        {
            if (_time != null)
            {
                _time.DateChanged -= OnDateChanged;
            }
            _time = time;
            _mines = mines;
            _wallet = wallet;
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
            if (_mines == null || _wallet == null)
            {
                // Lazy resolve in case services were instantiated after us
                ResolveMineProvider();
                ResolveWallet();
                if (_mines == null || _wallet == null)
                    return;
            }

            // Copy to buffer to avoid iterating a potentially mutable list directly
            _buffer.Clear();
            var nodes = _mines.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                _buffer.Add(nodes[i]);
            }

            int totalMines = 0;
            for (int i = 0; i < _buffer.Count; i++)
            {
                var mine = _buffer[i];
                if (!mine.IsOwned)
                    continue;
                if (mine.DailyYield <= 0)
                    continue;
                var rid = mine.ResourceId;
                if (string.IsNullOrEmpty(rid))
                    continue;

                _wallet.Add(rid, mine.DailyYield);
                totalMines++;
            }

            if (_debugLogs)
            {
                Debug.Log($"[MineProduction] Applied daily production from {totalMines} owned mines for {date}.", this);
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

        private void ResolveMineProvider()
        {
            if (_mines != null)
                return;

            if (_mineProviderBehaviour != null && _mineProviderBehaviour is IMineNodeProvider p)
            {
                _mines = p;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMineNodeProvider candidate)
                {
                    _mines = candidate;
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
    }
}
