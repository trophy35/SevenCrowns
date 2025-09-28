using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Provides read-only access to dynamic grid occupancy (e.g., heroes on tiles).
    /// Used by pathfinding and movement to treat occupied tiles as blocked.
    /// </summary>
    public interface IGridOccupancyProvider
    {
        /// <summary>Returns true if any hero occupies the given coord.</summary>
        bool IsOccupied(GridCoord c);

        /// <summary>
        /// Returns true if a hero other than <paramref name="self"/> occupies the given coord.
        /// Passing null for <paramref name="self"/> treats any occupant as a blocker.
        /// </summary>
        bool IsOccupiedByOther(GridCoord c, HeroIdentity self);

        /// <summary>Attempts to get the occupant hero at the given coord.</summary>
        bool TryGetOccupant(GridCoord c, out HeroIdentity hero);
    }
}

