using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map.Mines;

namespace SevenCrowns.Tests.EditMode.Systems.Mines
{
    public sealed class MineNodeServiceTests
    {
        [Test]
        public void RegisterAndQuery_ByIdAndCoord_Works()
        {
            var go = new GameObject("MineNodeServiceTests_Service");
            var service = go.AddComponent<MineNodeService>();

            var desc = new MineNodeDescriptor("mine-1", Vector3.zero, new SevenCrowns.Map.GridCoord(2, 3), false, "");

            bool registered = service.RegisterOrUpdate(desc);
            Assert.That(registered, Is.True);

            Assert.That(service.TryGetById("mine-1", out var byId), Is.True);
            Assert.That(byId.NodeId, Is.EqualTo("mine-1"));

            Assert.That(service.TryGetByCoord(new SevenCrowns.Map.GridCoord(2, 3), out var byCoord), Is.True);
            Assert.That(byCoord.NodeId, Is.EqualTo("mine-1"));
        }

        [Test]
        public void Update_Descriptor_RefreshesMappings()
        {
            var go = new GameObject("MineNodeServiceTests_Service");
            var service = go.AddComponent<MineNodeService>();

            var desc = new MineNodeDescriptor("mine-1", Vector3.zero, new SevenCrowns.Map.GridCoord(1, 1), false, "");
            service.RegisterOrUpdate(desc);

            var updated = new MineNodeDescriptor("mine-1", Vector3.zero, new SevenCrowns.Map.GridCoord(5, 5), true, "player");
            service.RegisterOrUpdate(updated);

            Assert.That(service.TryGetByCoord(new SevenCrowns.Map.GridCoord(5, 5), out var byCoord), Is.True);
            Assert.That(byCoord.IsOwned, Is.True);
        }
    }
}

