using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Instantiates the correct City UI prefab based on the city's faction id passed via CityEnterTransfer.
    /// Place this in the City scene. Assign mappings in Inspector.
    /// Prefabs should have a Canvas as root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityFactionUiController : MonoBehaviour
    {
        [Serializable]
        private struct FactionUi
        {
            public string factionId;
            public GameObject prefab;
        }

        [Header("UI Prefabs by Faction")]
        [SerializeField] private List<FactionUi> _mappings = new List<FactionUi>();
        [Tooltip("Optional parent for instantiated UI. Defaults to this transform when null.")]
        [SerializeField] private Transform _parent;
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private GameObject _instance;

        private void Awake()
        {
            TryShowFromTransfer();
        }

        private void TryShowFromTransfer()
        {
            if (CityEnterTransfer.TryConsumeCityContext(out var cityId, out var factionId))
            {
                if (_debugLogs)
                {
                    Debug.Log($"[CityFactionUI] City='{cityId}' Faction='{factionId}'", this);
                }
                ShowForFaction(factionId);
            }
            else if (_debugLogs)
            {
                Debug.Log("[CityFactionUI] No city context provided; nothing to spawn.", this);
            }
        }

        /// <summary>
        /// Instantiates the UI prefab for the given faction id if mapped.
        /// </summary>
        public void ShowForFaction(string factionId)
        {
            DestroyInstanceIfAny();
            var id = NormalizeId(factionId);
            var prefab = ResolvePrefab(id);
            if (prefab == null)
            {
                if (_debugLogs)
                    Debug.LogWarning($"[CityFactionUI] No UI prefab mapped for faction '{id}'.", this);
                return;
            }
            var parent = _parent != null ? _parent : transform;
            _instance = Instantiate(prefab, parent);
            _instance.name = string.IsNullOrEmpty(id) ? "CityUI" : $"CityUI_{id}";
        }

        /// <summary>
        /// Adds or replaces a mapping entry at runtime. Intended for tests or dynamic setup.
        /// </summary>
        public void AddOrReplaceMapping(string factionId, GameObject prefab)
        {
            if (_mappings == null) _mappings = new List<FactionUi>(2);
            var id = NormalizeId(factionId);
            for (int i = 0; i < _mappings.Count; i++)
            {
                if (string.Equals(NormalizeId(_mappings[i].factionId), id, StringComparison.Ordinal))
                {
                    _mappings[i] = new FactionUi { factionId = id, prefab = prefab };
                    return;
                }
            }
            _mappings.Add(new FactionUi { factionId = id, prefab = prefab });
        }

        private void DestroyInstanceIfAny()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }

        private GameObject ResolvePrefab(string factionId)
        {
            if (_mappings == null || _mappings.Count == 0) return null;
            for (int i = 0; i < _mappings.Count; i++)
            {
                var m = _mappings[i];
                if (string.Equals(NormalizeId(m.factionId), factionId, StringComparison.Ordinal))
                    return m.prefab;
            }
            return null;
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            id = id.Trim();
            return id.Replace(' ', '.');
        }
    }
}
