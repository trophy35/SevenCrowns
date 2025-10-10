using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Runtime implementation of <see cref="IResourceWallet"/> storing resource amounts for the current profile/session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceWalletService : MonoBehaviour, IResourceWallet
    {
        [Serializable]
        private struct StartingResource
        {
            public string resourceId;
            public int amount;
        }

        [Header("Starting Resources")]
        [SerializeField]
        private List<StartingResource> _startingResources = new();

        private readonly Dictionary<string, int> _amounts = new(StringComparer.Ordinal);

        public event Action<ResourceChange> ResourceChanged;

        private void Awake()
        {
            InitializeFromStartingResources();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_startingResources == null)
            {
                _startingResources = new List<StartingResource>();
                return;
            }

            for (int i = 0; i < _startingResources.Count; i++)
            {
                var entry = _startingResources[i];
                entry.resourceId = NormalizeResourceId(entry.resourceId);
                _startingResources[i] = entry;
            }
        }
#endif

        public int GetAmount(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return 0;

            var key = NormalizeResourceId(resourceId);
            return _amounts.TryGetValue(key, out var value) ? value : 0;
        }

        public void Add(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount == 0)
                return;

            var key = NormalizeResourceId(resourceId);
            int current = GetAmount(key);
            int newAmount = current + amount;
            _amounts[key] = newAmount;

            ResourceChanged?.Invoke(new ResourceChange(key, amount, newAmount));
        }

        public bool TrySpend(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0)
                return false;

            var key = NormalizeResourceId(resourceId);
            int current = GetAmount(key);
            if (current < amount)
                return false;

            int newAmount = current - amount;
            _amounts[key] = newAmount;
            ResourceChanged?.Invoke(new ResourceChange(key, -amount, newAmount));
            return true;
        }

        private void InitializeFromStartingResources()
        {
            _amounts.Clear();
            if (_startingResources == null)
                return;

            for (int i = 0; i < _startingResources.Count; i++)
            {
                var entry = _startingResources[i];
                if (string.IsNullOrWhiteSpace(entry.resourceId) || entry.amount == 0)
                    continue;

                var key = NormalizeResourceId(entry.resourceId);
                if (_amounts.TryGetValue(key, out var existing))
                {
                    _amounts[key] = existing + entry.amount;
                }
                else
                {
                    _amounts.Add(key, entry.amount);
                }
            }
        }

        private static string NormalizeResourceId(string resourceId)
        {
            return string.IsNullOrWhiteSpace(resourceId)
                ? string.Empty
                : resourceId.Trim();
        }
    }
}

