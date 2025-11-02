using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI.Cities.Buildings;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CostPillViewTests
    {
        private sealed class FakeWallet : MonoBehaviour, IResourceWallet
        {
            public event System.Action<ResourceChange> ResourceChanged;
            private System.Collections.Generic.Dictionary<string, int> _amounts = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);

            public int GetAmount(string resourceId)
            {
                if (string.IsNullOrEmpty(resourceId)) return 0;
                return _amounts.TryGetValue(resourceId, out var v) ? v : 0;
            }

            public void Add(string resourceId, int amount)
            {
                if (string.IsNullOrEmpty(resourceId)) return;
                var cur = GetAmount(resourceId);
                var next = cur + amount;
                _amounts[resourceId] = next;
                ResourceChanged?.Invoke(new ResourceChange(resourceId, amount, next));
            }

            public bool TrySpend(string resourceId, int amount)
            {
                var cur = GetAmount(resourceId);
                if (cur < amount) return false;
                Add(resourceId, -amount);
                return true;
            }
        }

        [Test]
        public void CostPill_Sets_State_Based_On_Wallet_And_Updates_On_Change()
        {
            // Arrange: wallet
            var walletGo = new GameObject("Wallet");
            var wallet = walletGo.AddComponent<FakeWallet>();
            wallet.Add("resource.wood", 5);

            // Arrange: pill prefab
            var root = new GameObject("CostPill");
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(root.transform, false);
            iconGo.AddComponent<Image>();
            var amountGo = new GameObject("Amount");
            amountGo.transform.SetParent(root.transform, false);
            amountGo.AddComponent<TextMeshProUGUI>();
            var stateGo = new GameObject("State");
            stateGo.transform.SetParent(root.transform, false);
            var stateImg = stateGo.AddComponent<Image>();

            // Create dummy sprites for enough/not-enough states
            var texRed = new Texture2D(2, 2);
            var texGreen = new Texture2D(2, 2);
            var redSprite = Sprite.Create(texRed, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            var greenSprite = Sprite.Create(texGreen, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);

            var pill = root.AddComponent<CostPillView>();
            // Inject private fields via reflection
            typeof(CostPillView)
                .GetField("_walletBehaviour", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(pill, wallet);
            typeof(CostPillView)
                .GetField("_stateImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(pill, stateImg);
            typeof(CostPillView)
                .GetField("_notEnoughSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(pill, redSprite);
            typeof(CostPillView)
                .GetField("_enoughSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(pill, greenSprite);

            // Act: bind a cost higher than current wallet
            var awake = typeof(CostPillView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(CostPillView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(pill, null);
            onEnable!.Invoke(pill, null);
            pill.Bind("resource.wood", 10); // not enough

            // Assert: state shows "not enough" sprite
            Assert.That(stateImg.enabled, Is.True);
            Assert.That(stateImg.sprite, Is.EqualTo(redSprite));

            // Act: add resources to satisfy the cost, event updates pill
            wallet.Add("resource.wood", 10);

            // Assert: state flipped to "enough" sprite
            Assert.That(stateImg.sprite, Is.EqualTo(greenSprite));

            // Cleanup
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(walletGo);
        }
    }
}
