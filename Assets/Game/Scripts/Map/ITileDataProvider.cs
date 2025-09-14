using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Read-only provider of TileData for a rectangular grid.
    /// </summary>
    public interface ITileDataProvider
    {
        GridBounds Bounds { get; }
        bool TryGet(GridCoord c, out TileData data);
    }
}

