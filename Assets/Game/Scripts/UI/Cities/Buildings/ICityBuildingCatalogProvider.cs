using System.Collections.Generic;

namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// UI-facing provider that supplies the list of city buildings available for a given faction.
    /// Implement in Core (e.g., a service loading JSON via Addressables) and discover from UI at runtime.
    /// </summary>
    public interface ICityBuildingCatalogProvider
    {
        /// <summary>
        /// Attempts to get the building entries for the specified faction id.
        /// Returns true when entries are available (possibly an empty list).
        /// </summary>
        bool TryGetBuildingEntries(string factionId, out IReadOnlyList<UiBuildingEntry> entries);
    }
}

