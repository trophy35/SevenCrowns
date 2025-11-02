using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.Systems;
using SevenCrowns.UI.Cities.Buildings;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class BuildingListItemViewTests
    {
        private sealed class FakeCityWalletProvider : MonoBehaviour, SevenCrowns.UI.Cities.ICityWalletProvider
        {
            public SevenCrowns.Map.Resources.IResourceWallet Wallet;
            public bool TryGetWallet(out SevenCrowns.Map.Resources.IResourceWallet wallet)
            {
                wallet = Wallet;
                return wallet != null;
            }
        }

        private static (BuildingListItemView view, Button button, Image stateImg, Sprite enabledSprite, Sprite disabledSprite) CreateViewWithBuy()
        {
            var root = new GameObject("BuildingItem");
            var view = root.AddComponent<BuildingListItemView>();

            // Buy button setup
            var btnGo = new GameObject("BuyButton");
            btnGo.transform.SetParent(root.transform, false);
            var btn = btnGo.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();

            var stateGo = new GameObject("State");
            stateGo.transform.SetParent(btnGo.transform, false);
            var stateImg = stateGo.AddComponent<Image>();

            // Assign private fields via reflection
            typeof(BuildingListItemView)
                .GetField("_buyButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, btn);
            typeof(BuildingListItemView)
                .GetField("_buyLabelText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, label);
            typeof(BuildingListItemView)
                .GetField("_buyStateImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, stateImg);

            // Minimal name/desc to avoid nulls
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(root.transform, false);
            var name = nameGo.AddComponent<TextMeshProUGUI>();
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(root.transform, false);
            var desc = descGo.AddComponent<TextMeshProUGUI>();
            typeof(BuildingListItemView)
                .GetField("_nameText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, name);
            typeof(BuildingListItemView)
                .GetField("_descriptionText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, desc);

            // Create dummy sprites
            var texA = new Texture2D(4, 4);
            var texB = new Texture2D(4, 4);
            var enabledSprite = Sprite.Create(texA, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            var disabledSprite = Sprite.Create(texB, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            typeof(BuildingListItemView)
                .GetField("_buyEnabledSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, enabledSprite);
            typeof(BuildingListItemView)
                .GetField("_buyDisabledSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, disabledSprite);

            // Invoke Awake to hook localization internals safely
            var awake = typeof(BuildingListItemView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(view, null);

            return (view, btn, stateImg, enabledSprite, disabledSprite);
        }

        [Test]
        public void BuyButton_Affordable_UsesEnabledSpriteAndGreenTint()
        {
            // Arrange wallet
            var walletGo = new GameObject("Wallet");
            var wallet = walletGo.AddComponent<ResourceWalletService>();
            wallet.Add("resource.gold", 1000);

            // Ensure provider exists so the view binds the right wallet
            var providerGo = new GameObject("CityWalletProvider");
            var provider = providerGo.AddComponent<FakeCityWalletProvider>();
            provider.Wallet = wallet;

            // Arrange view
            var tuple = CreateViewWithBuy();
            var view = tuple.view;
            var button = tuple.button;
            var stateImg = tuple.stateImg;
            var enabledSprite = tuple.enabledSprite;

            var entry = new UiBuildingEntry
            {
                buildingId = "city.market",
                nameEntry = "City.Buildings.Market.Name",
                descriptionEntry = "City.Buildings.Market.Desc",
                costs = new[] { new UiBuildingEntry.ResourceCost { resourceId = "resource.gold", amount = 500 } }
            };

            // Act
            view.GetType().GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(view, null);
            view.Bind(entry, null, null, null);

            // Assert
            Assert.That(stateImg.sprite, Is.EqualTo(enabledSprite));
            // Expect button highlightedColor is greener than red
            var colors = button.colors;
            Assert.That(colors.highlightedColor.g, Is.GreaterThan(colors.highlightedColor.r));

            Object.DestroyImmediate(view.gameObject);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(providerGo);
        }

        [Test]
        public void BuyButton_NotAffordable_UsesDisabledSpriteAndRedTint()
        {
            // Arrange wallet with insufficient funds
            var walletGo = new GameObject("Wallet");
            var wallet = walletGo.AddComponent<ResourceWalletService>();
            wallet.Add("resource.gold", 100);

            var providerGo = new GameObject("CityWalletProvider");
            var provider = providerGo.AddComponent<FakeCityWalletProvider>();
            provider.Wallet = wallet;

            var tuple = CreateViewWithBuy();
            var view = tuple.view;
            var button = tuple.button;
            var stateImg = tuple.stateImg;
            var disabledSprite = tuple.disabledSprite;

            var entry = new UiBuildingEntry
            {
                buildingId = "city.market",
                nameEntry = "City.Buildings.Market.Name",
                descriptionEntry = "City.Buildings.Market.Desc",
                costs = new[] { new UiBuildingEntry.ResourceCost { resourceId = "resource.gold", amount = 500 } }
            };

            // Act
            view.GetType().GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(view, null);
            view.Bind(entry, null, null, null);

            // Assert
            Assert.That(stateImg.sprite, Is.EqualTo(disabledSprite));
            // Expect button highlightedColor is redder than green
            var colors = button.colors;
            Assert.That(colors.highlightedColor.r, Is.GreaterThan(colors.highlightedColor.g));

            Object.DestroyImmediate(view.gameObject);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(providerGo);
        }
    }
}
