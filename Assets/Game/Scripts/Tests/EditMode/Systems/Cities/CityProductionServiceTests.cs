using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems;
using SevenCrowns.Map.Cities;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Tests.EditMode.Systems.Cities
{
    public sealed class CityProductionServiceTests
    {
        private sealed class DummyTime : MonoBehaviour, IWorldTimeService
        {
            public WorldDate CurrentDate { get; private set; }
            public event System.Action<WorldDate> DateChanged;
            public void Advance(WorldDate date)
            {
                CurrentDate = date;
                DateChanged?.Invoke(date);
            }
            public void AdvanceDay()
            {
                var d = CurrentDate;
                var next = new WorldDate(d.Day + 1, d.Week, d.Month);
                CurrentDate = next;
                DateChanged?.Invoke(next);
            }
            public void ResetTo(WorldDate date)
            {
                CurrentDate = date;
                DateChanged?.Invoke(date);
            }
        }

        [Test]
        public void OwnedCities_AddDailyGold_ByLevel()
        {
            var root = new GameObject("CityProductionServiceTests_Root");
            var time = root.AddComponent<DummyTime>();
            var citiesGO = new GameObject("Cities");
            var cityService = citiesGO.AddComponent<CityNodeService>();
            var walletGO = new GameObject("Wallet");
            var wallet = walletGO.AddComponent<ResourceWalletService>();

            // Two cities: Village (100), Fortress (500) owned; one neutral City (250) ignored
            var c1 = new CityNodeDescriptor("c1", Vector3.zero, new SevenCrowns.Map.GridCoord(0,0), true, "player", CityLevel.Village);
            var c2 = new CityNodeDescriptor("c2", Vector3.zero, new SevenCrowns.Map.GridCoord(1,0), true, "player", CityLevel.Fortress);
            var c3 = new CityNodeDescriptor("c3", Vector3.zero, new SevenCrowns.Map.GridCoord(2,0), false, string.Empty, CityLevel.City);
            cityService.RegisterOrUpdate(c1);
            cityService.RegisterOrUpdate(c2);
            cityService.RegisterOrUpdate(c3);

            var prod = root.AddComponent<CityProductionService>();
            prod.BindServices(time, cityService, wallet);

            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(0));
            time.Advance(new WorldDate(1,1,1));
            Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(600));
        }
    }
}
