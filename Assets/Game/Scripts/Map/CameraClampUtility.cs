using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Provides helper methods for clamping camera positions against world-space bounds.
    /// </summary>
    public static class CameraClampUtility
    {
        /// <summary>
        /// Clamp an orthographic camera centre position to remain within the supplied world rectangle.
        /// </summary>
        /// <param name="worldRect">World-space rectangle describing the playable area.</param>
        /// <param name="halfExtents">Camera half-extents (width, height) in world units.</param>
        /// <param name="desired">Desired camera position.</param>
        /// <returns>Clamped camera position preserving the original Z component.</returns>
        public static Vector3 ClampOrthographic(Rect worldRect, Vector2 halfExtents, Vector3 desired)
        {
            float minX = worldRect.xMin + halfExtents.x;
            float maxX = worldRect.xMax - halfExtents.x;
            float minY = worldRect.yMin + halfExtents.y;
            float maxY = worldRect.yMax - halfExtents.y;

            float clampedX = ClampAxis(desired.x, worldRect.xMin, worldRect.xMax, minX, maxX);
            float clampedY = ClampAxis(desired.y, worldRect.yMin, worldRect.yMax, minY, maxY);

            return new Vector3(clampedX, clampedY, desired.z);
        }

        private static float ClampAxis(float value, float worldMin, float worldMax, float min, float max)
        {
            if (worldMax <= worldMin)
            {
                return worldMin;
            }

            if (min > max)
            {
                return (worldMin + worldMax) * 0.5f;
            }

            return Mathf.Clamp(value, min, max);
        }
    }
}
