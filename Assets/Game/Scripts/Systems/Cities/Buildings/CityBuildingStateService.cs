using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI.Cities.Buildings;

namespace SevenCrowns.Systems.Cities.Buildings
{
    /// <summary>
    /// Simple in-memory implementation of ICityBuildingStateProvider.
    /// Stores which buildings are already constructed in the current city.
    /// Replace or extend with your city progression system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityBuildingStateService : MonoBehaviour, ICityBuildingStateProvider
    {
        [SerializeField]
        private List<string> _builtIds = new List<string>();

        private readonly HashSet<string> _built = new HashSet<string>(StringComparer.Ordinal);

        private void Awake()
        {
            if (_builtIds != null)
            {
                for (int i = 0; i < _builtIds.Count; i++)
                {
                    var id = Normalize(_builtIds[i]);
                    if (!string.IsNullOrEmpty(id)) _built.Add(id);
                }
            }
        }

        public bool IsBuilt(string buildingId)
        {
            var id = Normalize(buildingId);
            if (string.IsNullOrEmpty(id)) return false;
            return _built.Contains(id);
        }

        public void MarkBuilt(string buildingId)
        {
            var id = Normalize(buildingId);
            if (string.IsNullOrEmpty(id)) return;
            _built.Add(id);
        }

        public void ResetAll()
        {
            _built.Clear();
        }

        private static string Normalize(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            id = id.Trim();
            return id.Replace(' ', '.');
        }
    }
}

