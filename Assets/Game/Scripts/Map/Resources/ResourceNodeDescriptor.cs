using System;
using SevenCrowns.Map;

namespace SevenCrowns.Map.Resources
{
    /// <summary>
    /// Snapshot of a resource node present on the strategic map.
    /// </summary>
    public readonly struct ResourceNodeDescriptor : IEquatable<ResourceNodeDescriptor>
    {
        public ResourceNodeDescriptor(
            string nodeId,
            ResourceDefinition resource,
            ResourceVisualVariant variant,
            UnityEngine.Vector3 worldPosition,
            GridCoord? gridCoord,
            int baseYield)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
            Resource = resource;
            Variant = variant;
            WorldPosition = worldPosition;
            GridCoord = gridCoord;
            BaseYield = baseYield < 0 ? 0 : baseYield;
        }

        public string NodeId { get; }
        public ResourceDefinition Resource { get; }
        public ResourceVisualVariant Variant { get; }
        public UnityEngine.Vector3 WorldPosition { get; }
        public GridCoord? GridCoord { get; }
        public int BaseYield { get; }
        public bool IsValid => !string.IsNullOrEmpty(NodeId) && Resource != null;
        public bool HasGridCoord => GridCoord.HasValue;

        public bool Equals(ResourceNodeDescriptor other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                   && ReferenceEquals(Resource, other.Resource)
                   && Variant.Equals(other.Variant)
                   && WorldPosition.Equals(other.WorldPosition)
                   && Nullable.Equals(GridCoord, other.GridCoord)
                   && BaseYield == other.BaseYield;
        }

        public override bool Equals(object obj) => obj is ResourceNodeDescriptor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NodeId != null ? StringComparer.Ordinal.GetHashCode(NodeId) : 0;
                hash = (hash * 397) ^ (Resource != null ? Resource.GetHashCode() : 0);
                hash = (hash * 397) ^ Variant.GetHashCode();
                hash = (hash * 397) ^ WorldPosition.GetHashCode();
                hash = (hash * 397) ^ (GridCoord.HasValue ? GridCoord.Value.GetHashCode() : 0);
                hash = (hash * 397) ^ BaseYield.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(ResourceNodeDescriptor left, ResourceNodeDescriptor right) => left.Equals(right);
        public static bool operator !=(ResourceNodeDescriptor left, ResourceNodeDescriptor right) => !left.Equals(right);
    }
}
