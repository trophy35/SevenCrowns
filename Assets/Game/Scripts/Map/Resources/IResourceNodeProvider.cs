using System;

namespace SevenCrowns.Map.Resources
{
    /// <summary>
    /// Read-only contract exposing resource nodes present on the strategic map.
    /// </summary>
    public interface IResourceNodeProvider
    {
        event Action<ResourceNodeDescriptor> NodeRegistered;
        event Action<ResourceNodeDescriptor> NodeUpdated;
        event Action<string> NodeUnregistered;

        System.Collections.Generic.IReadOnlyList<ResourceNodeDescriptor> Nodes { get; }

        bool TryGetById(string nodeId, out ResourceNodeDescriptor descriptor);
        bool TryGetByCoord(GridCoord coord, out ResourceNodeDescriptor descriptor);
    }
}
