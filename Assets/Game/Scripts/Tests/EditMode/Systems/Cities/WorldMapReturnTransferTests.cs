using NUnit.Framework;
using SevenCrowns.Systems.Cities;
using SevenCrowns.Systems.Save;

namespace SevenCrowns.Tests.EditMode.Systems.Cities
{
    public sealed class WorldMapReturnTransferTests
    {
        [Test]
        public void TryConsume_ReturnsFalse_WhenEmpty()
        {
            byte[] data;
            Assert.That(WorldMapReturnTransfer.TryConsume(out data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void SetSnapshot_Then_TryConsume_ReturnsDataOnce()
        {
            var empty = WorldMapSnapshot.CreateEmpty();
            var bytes = JsonWorldMapSerializer.Serialize(empty);

            WorldMapReturnTransfer.SetSnapshot(bytes);
            Assert.That(WorldMapReturnTransfer.TryConsume(out var first), Is.True);
            Assert.That(first, Is.Not.Null);
            Assert.That(first.Length, Is.GreaterThan(0));

            // Second consume returns false
            Assert.That(WorldMapReturnTransfer.TryConsume(out var second), Is.False);
            Assert.That(second, Is.Null);
        }
    }
}

