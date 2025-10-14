using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map.Mines
{
    /// <summary>
    /// Tracks mine nodes present in the active world map and exposes read-only access to other systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MineNodeService : MonoBehaviour, IMineNodeProvider
    {
        [Tooltip("Log warnings when multiple nodes map to the same entry grid coordinate.")]
        [SerializeField] private bool _logCoordinateConflicts = true;

        private readonly List<MineNodeDescriptor> _nodes = new();
        private readonly Dictionary<string, int> _indexById = new(StringComparer.Ordinal);
        private readonly Dictionary<GridCoord, string> _nodeIdByCoord = new();
        private readonly Dictionary<string, GridCoord> _coordByNodeId = new(StringComparer.Ordinal);

        public event Action<MineNodeDescriptor> NodeRegistered;
        public event Action<MineNodeDescriptor> NodeUpdated;
        public event Action<string> NodeUnregistered;

        public IReadOnlyList<MineNodeDescriptor> Nodes => _nodes;

        public bool RegisterOrUpdate(MineNodeDescriptor descriptor)
        {
            if (!descriptor.IsValid)
            {
                Debug.LogWarning($"[MineNodeService] Attempted to register invalid node descriptor (id='{descriptor.NodeId}').", this);
                return false;
            }

            bool exists = _indexById.TryGetValue(descriptor.NodeId, out int index);
            if (exists)
            {
                var previous = _nodes[index];
                UpdateCoordMapping(previous, descriptor);
                _nodes[index] = descriptor;
                NodeUpdated?.Invoke(descriptor);
                return false;
            }

            _indexById[descriptor.NodeId] = _nodes.Count;
            _nodes.Add(descriptor);
            ApplyCoordMapping(descriptor);
            NodeRegistered?.Invoke(descriptor);
            return true;
        }

        public bool Unregister(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || !_indexById.TryGetValue(nodeId, out int index))
            {
                return false;
            }

            var descriptor = _nodes[index];
            RemoveCoordMapping(nodeId, descriptor);

            int lastIndex = _nodes.Count - 1;
            if (index < lastIndex)
            {
                var moved = _nodes[lastIndex];
                _nodes[index] = moved;
                _indexById[moved.NodeId] = index;
            }

            _nodes.RemoveAt(lastIndex);
            _indexById.Remove(nodeId);
            NodeUnregistered?.Invoke(nodeId);
            return true;
        }

        public bool TryGetById(string nodeId, out MineNodeDescriptor descriptor)
        {
            if (!string.IsNullOrWhiteSpace(nodeId) && _indexById.TryGetValue(nodeId, out int index))
            {
                descriptor = _nodes[index];
                return true;
            }

            descriptor = default;
            return false;
        }

        public bool TryGetByCoord(GridCoord coord, out MineNodeDescriptor descriptor)
        {
            if (_nodeIdByCoord.TryGetValue(coord, out var nodeId) && _indexById.TryGetValue(nodeId, out int index))
            {
                descriptor = _nodes[index];
                return true;
            }

            descriptor = default;
            return false;
        }

        private void UpdateCoordMapping(MineNodeDescriptor previous, MineNodeDescriptor current)
        {
            RemoveCoordMapping(previous.NodeId, previous);
            ApplyCoordMapping(current);
        }

        private void ApplyCoordMapping(MineNodeDescriptor descriptor)
        {
            if (!descriptor.HasEntryCoord)
            {
                _coordByNodeId.Remove(descriptor.NodeId);
                return;
            }

            var coord = descriptor.EntryCoord.Value;
            if (_nodeIdByCoord.TryGetValue(coord, out var existingId) && !string.Equals(existingId, descriptor.NodeId, StringComparison.Ordinal))
            {
                if (_logCoordinateConflicts)
                {
                    Debug.LogWarning($"[MineNodeService] Entry coordinate {coord} is already mapped to node '{existingId}'. Overwriting with '{descriptor.NodeId}'.", this);
                }
            }

            _nodeIdByCoord[coord] = descriptor.NodeId;
            _coordByNodeId[descriptor.NodeId] = coord;
        }

        private void RemoveCoordMapping(string nodeId, MineNodeDescriptor descriptor)
        {
            if (descriptor.HasEntryCoord)
            {
                var coord = descriptor.EntryCoord.Value;
                if (_nodeIdByCoord.TryGetValue(coord, out var existingId) && string.Equals(existingId, nodeId, StringComparison.Ordinal))
                {
                    _nodeIdByCoord.Remove(coord);
                }
            }

            _coordByNodeId.Remove(nodeId);
        }
    }
}

