using System;
using UnityEngine;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Simple in-memory population pool for the current session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopulationService : MonoBehaviour, IPopulationService
    {
        [SerializeField, Min(0)] private int _startingAmount;

        private int _available;
        public event Action<int> PopulationChanged;

        private void Awake()
        {
            _available = Mathf.Max(0, _startingAmount);
        }

        public int GetAvailable() => _available;

        public void Add(int delta)
        {
            if (delta == 0) return;
            int next = _available + delta;
            if (next < 0) next = 0;
            if (next == _available) return;
            _available = next;
            PopulationChanged?.Invoke(_available);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (_available < amount) return false;
            _available -= amount;
            PopulationChanged?.Invoke(_available);
            return true;
        }

        public void ResetTo(int weeklyAmount)
        {
            weeklyAmount = Mathf.Max(0, weeklyAmount);
            if (_available == weeklyAmount) return;
            _available = weeklyAmount;
            PopulationChanged?.Invoke(_available);
        }
    }
}

