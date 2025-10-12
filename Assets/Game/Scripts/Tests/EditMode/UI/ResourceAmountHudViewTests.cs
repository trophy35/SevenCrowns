using NUnit.Framework;
using TMPro;
using UnityEngine;
using SevenCrowns.Systems;
using SevenCrowns.UI;

namespace SevenCrowns.Tests.EditMode.UI
{
    public sealed class ResourceAmountHudViewTests
    {
        [Test]
        public void ResourceAmountHudView_Updates_WhenWalletChanges()
        {
            // Arrange
            var walletGo = new GameObject("WalletService");
            var wallet = walletGo.AddComponent<ResourceWalletService>();

            var canvasGo = new GameObject("Canvas");
            var textGo = new GameObject("Value");
            textGo.transform.SetParent(canvasGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();

            var viewGo = new GameObject("ResourceAmountHudView");
            var view = viewGo.AddComponent<ResourceAmountHudView>();

            // Inject wallet and value via reflection to avoid relying on scene discovery in the test
            typeof(ResourceAmountHudView)
                .GetField("_walletBehaviour", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, wallet);
            typeof(ResourceAmountHudView)
                .GetField("_valueText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, tmp);
            typeof(ResourceAmountHudView)
                .GetField("_resourceId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, "resource.gold");

            // Manually invoke lifecycle methods via reflection to avoid Unity editor assertions.
            var awake = typeof(ResourceAmountHudView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(ResourceAmountHudView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(view, null);
            onEnable!.Invoke(view, null);

            // Act
            wallet.Add("resource.gold", 100);

            // Assert
            Assert.That(tmp.text, Is.EqualTo("100"));

            // Cleanup
            Object.DestroyImmediate(viewGo);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(walletGo);
        }
    }
}
