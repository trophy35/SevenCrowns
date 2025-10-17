using NUnit.Framework;
using SevenCrowns.Map;
using SevenCrowns.Map.Farms;
using UnityEngine;

namespace SevenCrowns.Tests.EditMode.Map
{
    [TestFixture]
    public sealed class FarmTooltipStateTests
    {
        [Test]
        public void Update_BeforeDelay_ReturnsNoHint()
        {
            var state = new FarmTooltipState(2f);
            var descriptor = CreateDescriptor("farm.a", 20);

            bool changed = state.Update(descriptor, 0.5f, out var hint);

            Assert.That(changed, Is.False);
            Assert.That(hint.HasTooltip, Is.False);
        }

        [Test]
        public void Update_ReachingDelay_ProducesFarmHint()
        {
            var state = new FarmTooltipState(1f);
            var descriptor = CreateDescriptor("farm.a", 40);

            state.Update(descriptor, 0.4f, out _);
            bool changed = state.Update(descriptor, 0.7f, out var hint);

            Assert.That(changed, Is.True);
            Assert.That(hint.HasTooltip, Is.True);
            Assert.That(hint.Kind, Is.EqualTo(WorldTooltipKind.Farm));
            Assert.That(hint.Farm.WeeklyPopulation, Is.EqualTo(40));
            Assert.That(hint.Farm.Descriptor.NodeId, Is.EqualTo("farm.a"));
        }

        [Test]
        public void Update_SwitchingNodes_HidesThenRequiresDelay()
        {
            var state = new FarmTooltipState(0.5f);
            var first = CreateDescriptor("farm.a", 20);
            var second = CreateDescriptor("farm.b", 30);

            bool firstReady = state.Update(first, 0.6f, out var firstHint);
            Assert.That(firstReady, Is.True);
            Assert.That(firstHint.HasTooltip, Is.True);

            bool hideChanged = state.Update(second, 0f, out var hideHint);
            Assert.That(hideChanged, Is.True);
            Assert.That(hideHint.HasTooltip, Is.False);

            bool secondReady = state.Update(second, 0.5f, out var secondHint);
            Assert.That(secondReady, Is.True);
            Assert.That(secondHint.HasTooltip, Is.True);
            Assert.That(secondHint.Farm.WeeklyPopulation, Is.EqualTo(30));
        }

        [Test]
        public void Update_NullAfterShown_HidesTooltip()
        {
            var state = new FarmTooltipState(0.25f);
            var descriptor = CreateDescriptor("farm.a", 25);

            state.Update(descriptor, 0.3f, out _);
            bool changed = state.Update(null, 0f, out var hint);
            Assert.That(changed, Is.True);
            Assert.That(hint.HasTooltip, Is.False);
        }

        [Test]
        public void ForceHide_WhenActive_ReturnsTrue()
        {
            var state = new FarmTooltipState(0.2f);
            var descriptor = CreateDescriptor("farm.a", 15);

            state.Update(descriptor, 0.25f, out _);
            bool changed = state.ForceHide(out var hint);
            Assert.That(changed, Is.True);
            Assert.That(hint.HasTooltip, Is.False);
        }

        private FarmNodeDescriptor CreateDescriptor(string nodeId, int weekly)
        {
            return new FarmNodeDescriptor(nodeId, Vector3.zero, new GridCoord(0, 0), false, string.Empty, weekly);
        }
    }
}

