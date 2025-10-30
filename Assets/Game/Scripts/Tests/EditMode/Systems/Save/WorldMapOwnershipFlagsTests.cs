using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Save;

namespace SevenCrowns.Tests.EditMode.Systems.Save
{
    public sealed class WorldMapOwnershipFlagsTests
    {
        [Test]
        public void Apply_OwnedCity_SpawnsFlagViaAuthoring()
        {
            // Arrange a CityAuthoring with a stable node id and pre-set entryCoord
            var cityGo = new GameObject("City");
            var city = cityGo.AddComponent<SevenCrowns.Map.Cities.CityAuthoring>();

            typeof(SevenCrowns.Map.Cities.CityAuthoring)
                .GetField("_nodeId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(city, "city.test.001");
            typeof(SevenCrowns.Map.Cities.CityAuthoring)
                .GetField("_entryCoord", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(city, new SevenCrowns.Map.GridCoord(0, 0));

            // Force OnEnable registration
            city.enabled = true;

            var reader = new WorldMapStateReader();
            var snap = WorldMapSnapshot.CreateEmpty();
            snap.cities.Add(new CityOwnershipSnapshot
            {
                nodeId = "city.test.001",
                owned = true,
                ownerId = string.Empty,
                level = 0
            });

            // Act
            reader.Apply(snap);

            // Assert: flag instance was created on the authoring
            var flagField = typeof(SevenCrowns.Map.Cities.CityAuthoring)
                .GetField("_flagInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var flag = (GameObject)flagField?.GetValue(city);
            Assert.That(flag, Is.Not.Null, "City flag instance was not created on load apply.");

            Object.DestroyImmediate(cityGo);
        }

        [Test]
        public void Apply_OwnedMine_SpawnsFlagViaAuthoring()
        {
            // Arrange a MineAuthoring with a stable node id and pre-set entryCoord
            var mineGo = new GameObject("Mine");
            var mine = mineGo.AddComponent<SevenCrowns.Map.Mines.MineAuthoring>();

            typeof(SevenCrowns.Map.Mines.MineAuthoring)
                .GetField("_nodeId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mine, "mine.test.001");
            typeof(SevenCrowns.Map.Mines.MineAuthoring)
                .GetField("_entryCoord", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mine, new SevenCrowns.Map.GridCoord(0, 0));

            // Force OnEnable registration
            mine.enabled = true;

            var reader = new WorldMapStateReader();
            var snap = WorldMapSnapshot.CreateEmpty();
            snap.mines.Add(new MineOwnershipSnapshot
            {
                nodeId = "mine.test.001",
                owned = true,
                ownerId = string.Empty,
                resourceId = "resource.gold",
                dailyYield = 1
            });

            // Act
            reader.Apply(snap);

            // Assert: flag instance was created on the authoring
            var flagField = typeof(SevenCrowns.Map.Mines.MineAuthoring)
                .GetField("_flagInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var flag = (GameObject)flagField?.GetValue(mine);
            Assert.That(flag, Is.Not.Null, "Mine flag instance was not created on load apply.");

            Object.DestroyImmediate(mineGo);
        }

        [Test]
        public void Apply_OwnedFarm_SpawnsFlagViaAuthoring()
        {
            var farmGo = new GameObject("Farm");
            var farm = farmGo.AddComponent<SevenCrowns.Map.Farms.FarmAuthoring>();

            typeof(SevenCrowns.Map.Farms.FarmAuthoring)
                .GetField("_nodeId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(farm, "farm.test.001");
            typeof(SevenCrowns.Map.Farms.FarmAuthoring)
                .GetField("_entryCoord", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(farm, new SevenCrowns.Map.GridCoord(0, 0));

            // Ensure it registers
            farm.enabled = true;

            var reader = new WorldMapStateReader();
            var snap = WorldMapSnapshot.CreateEmpty();
            snap.farms.Add(new FarmOwnershipSnapshot
            {
                nodeId = "farm.test.001",
                owned = true,
                ownerId = string.Empty,
                weeklyPopulationYield = 10
            });

            reader.Apply(snap);

            var flagField = typeof(SevenCrowns.Map.Farms.FarmAuthoring)
                .GetField("_flagInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var flag = (GameObject)flagField?.GetValue(farm);
            Assert.That(flag, Is.Not.Null, "Farm flag instance was not created on load apply.");

            Object.DestroyImmediate(farmGo);
        }
    }
}
