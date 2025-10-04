using System;
using UnityEngine;

namespace SevenCrowns.Systems
{
    [DisallowMultipleComponent]
    public sealed class WorldTimeService : MonoBehaviour, IWorldTimeService
    {
        [Header("Rules")]
        [SerializeField, Min(1)] private int _daysPerWeek = 7;
        [SerializeField, Min(1)] private int _weeksPerMonth = 4;

        [Header("Starting Date")]
        [SerializeField, Min(1)] private int _startDay = 1;
        [SerializeField, Min(1)] private int _startWeek = 1;
        [SerializeField, Min(1)] private int _startMonth = 1;

        private WorldTimeCounter _counter;

        public event Action<WorldDate> DateChanged;

        public WorldDate CurrentDate => _counter?.CurrentDate ?? new WorldDate(_startDay, _startWeek, _startMonth);

        private void Awake()
        {
            BuildCounter();
        }

        private void OnEnable()
        {
            RaiseCurrentDate();
        }

        private void OnValidate()
        {
            _daysPerWeek = Mathf.Max(1, _daysPerWeek);
            _weeksPerMonth = Mathf.Max(1, _weeksPerMonth);
            _startDay = Mathf.Max(1, _startDay);
            _startWeek = Mathf.Max(1, _startWeek);
            _startMonth = Mathf.Max(1, _startMonth);
        }

        public void AdvanceDay()
        {
            EnsureCounter();
            _counter.AdvanceDay();
            RaiseCurrentDate();
        }

        public void ResetTo(WorldDate date)
        {
            EnsureCounter();
            _counter.Reset(date);
            RaiseCurrentDate();
        }

        private void BuildCounter()
        {
            _counter = new WorldTimeCounter(new WorldDate(_startDay, _startWeek, _startMonth), _daysPerWeek, _weeksPerMonth);
        }

        private void EnsureCounter()
        {
            if (_counter == null)
            {
                BuildCounter();
            }
        }

        private void RaiseCurrentDate()
        {
            DateChanged?.Invoke(CurrentDate);
        }
    }
}
