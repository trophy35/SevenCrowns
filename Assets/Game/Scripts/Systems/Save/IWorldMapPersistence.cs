using System;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Abstracts persistence layer for world map save data.
    /// Keep implementation free of gameplay logic (file, memory, cloud, etc.).
    /// </summary>
    public interface IWorldMapPersistence
    {
        void Save(string slotId, byte[] data);
        bool TryLoad(string slotId, out byte[] data);
    }
}

