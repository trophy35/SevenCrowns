using System;

namespace SevenCrowns.Map.Cities
{
    /// <summary>
    /// Read-only contract exposing city nodes present on the strategic map.
    /// </summary>
    public interface ICityNodeProvider
    {
        event Action<CityNodeDescriptor> NodeRegistered;
        event Action<CityNodeDescriptor> NodeUpdated;
        event Action<string> NodeUnregistered;

        System.Collections.Generic.IReadOnlyList<CityNodeDescriptor> Nodes { get; }

        bool TryGetById(string nodeId, out CityNodeDescriptor descriptor);
        bool TryGetByCoord(GridCoord coord, out CityNodeDescriptor descriptor);
    }
}

