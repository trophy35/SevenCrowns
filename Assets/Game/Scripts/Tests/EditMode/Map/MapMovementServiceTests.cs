using System;
using System.Collections.Generic;
using NUnit.Framework;
using SevenCrowns.Map;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class MapMovementServiceTests
    {
        [Test]
        public void Initial_Defaults_AreFull()
        {
            var svc = new MapMovementService();
            Assert.AreEqual(240, svc.Max);
            Assert.AreEqual(240, svc.Current);
            Assert.IsFalse(svc.IsExhausted);
        }

        [Test]
        public void TrySpend_Succeeds_WhenEnough()
        {
            var svc = new MapMovementService(240);
            int changedCalls = 0;
            int spentAmount = 0;
            svc.Spent += (amt, cur) => spentAmount = amt;
            svc.Changed += (cur, max) => changedCalls++;

            var ok = svc.TrySpend(30);
            Assert.IsTrue(ok);
            Assert.AreEqual(210, svc.Current);
            Assert.AreEqual(30, spentAmount);
            Assert.AreEqual(1, changedCalls);
        }

        [Test]
        public void TrySpend_Fails_WhenInsufficient()
        {
            var svc = new MapMovementService(10);
            bool ok = svc.TrySpend(11);
            Assert.IsFalse(ok);
            Assert.AreEqual(10, svc.Current);
        }

        [Test]
        public void SpendUpTo_PartialSpend()
        {
            var svc = new MapMovementService(10);
            var spent = svc.SpendUpTo(25);
            Assert.AreEqual(10, spent);
            Assert.AreEqual(0, svc.Current);
        }

        [Test]
        public void Refund_Clamps_To_Max()
        {
            var svc = new MapMovementService(240, 200);
            var refunded = svc.Refund(100);
            Assert.AreEqual(40, refunded);
            Assert.AreEqual(240, svc.Current);
        }

        [Test]
        public void ResetDaily_Refills()
        {
            var svc = new MapMovementService(240);
            svc.TrySpend(150);
            svc.ResetDaily();
            Assert.AreEqual(240, svc.Current);
        }

        [Test]
        public void SetMax_Clamps_And_Optionally_Refills()
        {
            var svc = new MapMovementService(240, 120);
            svc.SetMax(100, refill: false);
            Assert.AreEqual(100, svc.Max);
            Assert.AreEqual(100, svc.Current);

            svc.SetMax(180, refill: true);
            Assert.AreEqual(180, svc.Max);
            Assert.AreEqual(180, svc.Current);
        }

        [Test]
        public void PreviewSequence_Partial_Payable()
        {
            var svc = new MapMovementService(30);
            var costs = (IReadOnlyList<int>)new List<int> { 10, 14, 21 };
            var total = svc.PreviewSequenceCost(costs, out var steps);
            Assert.AreEqual(24, total);
            Assert.AreEqual(2, steps);
            Assert.AreEqual(30, svc.Current);
        }

        [Test]
        public void SpendSequence_Commits()
        {
            var svc = new MapMovementService(30);
            var costs = (IReadOnlyList<int>)new List<int> { 10, 14, 21 };
            var total = svc.SpendSequence(costs, out var steps);
            Assert.AreEqual(24, total);
            Assert.AreEqual(2, steps);
            Assert.AreEqual(6, svc.Current);
        }

        [Test]
        public void Changed_Fires_Once_Per_Mutation()
        {
            var svc = new MapMovementService(100);
            int changes = 0;
            svc.Changed += (_, _) => changes++;
            svc.TrySpend(10);
            svc.Refund(5);
            svc.ResetDaily();
            Assert.AreEqual(3, changes);
        }

        [Test]
        public void Invalid_Inputs_No_Ops()
        {
            var svc = new MapMovementService(50);
            Assert.IsFalse(svc.TrySpend(0));
            Assert.IsFalse(svc.TrySpend(-5));
            Assert.AreEqual(0, svc.Refund(0));
            Assert.AreEqual(50, svc.Current);
        }
    }
}

