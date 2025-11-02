using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// Read-only UI DTO describing a single building entry for listing.
    /// Values should reference Localization String Tables (name/description) and Addressables keys (icon).
    /// </summary>
    [Serializable]
    public sealed class UiBuildingEntry
    {
        [Tooltip("Unique building id (e.g., 'city.barracks'). Used for state/dependencies.")]
        public string buildingId;

        [Header("Localization Keys")]
        [Tooltip("String Table name for the building name (e.g., 'UI.Common').")]
        public string nameTable = "UI.Common";
        [Tooltip("Table entry key for the building name (e.g., 'City.Buildings.Barracks.Name').")]
        public string nameEntry;

        [Tooltip("String Table name for the description (e.g., 'UI.Common').")]
        public string descriptionTable = "UI.Common";
        [Tooltip("Table entry key for the description (e.g., 'City.Buildings.Barracks.Desc').")]
        public string descriptionEntry;

        [Header("Icon")]
        [Tooltip("Addressables key for the icon Sprite (prefer Sprite sub-asset key).")]
        public string iconKey;

        [Header("Costs")] public ResourceCost[] costs = Array.Empty<ResourceCost>();

        [Header("Dependencies")]
        [Tooltip("Building ids that must be built first.")]
        public string[] requiredBuildingIds = Array.Empty<string>();
        [Tooltip("Research ids that must be completed first.")]
        public string[] requiredResearchIds = Array.Empty<string>();

        [Serializable]
        public struct ResourceCost
        {
            public string resourceId; // e.g., resource.gold
            public int amount;
        }

        public IReadOnlyList<ResourceCost> Costs => costs;
        public IReadOnlyList<string> RequiredBuildings => requiredBuildingIds;
        public IReadOnlyList<string> RequiredResearch => requiredResearchIds;

        public bool HasAnyDependency()
        {
            return (requiredBuildingIds != null && requiredBuildingIds.Length > 0)
                   || (requiredResearchIds != null && requiredResearchIds.Length > 0);
        }
    }
}

