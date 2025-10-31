using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Cities;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CityFactionUiControllerTests
    {
        [Test]
        public void ShowForFaction_InstantiatesMappedPrefab()
        {
            // Arrange
            var root = new GameObject("Root");
            try
            {
                var ctrl = root.AddComponent<CityFactionUiController>();
                var knightPrefab = new GameObject("KnightUI");
                knightPrefab.AddComponent<Canvas>();
                var deadPrefab = new GameObject("DeadUI");
                deadPrefab.AddComponent<Canvas>();

                ctrl.AddOrReplaceMapping("faction.knight", knightPrefab);
                ctrl.AddOrReplaceMapping("faction.dead", deadPrefab);

                // Act
                ctrl.ShowForFaction("faction.dead");

                // Assert
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                var instance = root.transform.GetChild(0).gameObject;
                Assert.That(instance.name, Does.StartWith("CityUI_faction.dead"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

