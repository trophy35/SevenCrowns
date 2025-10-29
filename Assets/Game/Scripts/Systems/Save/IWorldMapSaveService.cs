using System.Threading.Tasks;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// High-level save/load orchestrator.
    /// Defers state capture/apply to <see cref="IWorldMapStateReader"/> and persistence to <see cref="IWorldMapPersistence"/>.
    /// </summary>
    public interface IWorldMapSaveService
    {
        Task SaveAsync(string slotId);
        Task<bool> LoadAsync(string slotId);
    }
}

