using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map;
using SevenCrowns.Map.Farms;

namespace SevenCrowns.Tests.EditMode.Systems.Farms
{
    public sealed class FarmNodeServiceTests
    {
        [Test]
        public void RegisterOrUpdate_NewNode_ShouldAddAndRaiseEvent()
        {
            var go = new GameObject("FarmNodeServiceTests_Service");
            var service = go.AddComponent<FarmNodeService>();
            var descriptor = new FarmNodeDescriptor("farm-1", new Vector3(1f, 2f, 0f), new GridCoord(3, 4), false, string.Empty, 20);

            FarmNodeDescriptor registeredDescriptor = default;
            bool registeredInvoked = false;
            service.NodeRegistered += d =>
            {
                registeredDescriptor = d;
                registeredInvoked = true;
            };

            bool added = service.RegisterOrUpdate(descriptor);

            Assert.That(added, Is.True);
            Assert.That(registeredInvoked, Is.True);
            Assert.That(registeredDescriptor, Is.EqualTo(descriptor));
            Assert.That(service.Nodes, Has.Count.EqualTo(1));
            Assert.That(service.TryGetById("farm-1", out var fetched), Is.True);
            Assert.That(fetched, Is.EqualTo(descriptor));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void RegisterOrUpdate_ExistingNode_ShouldUpdateAndRaiseEvent()
        {
            var go = new GameObject("FarmNodeServiceTests_Service");
            var service = go.AddComponent<FarmNodeService>();
            var descriptor = new FarmNodeDescriptor("farm-1", Vector3.zero, new GridCoord(0, 0), false, string.Empty, 20);
            service.RegisterOrUpdate(descriptor);

            var updatedDescriptor = new FarmNodeDescriptor("farm-1", new Vector3(5f, 5f, 0f), new GridCoord(7, 8), true, "player", 40);

            FarmNodeDescriptor updatedEventDescriptor = default;
            bool updatedInvoked = false;
            service.NodeUpdated += d =>
            {
                updatedEventDescriptor = d;
                updatedInvoked = true;
            };

            bool added = service.RegisterOrUpdate(updatedDescriptor);

            Assert.That(added, Is.False);
            Assert.That(updatedInvoked, Is.True);
            Assert.That(updatedEventDescriptor, Is.EqualTo(updatedDescriptor));
            Assert.That(service.Nodes, Has.Count.EqualTo(1));
            Assert.That(service.TryGetByCoord(new GridCoord(7, 8), out var fetched), Is.True);
            Assert.That(fetched, Is.EqualTo(updatedDescriptor));
            Assert.That(service.TryGetByCoord(new GridCoord(0, 0), out _), Is.False);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Unregister_Node_ShouldRemoveAndRaiseEvent()
        {
            var go = new GameObject("FarmNodeServiceTests_Service");
            var service = go.AddComponent<FarmNodeService>();
            var descriptor = new FarmNodeDescriptor("farm-1", Vector3.zero, new GridCoord(1, 1), false, string.Empty, 20);
            service.RegisterOrUpdate(descriptor);

            string removedId = null;
            service.NodeUnregistered += id => removedId = id;

            bool removed = service.Unregister("farm-1");

            Assert.That(removed, Is.True);
            Assert.That(removedId, Is.EqualTo("farm-1"));
            Assert.That(service.Nodes, Is.Empty);
            Assert.That(service.TryGetById("farm-1", out _), Is.False);
            Assert.That(service.TryGetByCoord(new GridCoord(1, 1), out _), Is.False);

            Object.DestroyImmediate(go);
        }
    }
}

