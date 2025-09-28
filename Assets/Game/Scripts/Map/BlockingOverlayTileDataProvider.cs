using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// ITileDataProvider decorator that overlays dynamic blocking based on grid occupancy.
    /// If a tile is occupied by another hero (not the excluded one), it is treated as impassable for pathfinding.
    /// </summary>
    public sealed class BlockingOverlayTileDataProvider : ITileDataProvider
    {
        private readonly ITileDataProvider _inner;
        private readonly IGridOccupancyProvider _occupancy;
        private HeroIdentity _excluded;

        public GridBounds Bounds => _inner.Bounds;

        public BlockingOverlayTileDataProvider(ITileDataProvider inner, IGridOccupancyProvider occupancy)
        {
            _inner = inner;
            _occupancy = occupancy;
        }

        /// <summary>Exclude the given hero from occupancy checks (allows its own start tile).</summary>
        public void SetExcluded(HeroIdentity excluded) => _excluded = excluded;

        public bool TryGet(GridCoord c, out TileData data)
        {
            // Query base provider
            if (!_inner.TryGet(c, out data))
                return false;

            // Overlay occupancy as hard block if any other hero occupies the tile
            if (_occupancy != null && _occupancy.IsOccupiedByOther(c, _excluded))
            {
                data = GetSharedImpassable();
            }
            return true;
        }

        private static TileData s_Impassable;
        private static TileData GetSharedImpassable()
        {
            if (s_Impassable == null)
            {
                s_Impassable = ScriptableObject.CreateInstance<TileData>();
                s_Impassable.flags = 0; // not passable
                s_Impassable.enterMask = EnterMask8.None;
                s_Impassable.moveCostCardinal = 1;
                s_Impassable.moveCostDiagonal = 1;
                s_Impassable.terrainType = TerrainType.Mountain; // arbitrary
            }
            return s_Impassable;
        }
    }
}

