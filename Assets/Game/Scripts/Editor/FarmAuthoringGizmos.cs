using System.Reflection;
using UnityEditor;
using UnityEngine;
using SevenCrowns.Map.Farms;
using SevenCrowns.Map;

namespace SevenCrowns.Editor
{
    /// <summary>
    /// Editor gizmos for FarmAuthoring to preview the flag sprite position from entry.
    /// </summary>
    public static class FarmAuthoringGizmos
    {
        [DrawGizmo(GizmoType.Selected)]
        private static void DrawSelected(FarmAuthoring target, GizmoType gizmoType)
        {
            if (target == null) return;

            var t = typeof(FarmAuthoring);
            var grid = (Grid)t.GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
            var provider = (TilemapTileDataProvider)t.GetField("_tileDataProvider", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
            var localOffset = (Vector3)t.GetField("_flagLocalOffset", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

            var world = target.transform.position;
            Vector3 center = world;
            bool haveEntry = false;
            SevenCrowns.Map.GridCoord entryCoord = default;

            if (grid != null)
            {
                var g = (Grid)grid;
                var cell = g.WorldToCell(world);
                center = g.GetCellCenterWorld(cell);
                if (provider != null)
                {
                    bool inBounds;
                    var local = ((TilemapTileDataProvider)provider).WorldToCoordUnclamped(g, center, out inBounds);
                    if (inBounds)
                    {
                        entryCoord = local;
                        haveEntry = true;
                    }
                }
            }

            Gizmos.color = new Color(0.2f, 0.8f, 0.9f, 0.9f);
            Gizmos.DrawWireSphere(center, 0.12f);
            Handles.color = new Color(0.2f, 0.9f, 0.9f, 1f);
            Handles.Label(center + Vector3.up * 0.22f, haveEntry ? $"Entry {entryCoord}" : "Entry (out)");

            if (!haveEntry)
            {
                Handles.color = new Color(1f, 0.6f, 0.2f, 1f);
                Handles.Label(center + Vector3.up * 0.38f, "No provider/grid bounds");
                return;
            }

            // Compute flag world position: entry cell center + local offset
            Vector3 flagCenter = center + localOffset;
            if (grid != null && provider != null && haveEntry)
            {
                var g = (Grid)grid;
                var entryWorld = ((TilemapTileDataProvider)provider).CoordToWorld(g, entryCoord);
                flagCenter = entryWorld + localOffset;
            }
            Gizmos.color = new Color(0.95f, 0.55f, 0.1f, 0.95f);
            Gizmos.DrawWireCube(flagCenter, new Vector3(0.4f, 0.4f, 0.02f));
            Handles.color = new Color(0.95f, 0.75f, 0.2f, 1f);
            Handles.Label(flagCenter + Vector3.up * 0.2f, $"Flag @ {flagCenter.x:F2},{flagCenter.y:F2}");
        }
    }
}

