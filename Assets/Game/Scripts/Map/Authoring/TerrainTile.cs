using UnityEngine;
using UnityEngine.Tilemaps;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Custom tile that carries a reference to a TileData ScriptableObject.
    /// Create one TerrainTile asset per visual sprite (variant) and bind it to the
    /// appropriate TileData (e.g., Grass, Road, Forest). Paint these in the Tile Palette.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainTile", menuName = "SevenCrowns/Map/Terrain Tile")]
    public sealed class TerrainTile : Tile
    {
        [Tooltip("Gameplay data for this terrain tile (costs, flags, entry mask).")]
        public TileData data;
    }
}

