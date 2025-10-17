using System;

namespace SevenCrowns.Map.Farms
{
    /// <summary>
    /// Read-only contract exposing farm nodes present on the strategic map.
    /// </summary>
    public interface IFarmNodeProvider
    {
        event Action<FarmNodeDescriptor> NodeRegistered;
        event Action<FarmNodeDescriptor> NodeUpdated;
        event Action<string> NodeUnregistered;

        System.Collections.Generic.IReadOnlyList<FarmNodeDescriptor> Nodes { get; }

        bool TryGetById(string nodeId, out FarmNodeDescriptor descriptor);
        bool TryGetByCoord(GridCoord coord, out FarmNodeDescriptor descriptor);
    }
}

