using NUnit.Framework;
using UnityEngine;

namespace SevenCrowns.Map.Tests
{
    public class CameraClampUtilityTests
    {
        [Test]
        public void ClampOrthographic_RespectsHalfExtents()
        {
            var rect = new Rect(0f, 0f, 20f, 10f);
            var halfExtents = new Vector2(2f, 2f);
            var desired = new Vector3(19f, 9f, -5f);

            var result = CameraClampUtility.ClampOrthographic(rect, halfExtents, desired);

            Assert.That(result.x, Is.EqualTo(18f).Within(1e-4f));
            Assert.That(result.y, Is.EqualTo(8f).Within(1e-4f));
            Assert.That(result.z, Is.EqualTo(desired.z));
        }

        [Test]
        public void ClampOrthographic_ReturnsCenter_WhenCameraLargerThanBounds()
        {
            var rect = new Rect(10f, 10f, 2f, 2f);
            var halfExtents = new Vector2(2f, 3f);
            var desired = new Vector3(50f, -30f, 0f);

            var result = CameraClampUtility.ClampOrthographic(rect, halfExtents, desired);

            Assert.That(result.x, Is.EqualTo(11f).Within(1e-4f));
            Assert.That(result.y, Is.EqualTo(11f).Within(1e-4f));
            Assert.That(result.z, Is.EqualTo(desired.z));
        }

        [Test]
        public void ClampOrthographic_HandlesDegenerateBounds()
        {
            var rect = new Rect(5f, 5f, 0f, 0f);
            var halfExtents = new Vector2(1f, 1f);
            var desired = new Vector3(12f, 14f, 3f);

            var result = CameraClampUtility.ClampOrthographic(rect, halfExtents, desired);

            Assert.That(result.x, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(result.y, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(result.z, Is.EqualTo(desired.z));
        }
    }
}
