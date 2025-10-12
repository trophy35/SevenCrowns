using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems;

namespace SevenCrowns.Tests.EditMode.Systems.Resources
{
    public sealed class ResourceWalletService_WoodTests
    {
        [Test]
        public void AddWood_IncreasesAmount_AndRaisesEvent()
        {
            // Arrange
            var go = new GameObject("ResourceWallet_Wood");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                string observedId = null;
                int observedDelta = 0;
                int observedNewAmount = 0;
                wallet.ResourceChanged += change =>
                {
                    observedId = change.ResourceId;
                    observedDelta = change.Delta;
                    observedNewAmount = change.NewAmount;
                };

                // Act
                wallet.Add("resource.wood", 75);

                // Assert
                Assert.That(wallet.GetAmount("resource.wood"), Is.EqualTo(75));
                Assert.That(observedId, Is.EqualTo("resource.wood"));
                Assert.That(observedDelta, Is.EqualTo(75));
                Assert.That(observedNewAmount, Is.EqualTo(75));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TrySpendWood_Succeeds_WhenSufficient()
        {
            // Arrange
            var go = new GameObject("ResourceWallet_Wood_Spend");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                wallet.Add("resource.wood", 120);

                // Act
                bool ok = wallet.TrySpend("resource.wood", 45);

                // Assert
                Assert.That(ok, Is.True);
                Assert.That(wallet.GetAmount("resource.wood"), Is.EqualTo(75));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

