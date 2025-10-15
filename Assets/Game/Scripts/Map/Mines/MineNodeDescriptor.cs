using System;

namespace SevenCrowns.Map.Mines
{
    /// <summary>
    /// Snapshot of a mine node on the strategic map.
    /// </summary>
    public readonly struct MineNodeDescriptor : IEquatable<MineNodeDescriptor>
    {
        public MineNodeDescriptor(
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
            _resourceId = string.Empty;
            DailyYield = 0;
        }

        public MineNodeDescriptor(
            string nodeId,
            UnityEngine.Vector3 worldPosition,
            GridCoord? entryCoord,
            bool isOwned,
            string ownerId,
            string resourceId,
            int dailyYield)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
            WorldPosition = worldPosition;
            EntryCoord = entryCoord;
            IsOwned = isOwned;
            OwnerId = ownerId ?? string.Empty;
            _resourceId = string.IsNullOrWhiteSpace(resourceId) ? string.Empty : resourceId.Trim();
            DailyYield = dailyYield < 0 ? 0 : dailyYield;
        }

        public string NodeId { get; }
        public UnityEngine.Vector3 WorldPosition { get; }
        public GridCoord? EntryCoord { get; }
        public bool IsOwned { get; }
        public string OwnerId { get; }
        private readonly string _resourceId;
        public string ResourceId => _resourceId ?? string.Empty;
        public int DailyYield { get; }

        public bool IsValid => !string.IsNullOrEmpty(NodeId);
        public bool HasEntryCoord => EntryCoord.HasValue;

        public bool Equals(MineNodeDescriptor other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                   && WorldPosition.Equals(other.WorldPosition)
                   && Nullable.Equals(EntryCoord, other.EntryCoord)
                   && IsOwned == other.IsOwned
                   && string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal)
                   && string.Equals(ResourceId, other.ResourceId, StringComparison.Ordinal)
                   && DailyYield == other.DailyYield;
        }

        public override bool Equals(object obj) => obj is MineNodeDescriptor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NodeId != null ? StringComparer.Ordinal.GetHashCode(NodeId) : 0;
                hash = (hash * 397) ^ WorldPosition.GetHashCode();
                hash = (hash * 397) ^ (EntryCoord.HasValue ? EntryCoord.Value.GetHashCode() : 0);
                hash = (hash * 397) ^ IsOwned.GetHashCode();
                hash = (hash * 397) ^ (OwnerId != null ? StringComparer.Ordinal.GetHashCode(OwnerId) : 0);
                hash = (hash * 397) ^ (ResourceId != null ? StringComparer.Ordinal.GetHashCode(ResourceId) : 0);
                hash = (hash * 397) ^ DailyYield.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(MineNodeDescriptor left, MineNodeDescriptor right) => left.Equals(right);
        public static bool operator !=(MineNodeDescriptor left, MineNodeDescriptor right) => !left.Equals(right);
    }
}
