namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// UI-facing provider to expose the current city's faction id to UI.
    /// Implement in Core (e.g., CityHudInitializer) and discover from UI.
    /// </summary>
    public interface ICityFactionIdProvider
    {
        bool TryGetFactionId(out string factionId);
    }
}

