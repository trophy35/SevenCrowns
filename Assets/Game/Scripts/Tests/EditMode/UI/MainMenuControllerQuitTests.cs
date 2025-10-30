using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI;

namespace SevenCrowns.Tests.EditMode.UI
{
    public sealed class MainMenuControllerQuitTests
    {
        private sealed class FakeQuitter : MonoBehaviour, IApplicationQuitter
        {
            public int CallCount { get; private set; }
            public void Quit() => CallCount++;
        }

        [Test]
        public void QuitButton_Click_CallsQuitter_AndHidesMenu()
        {
            // Arrange
            var root = new GameObject("MenuRoot");
            var cancelGO = new GameObject("Cancel");
            cancelGO.transform.SetParent(root.transform);
            cancelGO.AddComponent<Button>();

            var quitGO = new GameObject("Quit");
            quitGO.transform.SetParent(root.transform);
            var quitButton = quitGO.AddComponent<Button>();

            var host = new GameObject("Host");
            var controller = host.AddComponent<MainMenuController>();

            var quitterHost = new GameObject("Quitter");
            var fakeQuitter = quitterHost.AddComponent<FakeQuitter>();

            // Inject private fields via reflection to keep production API clean
            typeof(MainMenuController)
                .GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, root);
            typeof(MainMenuController)
                .GetField("_quitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, quitButton);
            typeof(MainMenuController)
                .GetField("_quitterBehaviour", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, fakeQuitter);

            // Simulate OnEnable wiring
            host.SetActive(true);
            controller.Show();
            Assert.That(root.activeSelf, Is.True);

            // Act: click Quit
            quitButton.onClick.Invoke();

            // Assert: quitter called and menu hidden
            Assert.That(fakeQuitter.CallCount, Is.EqualTo(1));
            Assert.That(root.activeSelf, Is.False);

            // Cleanup
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(quitterHost);
        }
    }
}