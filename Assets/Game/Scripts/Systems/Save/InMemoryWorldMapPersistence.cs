using System;
using System.Collections.Generic;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// In-memory persistence for unit tests and editor usage.
    /// </summary>
    public sealed class InMemoryWorldMapPersistence : IWorldMapPersistence
    {
        private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public void Save(string slotId, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("slotId is required", nameof(slotId));
            _store[slotId] = data ?? Array.Empty<byte>();
        }

        public bool TryLoad(string slotId, out byte[] data)
        {
            if (string.IsNullOrWhiteSpace(slotId)) { data = null; return false; }
            return _store.TryGetValue(slotId, out data);
        }
    }
}

