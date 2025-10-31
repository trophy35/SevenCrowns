using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Cities;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.Tests.EditMode.Systems.Cities
{
    public sealed class CityHudInitializerTests
    {
        [Test]
        public void AddsMissingCoreServices_WhenNonePresent()
        {
            // Arrange
            var go = new GameObject("CityHudInit");
            var init = go.AddComponent<CityHudInitializer>();

            // Act
            // Awake is called automatically when adding component in Edit Mode tests via the Unity test runner
            // But to be explicit and avoid lifecycle coupling, invoke private Awake via reflection
            var m = typeof(CityHudInitializer).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(m, Is.Not.Null);
            m.Invoke(init, null);

            // Assert
            Assert.That(Object.FindObjectOfType<SevenCrowns.Systems.WorldTimeService>(true), Is.Not.Null, "WorldTimeService should exist");
            Assert.That(Object.FindObjectOfType<SevenCrowns.Systems.PopulationService>(true), Is.Not.Null, "PopulationService should exist");
            bool hasWallet = false;
            var behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IResourceWallet) { hasWallet = true; break; }
            }
            Assert.That(hasWallet, Is.True, "IResourceWallet should exist");
        }
    }
}

