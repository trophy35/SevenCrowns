using System;
using System.Collections.Generic;

namespace SevenCrowns.Map.FogOfWar
{
    public interface IFogOfWarService
    {
        GridBounds Bounds { get; }
        event Action<GridCoord, FogOfWarState> CellChanged;
        event Action VisibilityCleared;

        FogOfWarState GetState(GridCoord coord);
        bool IsVisible(GridCoord coord);
        bool IsExplored(GridCoord coord);

        void RevealArea(GridCoord center, int radius);
        void RevealCells(IReadOnlyList<GridCoord> cells);
        void ClearTransientVisibility();
    }
}
