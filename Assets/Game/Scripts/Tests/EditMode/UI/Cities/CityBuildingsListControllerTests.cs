using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI.Cities;
using SevenCrowns.UI.Cities.Buildings;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CityBuildingsListControllerTests
    {
        // Fakes for providers
        private sealed class FakeFaction : MonoBehaviour, ICityFactionIdProvider
        {
            public string Id = "faction.humans";
            public bool TryGetFactionId(out string factionId)
            {
                factionId = Id;
                return true;
            }
        }

        private sealed class FakeCatalog : MonoBehaviour, ICityBuildingCatalogProvider
        {
            public List<UiBuildingEntry> Entries = new List<UiBuildingEntry>();
            public bool TryGetBuildingEntries(string factionId, out IReadOnlyList<UiBuildingEntry> entries)
            {
                entries = Entries;
                return true;
            }
        }

        private sealed class FakeState : MonoBehaviour, ICityBuildingStateProvider
        {
            public HashSet<string> Built = new HashSet<string>();
            public bool IsBuilt(string buildingId) => Built.Contains(buildingId);
        }

        private sealed class FakeResearch : MonoBehaviour, IResearchStateProvider
        {
            public HashSet<string> Completed = new HashSet<string>();
            public bool IsCompleted(string researchId) => Completed.Contains(researchId);
        }

        private static BuildingListItemView CreateItemPrefab()
        {
            var root = new GameObject("BuildingItem");
            var cg = root.AddComponent<CanvasGroup>();
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(root.transform, false);
            var icon = iconGo.AddComponent<Image>();
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(root.transform, false);
            var name = nameGo.AddComponent<TextMeshProUGUI>();
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(root.transform, false);
            var desc = descGo.AddComponent<TextMeshProUGUI>();
            var costRoot = new GameObject("Costs");
            costRoot.transform.SetParent(root.transform, false);
            var content = costRoot.transform;

            // Cost pill prefab
            var pillGo = new GameObject("CostPillPrefab");
            var pillText = pillGo.AddComponent<TextMeshProUGUI>();
            var pill = pillGo.AddComponent<CostPillView>();
            typeof(CostPillView)
                .GetField("_amountText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(pill, pillText);

            var view = root.AddComponent<BuildingListItemView>();
            typeof(BuildingListItemView)
                .GetField("_icon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, icon);
            typeof(BuildingListItemView)
                .GetField("_nameText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, name);
            typeof(BuildingListItemView)
                .GetField("_descriptionText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, desc);
            typeof(BuildingListItemView)
                .GetField("_costContainer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, content);
            typeof(BuildingListItemView)
                .GetField("_costPillPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, pill);
            typeof(BuildingListItemView)
                .GetField("_canvasGroup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(view, cg);

            // Manually call Awake to hook localization safely
            var awake = typeof(BuildingListItemView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(view, null);

            // Return as prefab-like instance (the test will Instantiate via controller)
            return view;
        }

        [Test]
        public void Populate_Creates_Items_And_Dims_Locked()
        {
            // Arrange providers
            var go = new GameObject("Providers");
            var faction = go.AddComponent<FakeFaction>();
            var catalog = go.AddComponent<FakeCatalog>();
            var state = go.AddComponent<FakeState>();
            var research = go.AddComponent<FakeResearch>();

            // Two buildings: A requires none, B requires research.r1
            catalog.Entries.Add(new UiBuildingEntry
            {
                buildingId = "city.hall",
                nameEntry = "Name.CityHall",
                descriptionEntry = "Desc.CityHall",
                costs = new[] { new UiBuildingEntry.ResourceCost { resourceId = "resource.gold", amount = 500 } }
            });
            catalog.Entries.Add(new UiBuildingEntry
            {
                buildingId = "city.mage.tower",
                nameEntry = "Name.MageTower",
                descriptionEntry = "Desc.MageTower",
                requiredResearchIds = new[] { "research.magic1" },
                costs = new[] { new UiBuildingEntry.ResourceCost { resourceId = "resource.gold", amount = 1000 } }
            });

            // Research not completed => second item locked
            // Build UI hierarchy
            var contentGo = new GameObject("Content");
            var content = contentGo.transform;
            var controllerGo = new GameObject("BuildingsController");
            var controller = controllerGo.AddComponent<CityBuildingsListController>();

            typeof(CityBuildingsListController)
                .GetField("_content", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(controller, content);
            typeof(CityBuildingsListController)
                .GetField("_itemPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(controller, CreateItemPrefab());

            // Inject providers
            typeof(CityBuildingsListController)
                .GetField("_catalog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(controller, catalog);
            typeof(CityBuildingsListController)
                .GetField("_state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(controller, state);
            typeof(CityBuildingsListController)
                .GetField("_research", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(controller, research);
            typeof(CityBuildingsListController)
                .GetField("_faction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(controller, faction);

            // Act
            var onEnable = typeof(CityBuildingsListController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onEnable!.Invoke(controller, null);

            // Assert: two items spawned
            Assert.That(content.childCount, Is.EqualTo(2));

            // First item not locked => alpha ~ 1
            var item0 = content.GetChild(0).GetComponent<CanvasGroup>();
            Assert.That(item0 != null ? item0.alpha : 1f, Is.EqualTo(1f));

            // Second item locked => alpha ~ 0.5
            var item1 = content.GetChild(1).GetComponent<CanvasGroup>();
            Assert.That(item1 != null ? item1.alpha : 1f, Is.LessThan(0.99f));

            // Cleanup
            Object.DestroyImmediate(controllerGo);
            Object.DestroyImmediate(contentGo);
            Object.DestroyImmediate(go);
        }
    }
}

