namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// UI-facing provider for current City context in the City scene.
    /// Implemented in Core and discovered by UI without creating a Core dependency.
    /// </summary>
    public interface ICityNameKeyProvider
    {
        /// <summary>Returns a localization key for the city name if available.</summary>
        bool TryGetCityNameKey(out string cityNameKey);

        /// <summary>Returns the raw city id (e.g., "city.knights.town").</summary>
        bool TryGetCityId(out string cityId);
    }
}

