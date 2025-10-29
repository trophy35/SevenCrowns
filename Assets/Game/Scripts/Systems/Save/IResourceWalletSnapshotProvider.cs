using System.Collections.Generic;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Optional extension implemented by resource wallet services to expose a snapshot of all amounts.
    /// Used by the save system to capture current wallet state without reflection.
    /// </summary>
    public interface IResourceWalletSnapshotProvider
    {
        /// <summary>Returns a copy snapshot of all resource amounts keyed by resource id.</summary>
        IReadOnlyDictionary<string, int> GetAllAmountsSnapshot();
    }
}

