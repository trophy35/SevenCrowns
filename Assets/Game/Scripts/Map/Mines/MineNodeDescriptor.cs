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
        }

        public string NodeId { get; }
        public UnityEngine.Vector3 WorldPosition { get; }
        public GridCoord? EntryCoord { get; }
        public bool IsOwned { get; }
        public string OwnerId { get; }

        public bool IsValid => !string.IsNullOrEmpty(NodeId);
        public bool HasEntryCoord => EntryCoord.HasValue;

        public bool Equals(MineNodeDescriptor other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                   && WorldPosition.Equals(other.WorldPosition)
                   && Nullable.Equals(EntryCoord, other.EntryCoord)
                   && IsOwned == other.IsOwned
                   && string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal);
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
                return hash;
            }
        }

        public static bool operator ==(MineNodeDescriptor left, MineNodeDescriptor right) => left.Equals(right);
        public static bool operator !=(MineNodeDescriptor left, MineNodeDescriptor right) => !left.Equals(right);
    }
}

