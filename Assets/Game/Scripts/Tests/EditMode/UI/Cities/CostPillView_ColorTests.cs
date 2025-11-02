using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI.Cities.Buildings;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CostPillView_ColorTests
    {
        private sealed class FakeWallet : MonoBehaviour, IResourceWallet
        {
            public event System.Action<ResourceChange> ResourceChanged;
            private readonly System.Collections.Generic.Dictionary<string, int> _amounts = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
            public int GetAmount(string resourceId) => string.IsNullOrEmpty(resourceId) ? 0 : (_amounts.TryGetValue(resourceId, out var v) ? v : 0);
            public void Add(string resourceId, int amount)
            {
                if (string.IsNullOrEmpty(resourceId) || amount == 0) return;
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
        public void TintTarget_Color_Follows_Affordability()
        {
            // Arrange UI
            var root = new GameObject("CostPill_Tint");
            var iconGo = new GameObject("Icon"); iconGo.transform.SetParent(root.transform, false); iconGo.AddComponent<Image>();
            var amountGo = new GameObject("Amount"); amountGo.transform.SetParent(root.transform, false); amountGo.AddComponent<TextMeshProUGUI>();
            var stateGo = new GameObject("State"); stateGo.transform.SetParent(root.transform, false); var stateImg = stateGo.AddComponent<Image>();
            var glowGo = new GameObject("Glow"); glowGo.transform.SetParent(root.transform, false); var glowImg = glowGo.AddComponent<Image>();

            // Dummy sprites
            var redTex = new Texture2D(2, 2); var greenTex = new Texture2D(2, 2);
            var redSprite = Sprite.Create(redTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            var greenSprite = Sprite.Create(greenTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);

            var pill = root.AddComponent<CostPillView>();

            // Inject private fields
            typeof(CostPillView).GetField("_stateImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(pill, stateImg);
            typeof(CostPillView).GetField("_notEnoughSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(pill, redSprite);
            typeof(CostPillView).GetField("_enoughSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(pill, greenSprite);
            typeof(CostPillView).GetField("_tintTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(pill, glowImg);

            // Configure colors
            var enoughColorField = typeof(CostPillView).GetField("_enoughColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var notEnoughColorField = typeof(CostPillView).GetField("_notEnoughColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var enoughColor = new Color(0.1f, 0.9f, 0.2f, 1f);
            var notEnoughColor = new Color(0.9f, 0.1f, 0.2f, 1f);
            enoughColorField!.SetValue(pill, enoughColor);
            notEnoughColorField!.SetValue(pill, notEnoughColor);

            // Wallet
            var walletGo = new GameObject("Wallet");
            var wallet = walletGo.AddComponent<FakeWallet>();
            typeof(CostPillView).GetField("_walletBehaviour", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(pill, wallet);

            // Awake/Enable
            var awake = typeof(CostPillView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(CostPillView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(pill, null);
            onEnable!.Invoke(pill, null);

            // Act 1: Not enough
            wallet.Add("resource.wood", 5); // Under cost
            pill.Bind("resource.wood", 10);
            Assert.That(glowImg.color, Is.EqualTo(notEnoughColor));

            // Act 2: Become enough
            wallet.Add("resource.wood", 10); // Event triggers update
            Assert.That(glowImg.color, Is.EqualTo(enoughColor));

            // Cleanup
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(walletGo);
        }
    }
}

