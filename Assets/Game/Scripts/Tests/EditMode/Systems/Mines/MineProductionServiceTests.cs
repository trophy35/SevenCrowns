using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems;
using SevenCrowns.Map.Mines;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Tests.EditMode.Systems.Mines
{
    public sealed class MineProductionServiceTests
    {
        [Test]
        public void OwnedMine_Produces_OnAdvanceDay()
        {
            var root = new GameObject("MineProduction_Root");
            var time = root.AddComponent<WorldTimeService>();
            var wallet = root.AddComponent<ResourceWalletService>();
            var mineService = root.AddComponent<MineNodeService>();
            var prod = root.AddComponent<MineProductionService>();
            prod.BindServices(time, mineService, wallet);

            // Register an owned mine producing 2 gold/day
            var desc = new MineNodeDescriptor(
                nodeId: "mine-1",
                worldPosition: Vector3.zero,
                entryCoord: new SevenCrowns.Map.GridCoord(0, 0),
                isOwned: true,
                ownerId: "player",
                resourceId: "resource.gold",
                dailyYield: 2);
            mineService.RegisterOrUpdate(desc);

            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(0));
            // Force lifecycle to ensure OnEnable subscription in EditMode environments
            prod.enabled = false; prod.enabled = true;
            time.AdvanceDay();
            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(2));
        }

        [Test]
        public void UnownedMine_DoesNotProduce()
        {
            var root = new GameObject("MineProduction_Root2");
            var time = root.AddComponent<WorldTimeService>();
            var wallet = root.AddComponent<ResourceWalletService>();
            var mineService = root.AddComponent<MineNodeService>();
            var prod = root.AddComponent<MineProductionService>();
            prod.BindServices(time, mineService, wallet);

            var owned = new MineNodeDescriptor("mine-1", Vector3.zero, new SevenCrowns.Map.GridCoord(0, 0), true, "player", "resource.gold", 3);
            var neutral = new MineNodeDescriptor("mine-2", Vector3.zero, new SevenCrowns.Map.GridCoord(1, 0), false, string.Empty, "resource.gold", 5);
            mineService.RegisterOrUpdate(owned);
            mineService.RegisterOrUpdate(neutral);

            prod.enabled = false; prod.enabled = true;
            time.AdvanceDay();
            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(3));
        }

        [Test]
        public void ZeroYield_DoesNotChangeWallet()
        {
            var root = new GameObject("MineProduction_Root3");
            var time = root.AddComponent<WorldTimeService>();
            var wallet = root.AddComponent<ResourceWalletService>();
            var mineService = root.AddComponent<MineNodeService>();
            var prod = root.AddComponent<MineProductionService>();
            prod.BindServices(time, mineService, wallet);

            var zero = new MineNodeDescriptor("mine-3", Vector3.zero, new SevenCrowns.Map.GridCoord(2, 0), true, "player", "resource.gold", 0);
            mineService.RegisterOrUpdate(zero);

            prod.enabled = false; prod.enabled = true;
            time.AdvanceDay();
            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(0));
        }
    }
}
