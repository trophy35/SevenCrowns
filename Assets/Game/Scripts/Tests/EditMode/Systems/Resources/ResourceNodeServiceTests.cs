using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Tests.EditMode.Systems.Resources
{
    public sealed class ResourceNodeServiceTests
    {
        [Test]
        public void RegisterOrUpdate_NewNode_ShouldAddAndRaiseEvent()
        {
            var go = new GameObject("ResourceNodeServiceTests_Service");
            var service = go.AddComponent<ResourceNodeService>();
            var resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            var variant = new ResourceVisualVariant("pile.small", null, Vector3.zero, true);
            var descriptor = new ResourceNodeDescriptor("node-1", resource, variant, new Vector3(1f, 2f, 0f), new GridCoord(3, 4), 250);

            ResourceNodeDescriptor registeredDescriptor = default;
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
            Assert.That(service.TryGetById("node-1", out var fetched), Is.True);
            Assert.That(fetched, Is.EqualTo(descriptor));

            Object.DestroyImmediate(resource);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RegisterOrUpdate_ExistingNode_ShouldUpdateAndRaiseEvent()
        {
            var go = new GameObject("ResourceNodeServiceTests_Service");
            var service = go.AddComponent<ResourceNodeService>();
            var resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            var variant = new ResourceVisualVariant("pile.small", null, Vector3.zero, true);
            var descriptor = new ResourceNodeDescriptor("node-1", resource, variant, Vector3.zero, new GridCoord(0, 0), 100);
            service.RegisterOrUpdate(descriptor);

            var updatedVariant = new ResourceVisualVariant("pile.large", null, new Vector3(0.1f, 0.2f, 0f), true);
            var updatedDescriptor = new ResourceNodeDescriptor("node-1", resource, updatedVariant, new Vector3(5f, 5f, 0f), new GridCoord(7, 8), 500);

            ResourceNodeDescriptor updatedEventDescriptor = default;
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

            Object.DestroyImmediate(resource);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Unregister_Node_ShouldRemoveAndRaiseEvent()
        {
            var go = new GameObject("ResourceNodeServiceTests_Service");
            var service = go.AddComponent<ResourceNodeService>();
            var resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            var variant = new ResourceVisualVariant("pile.small", null, Vector3.zero, true);
            var descriptor = new ResourceNodeDescriptor("node-1", resource, variant, Vector3.zero, new GridCoord(1, 1), 100);
            service.RegisterOrUpdate(descriptor);

            string removedId = null;
            service.NodeUnregistered += id => removedId = id;

            bool removed = service.Unregister("node-1");

            Assert.That(removed, Is.True);
            Assert.That(removedId, Is.EqualTo("node-1"));
            Assert.That(service.Nodes, Is.Empty);
            Assert.That(service.TryGetById("node-1", out _), Is.False);
            Assert.That(service.TryGetByCoord(new GridCoord(1, 1), out _), Is.False);

            Object.DestroyImmediate(resource);
            Object.DestroyImmediate(go);
        }
    }
}
