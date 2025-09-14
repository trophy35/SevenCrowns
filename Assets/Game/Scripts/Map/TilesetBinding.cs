using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SevenCrowns.Map
{
    /// <summary>
    /// ScriptableObject mapping visual Tile assets to gameplay TileData.
    /// Use this to bake a fast lookup for a Tilemap-backed provider.
    /// </summary>
    [CreateAssetMenu(fileName = "TilesetBinding", menuName = "SevenCrowns/Map/Tileset Binding")]
    public sealed class TilesetBinding : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileBase tile;
            public TileData data;
        }

        [Tooltip("Fallback TileData when a Tile has no explicit binding. Optional.")]
        public TileData defaultTileData;

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        private Dictionary<TileBase, TileData> _map;

        private void OnEnable()
        {
            BuildMap();
        }

        private void BuildMap()
        {
            _map = new Dictionary<TileBase, TileData>(ReferenceEqualityComparer<TileBase>.Instance);
            if (_entries == null) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.tile == null || e.data == null) continue;
                _map[e.tile] = e.data;
            }
        }

        public bool TryResolve(TileBase tile, out TileData data)
        {
            if (_map == null) BuildMap();
            if (tile != null && _map.TryGetValue(tile, out data))
                return true;
            data = defaultTileData;
            return data != null;
        }

        // Reference equality for TileBase keys to avoid slow GetHashCode on ScriptableObjects
        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}

