using System;
using System.Threading.Tasks;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Orchestrates capturing world state and writing/reading it via persistence.
    /// </summary>
    public sealed class WorldMapSaveService : IWorldMapSaveService
    {
        private readonly IWorldMapStateReader _reader;
        private readonly IWorldMapPersistence _persistence;

        public WorldMapSaveService(IWorldMapStateReader reader, IWorldMapPersistence persistence)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        public Task SaveAsync(string slotId)
        {
            var snapshot = _reader.Capture();
            var bytes = JsonWorldMapSerializer.Serialize(snapshot);
            _persistence.Save(slotId, bytes);
            return Task.CompletedTask;
        }

        public Task<bool> LoadAsync(string slotId)
        {
            if (!_persistence.TryLoad(slotId, out var bytes))
                return Task.FromResult(false);

            var snapshot = JsonWorldMapSerializer.Deserialize(bytes);
            _reader.Apply(snapshot);
            return Task.FromResult(true);
        }
    }
}

