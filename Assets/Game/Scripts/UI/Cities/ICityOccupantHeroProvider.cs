using System;

namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// UI-facing provider for the current city's occupant hero information.
    /// Implemented by a Systems component in the City scene (e.g., CityHudInitializer).
    /// </summary>
    public interface ICityOccupantHeroProvider
    {
        /// <summary>Returns true if an occupant hero id is available, along with an optional portrait Addressables key.</summary>
        bool TryGetOccupantHero(out string heroId, out string portraitKey);
    }
}

