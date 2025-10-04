using System;
using UnityEngine;

namespace SevenCrowns.Systems
{
    [Serializable]
    public struct WorldDate : IEquatable<WorldDate>
    {
        [SerializeField, Min(1)] private int _day;
        [SerializeField, Min(1)] private int _week;
        [SerializeField, Min(1)] private int _month;

        public int Day => _day;
        public int Week => _week;
        public int Month => _month;

        public WorldDate(int day, int week, int month)
        {
            _day = Mathf.Max(1, day);
            _week = Mathf.Max(1, week);
            _month = Mathf.Max(1, month);
        }

        public bool Equals(WorldDate other) => _day == other._day && _week == other._week && _month == other._month;
        public override bool Equals(object obj) => obj is WorldDate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_day, _week, _month);
        public override string ToString() => $"Day {_day}, Week {_week}, Month {_month}";

        public static bool operator ==(WorldDate left, WorldDate right) => left.Equals(right);
        public static bool operator !=(WorldDate left, WorldDate right) => !left.Equals(right);
    }
}
