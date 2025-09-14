using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// A collection of TileVisuals that provides an efficient lookup for tile-specific offsets.
    /// </summary>
    [CreateAssetMenu(fileName = "TileVisualsSet", menuName = "SevenCrowns/Map/Tile Visuals Set")]
    public class TileVisualsSet : ScriptableObject
    {
        [SerializeField]
        private List<TileVisuals> _visuals;

        private Dictionary<TerrainType, Vector3> _lookup;

        private void OnEnable()
        {
            _lookup = new Dictionary<TerrainType, Vector3>();
            if (_visuals == null) return;

            foreach (var visual in _visuals)
            {
                if (visual != null && !_lookup.ContainsKey(visual.terrainType))
                {
                    _lookup.Add(visual.terrainType, visual.visualOffset);
                }
            }
        }

        /// <summary>
        /// Tries to get a specific visual offset for a given terrain type.
        /// </summary>
        /// <param name="terrainType">The type of the tile to check.</param>
        /// <param name="offset">The output offset if found.</param>
        /// <returns>True if a specific offset was found, false otherwise.</returns>
        public bool TryGetVisualOffset(TerrainType terrainType, out Vector3 offset)
        {
            if (_lookup == null)
            {
                // This can happen in the editor if the script is recompiled.
                OnEnable();
            }
            return _lookup.TryGetValue(terrainType, out offset);
        }
    }
}
