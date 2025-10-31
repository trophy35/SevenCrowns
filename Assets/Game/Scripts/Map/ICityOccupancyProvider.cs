using System;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Tracks which hero (if any) is currently inside a city. Enforces single-occupant rule.
    /// UI/Map can query to gate enter interactions.
    /// </summary>
    public interface ICityOccupancyProvider
    {
        /// <summary>Returns true when any hero is registered as inside the city.</summary>
        bool IsOccupied(string cityId);

        /// <summary>Gets the hero id occupying the city, or empty when none.</summary>
        string GetOccupant(string cityId);

        /// <summary>Attempts to mark the given hero as entering the city. Returns false if already occupied by another hero.</summary>
        bool TryEnter(string cityId, string heroId);

        /// <summary>Clears occupancy for the given city. Returns false if city had no occupant.</summary>
        bool TryLeaveByCity(string cityId);

        /// <summary>Clears occupancy for the given hero (when leaving any city). Returns false if hero was not occupying.</summary>
        bool TryLeaveByHero(string heroId);
    }
}

