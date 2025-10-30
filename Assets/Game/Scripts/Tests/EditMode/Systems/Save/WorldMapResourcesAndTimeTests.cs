using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Save;
using SevenCrowns.Map.Resources;
using SevenCrowns.Map;
using SevenCrowns.Systems;

namespace SevenCrowns.Tests.EditMode.Systems.Save
{
    public sealed class WorldMapResourcesAndTimeTests
    {
        [Test]
        public void Apply_RemainingResourceIds_UnregistersMissingNodes()
        {
            var root = new GameObject("ResourceRoot");
            var svc = root.AddComponent<ResourceNodeService>();

            // Register two nodes directly in service with a dummy definition
            var def = ScriptableObject.CreateInstance<ResourceDefinition>();
            svc.RegisterOrUpdate(new ResourceNodeDescriptor("res.1", def, default, Vector3.zero, new GridCoord(0,0), 10));
            svc.RegisterOrUpdate(new ResourceNodeDescriptor("res.2", def, default, Vector3.zero, new GridCoord(1,0), 10));

            var reader = new WorldMapStateReader();

            var snap = WorldMapSnapshot.CreateEmpty();
            // Only keep res.1
            snap.remainingResourceNodeIds.Add("res.1");

            reader.Apply(snap);

            Assert.That(svc.TryGetById("res.1", out _), Is.True);
            Assert.That(svc.TryGetById("res.2", out _), Is.False);

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Apply_RemainingResourceIds_Empty_RemovesAllNodes()
        {
            var root = new GameObject("ResourceRoot_All");
            var svc = root.AddComponent<ResourceNodeService>();

            var def = ScriptableObject.CreateInstance<ResourceDefinition>();
            svc.RegisterOrUpdate(new ResourceNodeDescriptor("res.a", def, default, Vector3.zero, new GridCoord(2,0), 5));
            svc.RegisterOrUpdate(new ResourceNodeDescriptor("res.b", def, default, Vector3.zero, new GridCoord(3,0), 7));

            var reader = new WorldMapStateReader();
            var snap = WorldMapSnapshot.CreateEmpty();
            // Authoritative empty set -> remove all
            // remainingResourceNodeIds list exists but has no items

            reader.Apply(snap);

            Assert.That(svc.TryGetById("res.a", out _), Is.False);
            Assert.That(svc.TryGetById("res.b", out _), Is.False);

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CaptureAndApply_WorldTime_RoundTrip()
        {
            var root = new GameObject("TimeRoot");
            var time = root.AddComponent<WorldTimeService>();
            time.ResetTo(new WorldDate(3, 2, 1));

            var reader = new WorldMapStateReader();
            var snap = reader.Capture();

            // change time away from snapshot
            time.ResetTo(new WorldDate(1, 1, 1));
            Assert.That(time.CurrentDate.Day, Is.EqualTo(1));

            reader.Apply(snap);

            Assert.That(time.CurrentDate.Day, Is.EqualTo(3));
            Assert.That(time.CurrentDate.Week, Is.EqualTo(2));
            Assert.That(time.CurrentDate.Month, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void CaptureAndApply_Population_RoundTrip()
        {
            var root = new GameObject("PopulationRoot");
            var pop = root.AddComponent<SevenCrowns.Systems.PopulationService>();
            pop.ResetTo(37);

            var reader = new WorldMapStateReader();
            var snap = reader.Capture();

            // mutate away from snapshot
            pop.ResetTo(5);
            Assert.That(pop.GetAvailable(), Is.EqualTo(5));

            reader.Apply(snap);

            Assert.That(pop.GetAvailable(), Is.EqualTo(37));

            Object.DestroyImmediate(root);
        }
    }
}
