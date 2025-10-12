using System.Reflection;
using UnityEditor;
using UnityEngine;
using SevenCrowns.Map;

namespace SevenCrowns.Editor
{
    /// <summary>
    /// Editor gizmos for ResourceNodeAuthoring to help diagnose placement and bounds issues.
    /// </summary>
    public static class ResourceNodeAuthoringGizmos
    {
        [DrawGizmo(GizmoType.Selected)]
        private static void DrawSelected(ResourceNodeAuthoring target, GizmoType gizmoType)
        {
            if (target == null) return;

            // Reflect private fields to avoid modifying runtime component API.
            var t = typeof(ResourceNodeAuthoring);
            var grid = (Grid)t.GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
            var provider = (TilemapTileDataProvider)t.GetField("_tileDataProvider", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

            var world = target.transform.position;
            Vector3 center = world;

            if (grid != null)
            {
                var g = (Grid)grid;
                var cell = g.WorldToCell(world);
                center = g.GetCellCenterWorld(cell);
            }

            // Cell center indicator
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(center, 0.12f);

            // Visual offset line from cell center to sprite position
            Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawLine(center, world);

            // Provider-local coord label and in-bounds state if provider and grid are present
            if (provider != null && grid != null)
            {
                var g = (Grid)grid;
                bool inBounds;
                var local = provider.WorldToCoordUnclamped(g, center, out inBounds);
                Handles.color = inBounds ? new Color(0.1f, 0.9f, 0.1f, 1f) : new Color(1f, 0.4f, 0.2f, 1f);
                var label = inBounds ? $"{local}" : $"{local} (out)";
                Handles.Label(center + Vector3.up * 0.22f, label);
            }
            else
            {
                Handles.color = new Color(1f, 0.6f, 0.2f, 1f);
                Handles.Label(center + Vector3.up * 0.22f, "No Grid/Provider");
            }
        }
    }
}

