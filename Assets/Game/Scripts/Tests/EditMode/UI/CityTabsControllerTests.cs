using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI.Tabs;

namespace SevenCrowns.Tests.EditMode.UI
{
    public sealed class CityTabsControllerTests
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

            // Manually call Awake to hook localization safely
            var awake = typeof(VerticalTabItemView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(item, null);

            return item;
        }

        [Test]
        public void SelectById_RaisesEvent_And_UpdatesVisuals()
        {
            // Arrange
            var root = new GameObject("TabsRoot");
            var controller = root.AddComponent<VerticalTabsController>();

            var itemA = CreateTab(root.transform, "city.building");
            var itemB = CreateTab(root.transform, "city.recruit");

            var onEnable = typeof(VerticalTabsController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var awake = typeof(VerticalTabsController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(controller, null);

            var received = new List<string>();
            controller.OnSelectionChanged.AddListener(received.Add);

            // Act: enable (auto-select first by default), then select by id
            onEnable!.Invoke(controller, null);
            controller.SelectById("city.recruit");

            // Assert
            Assert.That(controller.SelectedTabId, Is.EqualTo("city.recruit"));
            Assert.That(received.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(received[^1], Is.EqualTo("city.recruit"));

            var focusedColor = new Color32(246, 225, 156, 255);
            var unfocusedColor = new Color32(190, 181, 182, 255);

            Assert.That(itemA.Label.color, Is.EqualTo((Color)unfocusedColor));
            Assert.That(itemA.Icon.color, Is.EqualTo((Color)unfocusedColor));
            Assert.That(itemB.Label.color, Is.EqualTo((Color)focusedColor));
            Assert.That(itemB.Icon.color, Is.EqualTo((Color)focusedColor));

            // Cleanup
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Clicking_Tab_Invokes_Selection()
        {
            // Arrange
            var root = new GameObject("TabsRoot");
            var controller = root.AddComponent<VerticalTabsController>();

            var itemA = CreateTab(root.transform, "city.building");
            var itemB = CreateTab(root.transform, "city.recruit");

            var onEnable = typeof(VerticalTabsController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var awake = typeof(VerticalTabsController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(controller, null);
            onEnable!.Invoke(controller, null);

            // Act: click itemB's button
            itemB.Button.onClick.Invoke();

            // Assert
            Assert.That(controller.SelectedTabId, Is.EqualTo("city.recruit"));

            // Cleanup
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Selecting_Same_Id_Does_Not_Raise_Duplicate_Event()
        {
            // Arrange
            var root = new GameObject("TabsRoot");
            var controller = root.AddComponent<VerticalTabsController>();
            var itemA = CreateTab(root.transform, "city.building");

            var onEnable = typeof(VerticalTabsController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var awake = typeof(VerticalTabsController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake!.Invoke(controller, null);

            var received = 0;
            controller.OnSelectionChanged.AddListener(_ => received++);

            // Act
            onEnable!.Invoke(controller, null); // first selection (auto)
            var initial = controller.SelectedTabId;
            controller.SelectById(initial); // same id

            // Assert: only one event fired (from initial selection)
            Assert.That(received, Is.EqualTo(1));

            // Cleanup
            Object.DestroyImmediate(root);
        }
    }
}

