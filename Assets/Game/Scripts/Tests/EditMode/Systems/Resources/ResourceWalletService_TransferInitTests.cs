using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems;

namespace SevenCrowns.Tests.EditMode.Systems.Resources
{
    /// <summary>
    /// Verifies that seeding starting resources does not wipe amounts already present
    /// (e.g., applied by City transfer before wallet Awake/initialization).
    /// </summary>
    public sealed class ResourceWalletService_TransferInitTests
    {
        private static object CreateStartingResource(string resourceId, int amount)
        {
            var walletType = typeof(ResourceWalletService);
            var srType = walletType.GetNestedType("StartingResource", BindingFlags.NonPublic);
            Assert.That(srType, Is.Not.Null, "StartingResource type not found via reflection.");

            var sr = Activator.CreateInstance(srType);
            var fId = srType.GetField("resourceId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fAmt = srType.GetField("amount", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(fId, Is.Not.Null);
            Assert.That(fAmt, Is.Not.Null);
            fId.SetValue(sr, resourceId);
            fAmt.SetValue(sr, amount);
            return sr;
        }

        private static void SetStartingResources(ResourceWalletService wallet, params (string id, int amount)[] entries)
        {
            var walletType = typeof(ResourceWalletService);
            var srType = walletType.GetNestedType("StartingResource", BindingFlags.NonPublic);
            var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(srType);
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");
            foreach (var e in entries)
            {
                var sr = CreateStartingResource(e.id, e.amount);
                addMethod.Invoke(list, new[] { sr });
            }

            var field = walletType.GetField("_startingResources", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "_startingResources field not found.");
            field.SetValue(wallet, list);
        }

        private static void InvokeInitialize(ResourceWalletService wallet)
        {
            var walletType = typeof(ResourceWalletService);
            var init = walletType.GetMethod("InitializeFromStartingResources", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(init, Is.Not.Null, "InitializeFromStartingResources method not found.");
            init.Invoke(wallet, null);
        }

        [Test]
        public void InitializeFromStartingResources_ShouldNotWipe_PreExistingAmounts()
        {
            var go = new GameObject("Wallet_TransferInit");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                // Simulate transfer-applied amounts before initialization logic
                wallet.Add("resource.gold", 1000);

                // Seed starting resources (would be configured in Inspector)
                SetStartingResources(wallet, ("resource.gold", 5), ("resource.wood", 2));

                // Invoke initialization routine explicitly
                InvokeInitialize(wallet);

                Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(1005));
                Assert.That(wallet.GetAmount("resource.wood"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InitializeFromStartingResources_WithEmptyStart_ShouldPreserveAmounts()
        {
            var go = new GameObject("Wallet_TransferInit_EmptyStart");
            try
            {
                var wallet = go.AddComponent<ResourceWalletService>();
                wallet.Add("resource.gold", 777);

                // Empty list -> should no-op, not clear
                SetStartingResources(wallet);
                InvokeInitialize(wallet);

                Assert.That(wallet.GetAmount("resource.gold"), Is.EqualTo(777));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}

