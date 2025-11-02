using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI.Cities.Buildings;

namespace SevenCrowns.Systems.Cities.Buildings
{
    /// <summary>
    /// Simple in-memory implementation of IResearchStateProvider.
    /// Stores completed research ids relevant for building dependencies.
    /// Replace with your research/progression service.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResearchStateService : MonoBehaviour, IResearchStateProvider
    {
        [SerializeField] private List<string> _completedIds = new List<string>();
        private readonly HashSet<string> _completed = new HashSet<string>(StringComparer.Ordinal);

        private void Awake()
        {
            if (_completedIds != null)
            {
                for (int i = 0; i < _completedIds.Count; i++)
                {
                    var id = Normalize(_completedIds[i]);
                    if (!string.IsNullOrEmpty(id)) _completed.Add(id);
                }
            }
        }

        public bool IsCompleted(string researchId)
        {
            var id = Normalize(researchId);
            if (string.IsNullOrEmpty(id)) return false;
            return _completed.Contains(id);
        }

        public void MarkCompleted(string researchId)
        {
            var id = Normalize(researchId);
            if (string.IsNullOrEmpty(id)) return;
            _completed.Add(id);
        }

        public void ResetAll()
        {
            _completed.Clear();
        }

        private static string Normalize(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            id = id.Trim();
            return id.Replace(' ', '.');
        }
    }
}

