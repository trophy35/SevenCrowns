using NUnit.Framework;
using SevenCrowns.Systems.Save;

namespace SevenCrowns.Tests.EditMode.Systems.Save
{
    public sealed class WorldMapSaveSkeletonTests
    {
        [Test]
        public void StateReader_Capture_NoHeroes_ReturnsEmpty()
        {
            var reader = new WorldMapStateReader();
            var s = reader.Capture();
            Assert.That(s.heroes.Count, Is.EqualTo(0));
        }

        [Test]
        public void StateReader_Apply_Empty_DoesNotThrow()
        {
            var reader = new WorldMapStateReader();
            Assert.DoesNotThrow(() => reader.Apply(WorldMapSnapshot.CreateEmpty()));
        }

        [Test]
        public void Snapshot_Empty_RoundTripJson()
        {
            var empty = WorldMapSnapshot.CreateEmpty();
            var bytes = JsonWorldMapSerializer.Serialize(empty);
            var back = JsonWorldMapSerializer.Deserialize(bytes);

            Assert.That(back, Is.Not.Null);
            Assert.That(back.heroes.Count, Is.EqualTo(0));
            Assert.That(back.resources.Count, Is.EqualTo(0));
        }

        [Test]
        public void InMemoryPersistence_SaveLoad_RoundTrip()
        {
            var mem = new InMemoryWorldMapPersistence();
            var data = new byte[] { 1, 2, 3 };
            mem.Save("slot1", data);
            Assert.That(mem.TryLoad("slot1", out var loaded), Is.True);
            Assert.That(loaded, Is.EquivalentTo(data));
        }

        private sealed class FakeReader : IWorldMapStateReader
        {
            public WorldMapSnapshot LastApplied { get; private set; }
            public WorldMapSnapshot CaptureResult { get; set; } = WorldMapSnapshot.CreateEmpty();

            public WorldMapSnapshot Capture() => CaptureResult;
            public void Apply(WorldMapSnapshot snapshot) => LastApplied = snapshot;
        }

        [Test]
        public void SaveService_SavesAndLoads_UsesReaderAndPersistence()
        {
            var reader = new FakeReader
            {
                CaptureResult = new WorldMapSnapshot()
            };
            reader.CaptureResult.heroes.Add(new HeroSnapshot { id = "hero.harry", level = 2, gridX = 10, gridY = 5 });

            var mem = new InMemoryWorldMapPersistence();
            var svc = new WorldMapSaveService(reader, mem);

            // Save
            svc.SaveAsync("slotA").GetAwaiter().GetResult();
            Assert.That(mem.TryLoad("slotA", out var _), Is.True);

            // Load
            reader.CaptureResult = WorldMapSnapshot.CreateEmpty(); // ensure load applies stored data, not current capture
            var ok = svc.LoadAsync("slotA").GetAwaiter().GetResult();
            Assert.That(ok, Is.True);
            Assert.That(reader.LastApplied, Is.Not.Null);
            Assert.That(reader.LastApplied.heroes.Count, Is.EqualTo(1));
            Assert.That(reader.LastApplied.heroes[0].id, Is.EqualTo("hero.harry"));
        }

        [Test]
        public void StateReader_CapturesAndApplies_Wallet()
        {
            var go = new UnityEngine.GameObject("Wallet");
            var wallet = go.AddComponent<SevenCrowns.Systems.ResourceWalletService>();

            wallet.Add("resource.gold", 125);
            wallet.Add("resource.wood", 40);

            var reader = new WorldMapStateReader();
            var snap = reader.Capture();

            // change amounts away from snapshot
            wallet.TrySpend("resource.gold", 100);
            wallet.Add("resource.wood", 10);

            reader.Apply(snap);

            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(125));
            Assert.That(wallet.GetAmount("resource.wood"), Is.EqualTo(40));

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
