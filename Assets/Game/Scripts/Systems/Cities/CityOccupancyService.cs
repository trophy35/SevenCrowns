using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Simple in-memory occupancy tracker for city interiors. Ensures at most one hero per city.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityOccupancyService : MonoBehaviour, SevenCrowns.Map.ICityOccupancyProvider
    {
        private readonly Dictionary<string, string> _occupantByCityId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _cityByHeroId = new(StringComparer.Ordinal);

        public bool IsOccupied(string cityId)
        {
            return !string.IsNullOrWhiteSpace(cityId) && _occupantByCityId.ContainsKey(cityId);
        }

        public string GetOccupant(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId)) return string.Empty;
            return _occupantByCityId.TryGetValue(cityId, out var heroId) ? (heroId ?? string.Empty) : string.Empty;
        }

        public bool TryEnter(string cityId, string heroId)
        {
            if (string.IsNullOrWhiteSpace(cityId) || string.IsNullOrWhiteSpace(heroId))
                return false;

            if (_occupantByCityId.TryGetValue(cityId, out var existing))
            {
                // Allow re-enter by the same hero (idempotent)
                return string.Equals(existing, heroId, StringComparison.Ordinal);
            }

            // If hero is already registered in another city, clear that (defensive)
            if (_cityByHeroId.TryGetValue(heroId, out var previousCity))
            {
                _occupantByCityId.Remove(previousCity);
                _cityByHeroId.Remove(heroId);
            }

            _occupantByCityId[cityId] = heroId;
            _cityByHeroId[heroId] = cityId;
            return true;
        }

        public bool TryLeaveByCity(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId)) return false;
            if (!_occupantByCityId.TryGetValue(cityId, out var heroId))
                return false;
            _occupantByCityId.Remove(cityId);
            if (!string.IsNullOrEmpty(heroId)) _cityByHeroId.Remove(heroId);
            return true;
        }

        public bool TryLeaveByHero(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return false;
            if (!_cityByHeroId.TryGetValue(heroId, out var cityId))
                return false;
            _cityByHeroId.Remove(heroId);
            if (!string.IsNullOrEmpty(cityId)) _occupantByCityId.Remove(cityId);
            return true;
        }
    }
}

