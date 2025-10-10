using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map.Resources;
using SevenCrowns.Systems;

namespace SevenCrowns.Tests.EditMode.Systems.Resources
{
    public sealed class ResourceWalletServiceTests
    {
        [Test]
        public void Add_ShouldIncreaseAmount_AndRaiseEvent()
        {
            var go = new GameObject("ResourceWalletService_Add");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                ResourceChange? received = null;
                wallet.ResourceChanged += change => received = change;

                wallet.Add("resource.gold", 150);

                Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(150));
                Assert.That(received.HasValue, Is.True);
                Assert.That(received.Value.ResourceId, Is.EqualTo("resource.gold"));
                Assert.That(received.Value.Delta, Is.EqualTo(150));
                Assert.That(received.Value.NewAmount, Is.EqualTo(150));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TrySpend_ShouldSucceed_WhenSufficientResources()
        {
            var go = new GameObject("ResourceWalletService_Spend");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                wallet.Add("resource.gold", 200);
                ResourceChange? received = null;
                wallet.ResourceChanged += change => received = change;

                bool success = wallet.TrySpend("resource.gold", 75);

                Assert.That(success, Is.True);
                Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(125));
                Assert.That(received.HasValue, Is.True);
                Assert.That(received.Value.ResourceId, Is.EqualTo("resource.gold"));
                Assert.That(received.Value.Delta, Is.EqualTo(-75));
                Assert.That(received.Value.NewAmount, Is.EqualTo(125));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TrySpend_ShouldFail_WhenInsufficientResources()
        {
            var go = new GameObject("ResourceWalletService_FailSpend");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                wallet.Add("resource.gold", 50);
                bool eventRaised = false;
                wallet.ResourceChanged += _ => eventRaised = true;

                bool success = wallet.TrySpend("resource.gold", 100);

                Assert.That(success, Is.False);
                Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(50));
                Assert.That(eventRaised, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
