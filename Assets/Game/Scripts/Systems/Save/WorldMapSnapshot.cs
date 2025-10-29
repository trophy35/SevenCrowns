using System;
using System.Collections.Generic;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Top-level serializable world state container.
    /// Keep fields public for Unity JsonUtility compatibility.
    /// </summary>
    [Serializable]
    public class WorldMapSnapshot
    {
        public List<HeroSnapshot> heroes = new();
        public List<ResourceAmountSnapshot> resources = new();
        public List<CityOwnershipSnapshot> cities = new();
        public List<MineOwnershipSnapshot> mines = new();
        public List<FarmOwnershipSnapshot> farms = new();
        public List<string> collectedResourceNodeIds = new();
        public List<string> remainingResourceNodeIds = new();

        // World time
        public int timeDay;
        public int timeWeek;
        public int timeMonth;

        // Fog of war (grid-sized, row-major states: 0=Unknown,1=Explored,2=Visible)
        public int fogWidth;
        public int fogHeight;
        public byte[] fogStates;

        // Camera
        public float camX;
        public float camY;
        public float camZ;
        public float camSize;

        // Selection
        public string selectedHeroId;

        // Population (weekly pool available). Guarded by populationHasSnapshot flag for version tolerance.
        public bool populationHasSnapshot;
        public int populationAvailable;

        public static WorldMapSnapshot CreateEmpty() => new WorldMapSnapshot();
    }

    [Serializable]
    public class HeroSnapshot
    {
        public string id;
        public int level;
        public int gridX;
        public int gridY;
        public int mpCurrent;
        public int mpMax;
    }

    [Serializable]
    public class ResourceAmountSnapshot
    {
        public string id;
        public int amount;
    }

    [Serializable]
    public class CityOwnershipSnapshot
    {
        public string nodeId;
        public bool owned;
        public string ownerId;
        public int level; // Serialized enum value (e.g., CityLevel)
    }

    [Serializable]
    public class MineOwnershipSnapshot
    {
        public string nodeId;
        public bool owned;
        public string ownerId;
        public string resourceId;
        public int dailyYield;
    }

    [Serializable]
    public class FarmOwnershipSnapshot
    {
        public string nodeId;
        public bool owned;
        public string ownerId;
        public int weeklyPopulationYield;
    }
}
