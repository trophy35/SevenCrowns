using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI.Cities.Buildings;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace SevenCrowns.Systems.Cities.Buildings
{
    /// <summary>
    /// Runtime implementation of ICityBuildingCatalogProvider.
    /// Loads a per-faction JSON file (Addressables TextAsset) describing building entries.
    /// Falls back to a serialized list when JSON is not available.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingCatalogService : MonoBehaviour, ICityBuildingCatalogProvider
    {
        [Header("Source")]
        [SerializeField, Tooltip("Addressables key format for per-faction JSON (TextAsset). Use {0} for faction id.")]
        private string _jsonKeyFormat = "Data/Buildings/{0}.json";
        [SerializeField, Tooltip("When true, normalizes faction id by trimming and replacing spaces with dots.")]
        private bool _normalizeFactionId = true;
        [SerializeField, Tooltip("Fallback entries used when JSON is not available.")]
        private List<UiBuildingEntry> _fallbackEntries = new List<UiBuildingEntry>();
        [SerializeField, Tooltip("If true and the JSON is not in PreloadRegistry, auto-load via Addressables and cache it for subsequent retries.")]
        private bool _autoLoadIfMissing = true;
        [SerializeField, Tooltip("Enable verbose debug logs for catalog resolution and JSON parsing.")]
        private bool _debugLogs = false;

        private readonly Dictionary<string, List<UiBuildingEntry>> _cache = new Dictionary<string, List<UiBuildingEntry>>(StringComparer.Ordinal);

        public bool TryGetBuildingEntries(string factionId, out IReadOnlyList<UiBuildingEntry> entries)
        {
            entries = Array.Empty<UiBuildingEntry>();
            var id = Normalize(factionId);
            if (string.IsNullOrEmpty(id))
                return false;

            if (_cache.TryGetValue(id, out var cached) && cached != null)
            {
                entries = cached;
                if (_debugLogs)
                    Debug.Log($"[BuildingCatalog] Cache hit for faction='{id}'. entries={cached.Count}", this);
                return true;
            }

            // Try Addressables cache first
#if ADDRESSABLES
            var key = string.Format(string.IsNullOrEmpty(_jsonKeyFormat) ? "{0}" : _jsonKeyFormat, id);
            if (_debugLogs) Debug.Log($"[BuildingCatalog] Resolving Addressables key='{key}' for faction='{id}'", this);
            if (SevenCrowns.Systems.PreloadRegistry.TryGet<TextAsset>(key, out var ta) && ta != null)
            {
                if (_debugLogs) Debug.Log($"[BuildingCatalog] Found TextAsset in PreloadRegistry for key='{key}'. Size={ta.text?.Length ?? 0}", this);
                if (TryParse(ta, out var list))
                {
                    _cache[id] = list;
                    entries = list;
                    if (_debugLogs) Debug.Log($"[BuildingCatalog] Parsed {list.Count} entries for faction='{id}'.", this);
                    return true;
                }
                else if (_debugLogs)
                {
                    Debug.LogWarning($"[BuildingCatalog] Failed to parse JSON for key='{key}'.", this);
                }
            }
            else
            {
                if (_autoLoadIfMissing)
                {
                    if (_debugLogs) Debug.Log($"[BuildingCatalog] Auto-loading TextAsset for key='{key}' via Addressables.", this);
                    var h = Addressables.LoadAssetAsync<TextAsset>(key);
                    SevenCrowns.Systems.PreloadRegistry.Register(key, h);
                }
                else if (_debugLogs)
                {
                    Debug.LogWarning($"[BuildingCatalog] No TextAsset found in PreloadRegistry for key='{key}'. Ensure Addressables key matches and is preloaded.", this);
                }
            }
#endif
            // Fallback to serialized entries (shared across factions) when present
            if (_fallbackEntries != null && _fallbackEntries.Count > 0)
            {
                // Shallow copy to avoid external mutation
                var list = new List<UiBuildingEntry>(_fallbackEntries.Count);
                list.AddRange(_fallbackEntries);
                _cache[id] = list;
                entries = list;
                if (_debugLogs) Debug.Log($"[BuildingCatalog] Using fallback entries. count={list.Count}", this);
                return true;
            }
            if (_debugLogs)
                Debug.LogWarning($"[BuildingCatalog] No entries available for faction='{id}'.", this);
            return false;
        }

        private static bool TryParse(TextAsset json, out List<UiBuildingEntry> entries)
        {
            try
            {
                var raw = json != null ? json.text : null;
                if (string.IsNullOrEmpty(raw))
                {
                    entries = null;
                    return false;
                }

                // Strip BOM and invisible chars that may break JsonUtility
                var s = StripBomAndTrim(raw);

                // Try wrapped-object form first when JSON begins with '{'
                if (s.Length > 0 && s[0] == '{')
                {
                    var wrapper = JsonUtility.FromJson<UiBuildingEntryArray>(s);
                    if (wrapper != null && wrapper.entries != null)
                    {
                        entries = new List<UiBuildingEntry>(wrapper.entries);
                        return true;
                    }
                }

                // Try direct array form: wrap into an object container
                var direct = JsonHelper.FromJson<UiBuildingEntry>(s);
                if (direct != null)
                {
                    entries = new List<UiBuildingEntry>(direct);
                    return true;
                }

                // As a last attempt, try the wrapper path even if it wasn't an object initially (in case of leading noise)
                var fallbackWrapper = JsonUtility.FromJson<UiBuildingEntryArray>(s);
                if (fallbackWrapper != null && fallbackWrapper.entries != null)
                {
                    entries = new List<UiBuildingEntry>(fallbackWrapper.entries);
                    return true;
                }
            }
            catch (Exception)
            {
                // ignore and fall back
            }
            entries = null;
            return false;
        }

        private static string StripBomAndTrim(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            // Remove common BOM/ZWSP and trim whitespace
            s = s.TrimStart('\uFEFF', '\u200B', '\u0000', '\u2028', '\u2029', '\ufeff');
            return s.Trim();
        }

        private string Normalize(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId)) return string.Empty;
            if (!_normalizeFactionId) return factionId;
            factionId = factionId.Trim();
            return factionId.Replace(' ', '.');
        }

        [Serializable]
        private sealed class UiBuildingEntryArray
        {
            public UiBuildingEntry[] entries;
        }

        private static class JsonHelper
        {
            public static T[] FromJson<T>(string json)
            {
                // Supports array root by wrapping
                var wrapped = "{ \"items\": " + json + " }";
                var container = JsonUtility.FromJson<Container<T>>(wrapped);
                return container != null ? container.items : null;
            }

            [Serializable]
            private class Container<T>
            {
                public T[] items;
            }
        }
    }
}
