using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map;
using SevenCrowns.Map.Resources;
using SevenCrowns.Systems;

namespace SevenCrowns.Tests.EditMode.Map
{
    public sealed class ClickToMoveControllerCollectTests
    {
        [SetUp]
        public void ClearRegistry()
        {
            var field = typeof(ResourceNodeAuthoring).GetField("s_ByNodeId", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "ResourceNodeAuthoring registry field missing.");

            var map = (Dictionary<string, ResourceNodeAuthoring>)field!.GetValue(null);
            map.Clear();
        }

        [Test]
        public void TryCollectResource_AddsYieldAndDestroysNode()
        {
            var walletGo = new GameObject("WalletService");
            var wallet = walletGo.AddComponent<ResourceWalletService>();

            var controllerGo = new GameObject("ClickToMoveController_Collect");
            var controller = controllerGo.AddComponent<ClickToMoveController>();
            typeof(ClickToMoveController)
                .GetField("_resourceWallet", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(controller, wallet);

            var definition = ScriptableObject.CreateInstance<ResourceDefinition>();

            var nodeGo = new GameObject("ResourceNode") { active = false };
            nodeGo.AddComponent<SpriteRenderer>();
            var authoring = nodeGo.AddComponent<ResourceNodeAuthoring>();
            typeof(ResourceNodeAuthoring)
                .GetField("_nodeId", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(authoring, "resource.node.collect-test");
            nodeGo.SetActive(true);

            Assert.That(ResourceNodeAuthoring.TryGetNode("resource.node.collect-test", out var registered), Is.True);
            Assert.That(registered, Is.SameAs(authoring));

            var descriptor = new ResourceNodeDescriptor(
                "resource.node.collect-test",
                definition,
                default,
                Vector3.zero,
                new GridCoord(2, 3),
                250);

            InvokeCollect(controller, descriptor);

            Assert.That(wallet.GetAmount(definition.ResourceId), Is.EqualTo(250));
            Assert.That(ResourceNodeAuthoring.TryGetNode("resource.node.collect-test", out _), Is.False);
            Assert.That(authoring == null, Is.True);

            Object.DestroyImmediate(controllerGo);
            Object.DestroyImmediate(walletGo);
            if (definition != null)
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static void InvokeCollect(ClickToMoveController controller, ResourceNodeDescriptor descriptor)
        {
            var method = typeof(ClickToMoveController).GetMethod("TryCollectResource", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "TryCollectResource reflection lookup failed.");
            method!.Invoke(controller, new object[] { descriptor });
        }
    }
}
