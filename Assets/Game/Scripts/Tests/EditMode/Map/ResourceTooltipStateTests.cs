using NUnit.Framework;
using SevenCrowns.Map;
using SevenCrowns.Map.Resources;
using UnityEngine;

namespace SevenCrowns.Tests.EditMode.Map
{
    [TestFixture]
    public sealed class ResourceTooltipStateTests
    {
        private ResourceDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<ResourceDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
            }
        }

        [Test]
        public void Update_BeforeDelay_ReturnsNoHint()
        {
            var state = new ResourceTooltipState(2f);
            var descriptor = CreateDescriptor("node.a", 250);

            bool changed = state.Update(descriptor, 0.5f, out var hint);

            Assert.That(changed, Is.False);
            Assert.That(hint.HasTooltip, Is.False);
        }

        [Test]
        public void Update_ReachingDelay_ProducesResourceHint()
        {
            var state = new ResourceTooltipState(1f);
            var descriptor = CreateDescriptor("node.a", 500);

            state.Update(descriptor, 0.4f, out _);
            bool changed = state.Update(descriptor, 0.7f, out var hint);

            Assert.That(changed, Is.True);
            Assert.That(hint.HasTooltip, Is.True);
            Assert.That(hint.Kind, Is.EqualTo(WorldTooltipKind.Resource));
            Assert.That(hint.Resource.Amount, Is.EqualTo(500));
            Assert.That(hint.Resource.Descriptor.NodeId, Is.EqualTo("node.a"));
        }

        [Test]
        public void Update_SwitchingNodes_HidesThenRequiresDelay()
        {
            var state = new ResourceTooltipState(0.5f);
            var first = CreateDescriptor("node.a", 100);
            var second = CreateDescriptor("node.b", 200);

            bool firstReady = state.Update(first, 0.6f, out var firstHint);
            Assert.That(firstReady, Is.True);
            Assert.That(firstHint.HasTooltip, Is.True);

            bool hideChanged = state.Update(second, 0f, out var hideHint);
            Assert.That(hideChanged, Is.True);
            Assert.That(hideHint.HasTooltip, Is.False);

            bool secondReady = state.Update(second, 0.5f, out var secondHint);
            Assert.That(secondReady, Is.True);
            Assert.That(secondHint.HasTooltip, Is.True);
            Assert.That(secondHint.Resource.Amount, Is.EqualTo(200));
        }

        [Test]
        public void Update_NullAfterShown_HidesTooltip()
        {
            var state = new ResourceTooltipState(0.25f);
            var descriptor = CreateDescriptor("node.a", 75);

            state.Update(descriptor, 0.3f, out _);
            bool changed = state.Update(null, 0f, out var hint);

            Assert.That(changed, Is.True);
            Assert.That(hint.HasTooltip, Is.False);
        }

        [Test]
        public void ForceHide_WhenActive_ReturnsTrue()
        {
            var state = new ResourceTooltipState(0.2f);
            var descriptor = CreateDescriptor("node.a", 40);

            state.Update(descriptor, 0.25f, out _);
            bool changed = state.ForceHide(out var hint);

            Assert.That(changed, Is.True);
            Assert.That(hint.HasTooltip, Is.False);
        }

        private ResourceNodeDescriptor CreateDescriptor(string nodeId, int yield)
        {
            var variant = new ResourceVisualVariant("default", null, Vector3.zero, true);
            return new ResourceNodeDescriptor(nodeId, _definition, variant, Vector3.zero, new GridCoord(0, 0), yield);
        }
    }
}
