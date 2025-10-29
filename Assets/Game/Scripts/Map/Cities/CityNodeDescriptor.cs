using System;

namespace SevenCrowns.Map.Cities
{
    /// <summary>
    /// Snapshot of a city node on the strategic map.
    /// </summary>
    public readonly struct CityNodeDescriptor : IEquatable<CityNodeDescriptor>
    {
        public CityNodeDescriptor(
            string nodeId,
            UnityEngine.Vector3 worldPosition,
            GridCoord? entryCoord,
            bool isOwned,
            string ownerId,
            CityLevel level)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
            WorldPosition = worldPosition;
            EntryCoord = entryCoord;
            IsOwned = isOwned;
            OwnerId = ownerId ?? string.Empty;
            Level = level;
        }

        public string NodeId { get; }
        public UnityEngine.Vector3 WorldPosition { get; }
        public GridCoord? EntryCoord { get; }
        public bool IsOwned { get; }
        public string OwnerId { get; }
        public CityLevel Level { get; }

        public int DailyGoldYield => Level switch
        {
            CityLevel.Village => 100,
            CityLevel.City => 250,
            CityLevel.Fortress => 500,
            CityLevel.Capital => 1000,
            _ => 0
        };

        public bool IsValid => !string.IsNullOrEmpty(NodeId);
        public bool HasEntryCoord => EntryCoord.HasValue;

        public bool Equals(CityNodeDescriptor other)
        {
            return string.Equals(NodeId, other.NodeId, StringComparison.Ordinal)
                   && WorldPosition.Equals(other.WorldPosition)
                   && Nullable.Equals(EntryCoord, other.EntryCoord)
                   && IsOwned == other.IsOwned
                   && string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal)
                   && Level == other.Level;
        }

        public override bool Equals(object obj) => obj is CityNodeDescriptor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NodeId != null ? StringComparer.Ordinal.GetHashCode(NodeId) : 0;
                hash = (hash * 397) ^ WorldPosition.GetHashCode();
                hash = (hash * 397) ^ (EntryCoord.HasValue ? EntryCoord.Value.GetHashCode() : 0);
                hash = (hash * 397) ^ IsOwned.GetHashCode();
                hash = (hash * 397) ^ (OwnerId != null ? StringComparer.Ordinal.GetHashCode(OwnerId) : 0);
                hash = (hash * 397) ^ Level.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CityNodeDescriptor left, CityNodeDescriptor right) => left.Equals(right);
        public static bool operator !=(CityNodeDescriptor left, CityNodeDescriptor right) => !left.Equals(right);
    }
}

