using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Defines a visual offset for a hero when standing on a specific type of tile.
    /// </summary>
    [CreateAssetMenu(fileName = "TileVisuals", menuName = "SevenCrowns/Map/Tile Visuals")]
    public class TileVisuals : ScriptableObject
    {
        public TerrainType terrainType;
        public Vector3 visualOffset;
    }
}
