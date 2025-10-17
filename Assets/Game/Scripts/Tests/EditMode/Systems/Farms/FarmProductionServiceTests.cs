using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems;
using SevenCrowns.Map.Farms;

namespace SevenCrowns.Tests.EditMode.Systems.Farms
{
    public sealed class FarmProductionServiceTests
    {
        [Test]
        public void WeekStart_SetsPopulationSumOfOwnedFarms()
        {
            var root = new GameObject("FarmProduction_Root");
            var time = root.AddComponent<WorldTimeService>();
            var farms = root.AddComponent<FarmNodeService>();
            var pop = root.AddComponent<PopulationService>();
            var prod = root.AddComponent<FarmProductionService>();
            prod.BindServices(time, farms, pop);

            farms.RegisterOrUpdate(new FarmNodeDescriptor("farm-1", Vector3.zero, new SevenCrowns.Map.GridCoord(0, 0), true, "player", 20));
            farms.RegisterOrUpdate(new FarmNodeDescriptor("farm-2", Vector3.zero, new SevenCrowns.Map.GridCoord(1, 0), true, "player", 40));

            // Force lifecycle to ensure OnEnable subscription in EditMode environments
            prod.enabled = false; prod.enabled = true;
            time.AdvanceDay();

            // Production applies on first observed date because _hasProcessedDate is false -> treat as week boundary
            Assert.That(pop.GetAvailable(), Is.EqualTo(60));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MidWeek_CaptureDoesNotAffectUntilNextWeek()
        {
            var root = new GameObject("FarmProduction_Root2");
            var time = root.AddComponent<WorldTimeService>();
            var farms = root.AddComponent<FarmNodeService>();
            var pop = root.AddComponent<PopulationService>();
            var prod = root.AddComponent<FarmProductionService>();
            prod.BindServices(time, farms, pop);

            farms.RegisterOrUpdate(new FarmNodeDescriptor("farm-1", Vector3.zero, new SevenCrowns.Map.GridCoord(0, 0), true, "player", 20));

            prod.enabled = false; prod.enabled = true;
            time.AdvanceDay();
            Assert.That(pop.GetAvailable(), Is.EqualTo(20));

            // Capture a new farm mid-week
            farms.RegisterOrUpdate(new FarmNodeDescriptor("farm-2", Vector3.zero, new SevenCrowns.Map.GridCoord(1, 0), true, "player", 40));
            // Advance day within the same week -> should not change population
            time.AdvanceDay();
            Assert.That(pop.GetAvailable(), Is.EqualTo(20));

            // Simulate start of new week: advance enough days to change week index
            for (int i = 0; i < 7; i++) time.AdvanceDay();
            Assert.That(pop.GetAvailable(), Is.EqualTo(60));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void WeeklyReset_DiscardsRemainder()
        {
            var root = new GameObject("FarmProduction_Root3");
            var time = root.AddComponent<WorldTimeService>();
            var farms = root.AddComponent<FarmNodeService>();
            var pop = root.AddComponent<PopulationService>();
            var prod = root.AddComponent<FarmProductionService>();
            prod.BindServices(time, farms, pop);

            farms.RegisterOrUpdate(new FarmNodeDescriptor("farm-1", Vector3.zero, new SevenCrowns.Map.GridCoord(0, 0), true, "player", 30));

            prod.enabled = false; prod.enabled = true;
            time.AdvanceDay();
            Assert.That(pop.GetAvailable(), Is.EqualTo(30));

            // Spend some population
            Assert.That(pop.TrySpend(10), Is.True);
            Assert.That(pop.GetAvailable(), Is.EqualTo(20));

            // New week should reset to 30 (not 20)
            for (int i = 0; i < 7; i++) time.AdvanceDay();
            Assert.That(pop.GetAvailable(), Is.EqualTo(30));

            Object.DestroyImmediate(root);
        }
    }
}

