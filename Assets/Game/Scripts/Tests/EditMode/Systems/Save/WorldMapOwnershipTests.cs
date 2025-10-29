using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Save;
using SevenCrowns.Map;
using SevenCrowns.Map.Cities;
using SevenCrowns.Map.Mines;
using SevenCrowns.Map.Farms;

namespace SevenCrowns.Tests.EditMode.Systems.Save
{
    public sealed class WorldMapOwnershipTests
    {
        [Test]
        public void CaptureAndApply_Ownership_CitiesMinesFarms()
        {
            // Arrange services
            var root = new GameObject("ServicesRoot");
            var citySvc = root.AddComponent<CityNodeService>();
            var mineSvc = root.AddComponent<MineNodeService>();
            var farmSvc = root.AddComponent<FarmNodeService>();

            // Seed nodes
            citySvc.RegisterOrUpdate(new CityNodeDescriptor(
                nodeId: "city.alpha",
                worldPosition: Vector3.zero,
                entryCoord: new GridCoord(5, 5),
                isOwned: true,
                ownerId: "player",
                level: CityLevel.City));

            mineSvc.RegisterOrUpdate(new MineNodeDescriptor(
                nodeId: "mine.gold.01",
                worldPosition: Vector3.zero,
                entryCoord: new GridCoord(2, 1),
                isOwned: false,
                ownerId: string.Empty,
                resourceId: "resource.gold",
                dailyYield: 2));

            farmSvc.RegisterOrUpdate(new FarmNodeDescriptor(
                nodeId: "farm.beta",
                worldPosition: Vector3.zero,
                entryCoord: new GridCoord(9, 3),
                isOwned: true,
                ownerId: "player",
                weeklyPopulationYield: 10));

            var reader = new WorldMapStateReader();
            var snap = reader.Capture();

            // Mutate current state away from snapshot
            citySvc.RegisterOrUpdate(new CityNodeDescriptor(
                nodeId: "city.alpha",
                worldPosition: Vector3.zero,
                entryCoord: new GridCoord(5, 5),
                isOwned: false,
                ownerId: string.Empty,
                level: CityLevel.Village));

            mineSvc.RegisterOrUpdate(new MineNodeDescriptor(
                nodeId: "mine.gold.01",
                worldPosition: Vector3.zero,
                entryCoord: new GridCoord(2, 1),
                isOwned: true,
                ownerId: "enemy",
                resourceId: "resource.gold",
                dailyYield: 1));

            farmSvc.RegisterOrUpdate(new FarmNodeDescriptor(
                nodeId: "farm.beta",
                worldPosition: Vector3.zero,
                entryCoord: new GridCoord(9, 3),
                isOwned: false,
                ownerId: string.Empty,
                weeklyPopulationYield: 5));

            // Act: apply snapshot
            reader.Apply(snap);

            // Assert restored to snapshot values
            Assert.That(citySvc.TryGetById("city.alpha", out var c), Is.True);
            Assert.That(c.IsOwned, Is.True);
            Assert.That(c.OwnerId, Is.EqualTo("player"));
            Assert.That(c.Level, Is.EqualTo(CityLevel.City));

            Assert.That(mineSvc.TryGetById("mine.gold.01", out var m), Is.True);
            Assert.That(m.IsOwned, Is.False);
            Assert.That(m.OwnerId, Is.EqualTo(string.Empty));
            Assert.That(m.ResourceId, Is.EqualTo("resource.gold"));
            Assert.That(m.DailyYield, Is.EqualTo(2));

            Assert.That(farmSvc.TryGetById("farm.beta", out var f), Is.True);
            Assert.That(f.IsOwned, Is.True);
            Assert.That(f.OwnerId, Is.EqualTo("player"));
            Assert.That(f.WeeklyPopulationYield, Is.EqualTo(10));

            Object.DestroyImmediate(root);
        }
    }
}

