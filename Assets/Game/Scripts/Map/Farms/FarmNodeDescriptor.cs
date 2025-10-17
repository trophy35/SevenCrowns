using System;

namespace SevenCrowns.Map.Farms
{
    /// <summary>
    /// Snapshot of a farm node on the strategic map.
    /// </summary>
    public readonly struct FarmNodeDescriptor : IEquatable<FarmNodeDescriptor>
    {
        public FarmNodeDescriptor(
            string nodeId,
            UnityEngine.Vector3 worldPosition,
            GridCoord? entryCoord,
            bool isOwned,
            string ownerId)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
            WorldPosition = worldPosition;
            EntryCoord = entryCoord;
            IsOwned = isOwned;
            OwnerId = ownerId ?? string.Empty;
            WeeklyPopulationYield = 0;
        }

        public FarmNodeDescriptor(
            string nodeId,
            UnityEngine.Vector3 worldPosition,
            GridCoord? entryCoord,
            bool isOwned,
            string ownerId,
            int weeklyPopulationYield)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
            WorldPosition = worldPosition;
            EntryCoord = entryCoord;
            IsOwned = isOwned;
            OwnerId = ownerId ?? string.Empty;
            WeeklyPopulationYield = weeklyPopulationYield < 0 ? 0 : weeklyPopulationYield;
        }

        public string NodeId { get; }
        public UnityEngine.Vector3 WorldPosition { get; }
        public GridCoord? EntryCoord { get; }
        public bool IsOwned { get; }
        public string OwnerId { get; }
        public int WeeklyPopulationYield { get; }

        public bool IsValid => !string.IsNullOrEmpty(NodeId);
        public bool HasEntryCoord => EntryCoord.HasValue;

        public bool Equals(FarmNodeDescriptor other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                   && WorldPosition.Equals(other.WorldPosition)
                   && Nullable.Equals(EntryCoord, other.EntryCoord)
                   && IsOwned == other.IsOwned
                   && string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal)
                   && WeeklyPopulationYield == other.WeeklyPopulationYield;
        }

        public override bool Equals(object obj) => obj is FarmNodeDescriptor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NodeId != null ? StringComparer.Ordinal.GetHashCode(NodeId) : 0;
                hash = (hash * 397) ^ WorldPosition.GetHashCode();
                hash = (hash * 397) ^ (EntryCoord.HasValue ? EntryCoord.Value.GetHashCode() : 0);
                hash = (hash * 397) ^ IsOwned.GetHashCode();
                hash = (hash * 397) ^ (OwnerId != null ? StringComparer.Ordinal.GetHashCode(OwnerId) : 0);
                hash = (hash * 397) ^ WeeklyPopulationYield.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(FarmNodeDescriptor left, FarmNodeDescriptor right) => left.Equals(right);
        public static bool operator !=(FarmNodeDescriptor left, FarmNodeDescriptor right) => !left.Equals(right);
    }
}

