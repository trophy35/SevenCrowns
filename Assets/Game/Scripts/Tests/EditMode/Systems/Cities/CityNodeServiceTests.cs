using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map.Cities;

namespace SevenCrowns.Tests.EditMode.Systems.Cities
{
    public sealed class CityNodeServiceTests
    {
        [Test]
        public void Register_And_Get_ById_And_ByCoord()
        {
            var go = new GameObject("CityNodeServiceTests_Service");
            var service = go.AddComponent<CityNodeService>();

            var desc = new CityNodeDescriptor("city-1", Vector3.zero, new SevenCrowns.Map.GridCoord(2, 3), false, string.Empty, CityLevel.City);
            bool registered = service.RegisterOrUpdate(desc);
            Assert.That(registered, Is.True);

            Assert.That(service.TryGetById("city-1", out var byId), Is.True);
            Assert.That(byId.NodeId, Is.EqualTo("city-1"));
            Assert.That(byId.EntryCoord.HasValue, Is.True);
            Assert.That(byId.EntryCoord.Value, Is.EqualTo(new SevenCrowns.Map.GridCoord(2, 3)));

            Assert.That(service.TryGetByCoord(new SevenCrowns.Map.GridCoord(2, 3), out var byCoord), Is.True);
            Assert.That(byCoord.NodeId, Is.EqualTo("city-1"));
        }
    }
}

