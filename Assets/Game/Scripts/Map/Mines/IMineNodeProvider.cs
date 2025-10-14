using System;

namespace SevenCrowns.Map.Mines
{
    /// <summary>
    /// Read-only contract exposing mine nodes present on the strategic map.
    /// </summary>
    public interface IMineNodeProvider
    {
        event Action<MineNodeDescriptor> NodeRegistered;
        event Action<MineNodeDescriptor> NodeUpdated;
        event Action<string> NodeUnregistered;

        System.Collections.Generic.IReadOnlyList<MineNodeDescriptor> Nodes { get; }

        bool TryGetById(string nodeId, out MineNodeDescriptor descriptor);
        bool TryGetByCoord(GridCoord coord, out MineNodeDescriptor descriptor);
    }
}

