using System;
using System.Collections.Generic;
using NUnit.Framework;
using SevenCrowns.UI.Cities;
using SevenCrowns.UI.Tabs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.Tests.EditMode.UI
{
    public sealed class CityTabsContentControllerTests
    {
        private static VerticalTabItemView CreateTab(Transform parent, string id)
        {
            var go = new GameObject($"Tab_{id}");
            go.transform.SetParent(parent, false);
            var btn = go.AddComponent<Button>();

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var label = textGo.AddComponent<TextMeshProUGUI>();

            var focusGo = new GameObject("FocusLine");
            focusGo.transform.SetParent(go.transform, false);

            var item = go.AddComponent<VerticalTabItemView>();
            item.SetId(id);
            item.SetLabelEntry("UI.Common", "Dummy.Key");

            // Inject private fields via reflection to avoid relying on auto-discovery in tests
            typeof(VerticalTabItemView)
                .GetField("_button", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(item, btn);
            typeof(VerticalTabItemView)
                .GetField("_iconImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(item, icon);
            typeof(VerticalTabItemView)
                .GetField("_label", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(item, label);
            typeof(VerticalTabItemView)
                .GetField("_focusLine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(item, focusGo);

            // Manually call Awake to hook safely
            var awake = typeof(VerticalTabItemView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(item, null);

            return item;
        }

        private static void SetContentsMapping(CityTabsContentController controller, params (string id, GameObject root)[] pairs)
        {
            // Access the private _contents list via reflection and populate it.
            var field = typeof(CityTabsContentController).GetField("_contents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = (List<CityTabsContentController.TabContent>)field!.GetValue(controller);
            list.Clear();
            foreach (var (id, root) in pairs)
            {
                list.Add(new CityTabsContentController.TabContent
                {
                    tabId = id,
                    root = root
                });
            }
        }

        [Test]
        public void Selection_Activates_Corresponding_Content_And_Disables_Others()
        {
            // Arrange
            var root = new GameObject("CityUI");
            var tabs = root.AddComponent<VerticalTabsController>();

            CreateTab(root.transform, "city.building");
            CreateTab(root.transform, "city.recruit");

            var contentGoA = new GameObject("BuildingTab");
            var contentGoB = new GameObject("RecruitTab");
            contentGoA.SetActive(false);
            contentGoB.SetActive(false);

            var contentCtrl = root.AddComponent<CityTabsContentController>();

            // Inject _tabsController
            typeof(CityTabsContentController)
                .GetField("_tabsController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(contentCtrl, tabs);

            // Configure mapping
            SetContentsMapping(contentCtrl,
                ("city.building", contentGoA),
                ("city.recruit", contentGoB));

            // Simulate lifecycles
            var awakeTabs = typeof(VerticalTabsController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnableTabs = typeof(VerticalTabsController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awakeTabs!.Invoke(tabs, null);

            var awakeContent = typeof(CityTabsContentController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnableContent = typeof(CityTabsContentController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awakeContent!.Invoke(contentCtrl, null);

            // Act & Assert: enabling tabs auto-selects first (city.building)
            onEnableTabs!.Invoke(tabs, null);
            onEnableContent!.Invoke(contentCtrl, null);
            Assert.That(contentGoA.activeSelf, Is.True);
            Assert.That(contentGoB.activeSelf, Is.False);

            // Act: switch to recruit
            tabs.SelectById("city.recruit");
            Assert.That(contentGoA.activeSelf, Is.False);
            Assert.That(contentGoB.activeSelf, Is.True);

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(contentGoA);
            UnityEngine.Object.DestroyImmediate(contentGoB);
        }

        [Test]
        public void Unknown_Selection_Deactivates_All()
        {
            // Arrange
            var root = new GameObject("CityUI");
            var tabs = root.AddComponent<VerticalTabsController>();

            CreateTab(root.transform, "city.building");
            CreateTab(root.transform, "city.recruit");

            var contentGoA = new GameObject("BuildingTab");
            var contentGoB = new GameObject("RecruitTab");
            contentGoA.SetActive(true);
            contentGoB.SetActive(true);

            var contentCtrl = root.AddComponent<CityTabsContentController>();
            typeof(CityTabsContentController)
                .GetField("_tabsController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(contentCtrl, tabs);
            SetContentsMapping(contentCtrl,
                ("city.building", contentGoA),
                ("city.recruit", contentGoB));

            // Simulate lifecycles
            var awakeTabs = typeof(VerticalTabsController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnableTabs = typeof(VerticalTabsController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awakeTabs!.Invoke(tabs, null);

            var onEnableContent = typeof(CityTabsContentController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onEnableContent!.Invoke(contentCtrl, null);

            // Act: select unknown id
            tabs.SelectById("city.unknown");

            // Assert
            Assert.That(contentGoA.activeSelf, Is.False);
            Assert.That(contentGoB.activeSelf, Is.False);

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(contentGoA);
            UnityEngine.Object.DestroyImmediate(contentGoB);
        }
    }
}

