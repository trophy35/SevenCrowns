namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// UI-facing provider exposing current city building state (which buildings are already constructed).
    /// Implement in Core and discover from UI at runtime.
    /// </summary>
    public interface ICityBuildingStateProvider
    {
        /// <summary>Returns true if the specified building id is already constructed in the current city.</summary>
        bool IsBuilt(string buildingId);
    }
}

