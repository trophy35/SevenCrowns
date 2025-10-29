using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI;

namespace SevenCrowns.Tests.EditMode.UI
{
    public sealed class MainMenuControllerTests
    {
        [Test]
        public void ShowHideToggle_ControlsRootActiveState()
        {
            // Arrange
            var root = new GameObject("MenuRoot");
            var cancelGO = new GameObject("Cancel");
            cancelGO.transform.SetParent(root.transform);
            var button = cancelGO.AddComponent<Button>();

            var host = new GameObject("Host");
            var controller = host.AddComponent<MainMenuController>();

            // Inject private fields via reflection to keep production API clean
            typeof(MainMenuController)
                .GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, root);
            typeof(MainMenuController)
                .GetField("_cancelButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, button);

            // Ensure a known starting state
            controller.Hide();
            Assert.That(root.activeSelf, Is.False);

            // Act + Assert
            controller.Show();
            Assert.That(root.activeSelf, Is.True);

            controller.Toggle();
            Assert.That(root.activeSelf, Is.False);

            controller.Toggle();
            Assert.That(root.activeSelf, Is.True);

            // Cleanup
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CancelButton_Click_HidesMenu()
        {
            // Arrange
            var root = new GameObject("MenuRoot");
            var cancelGO = new GameObject("Cancel");
            cancelGO.transform.SetParent(root.transform);
            var button = cancelGO.AddComponent<Button>();

            var host = new GameObject("Host");
            var controller = host.AddComponent<MainMenuController>();

            typeof(MainMenuController)
                .GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, root);
            typeof(MainMenuController)
                .GetField("_cancelButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, button);

            // Simulate OnEnable wiring
            host.SetActive(true);
            controller.Show();
            Assert.That(root.activeSelf, Is.True);

            // Act: click Cancel
            button.onClick.Invoke();

            // Assert
            Assert.That(root.activeSelf, Is.False);

            // Cleanup
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(root);
        }
    }
}

