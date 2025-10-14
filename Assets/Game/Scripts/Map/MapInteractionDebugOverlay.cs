using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using SevenCrowns.Map.FogOfWar;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Lightweight in-game debug overlay to diagnose click-to-move hover/collect/mine issues in Player builds.
    /// Draws only in Editor or Development builds. Uses reflection to avoid changing runtime APIs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapInteractionDebugOverlay : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _clickToMoveBehaviour; // ClickToMoveController
        [SerializeField] private bool _visible = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F9;

        private ClickToMoveController _ctl;
        private Camera _cam;
        private Grid _grid;
        private TilemapTileDataProvider _provider;
        private object _mineProvider;
        private IFogOfWarService _fog;

        private static readonly FieldInfo f_camera = typeof(ClickToMoveController).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo f_grid = typeof(ClickToMoveController).GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo f_provider = typeof(ClickToMoveController).GetField("_provider", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo f_mineProvider = typeof(ClickToMoveController).GetField("_mineProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo f_moveMode = typeof(ClickToMoveController).GetField("_moveModeEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo f_fog = typeof(ClickToMoveController).GetField("_fog", BindingFlags.Instance | BindingFlags.NonPublic);

        private void Awake()
        {
            if (_clickToMoveBehaviour != null)
                _ctl = _clickToMoveBehaviour as ClickToMoveController;
            if (_ctl == null)
                _ctl = FindObjectOfType<ClickToMoveController>(true);
            RefreshRefs();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) _visible = !_visible;
            if (_ctl == null) return;
            // Refresh occasionally; bindings may appear late in Player builds
            RefreshRefs();
        }

        private void RefreshRefs()
        {
            if (_ctl == null) return;
            _cam = (Camera)f_camera.GetValue(_ctl);
            _grid = (Grid)f_grid.GetValue(_ctl);
            _provider = (TilemapTileDataProvider)f_provider.GetValue(_ctl);
            _mineProvider = f_mineProvider.GetValue(_ctl); // interface type from Mines namespace
            _fog = (IFogOfWarService)f_fog.GetValue(_ctl);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!_visible) return;
            GUI.depth = 0;
            var rect = new Rect(12, 12, 460, 160);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Map Interaction Debug");
            GUILayout.Label($"Camera: {(_cam != null ? _cam.name : "<null>")}");
            GUILayout.Label($"Grid:   {(_grid != null ? _grid.name : "<null>")}");
            GUILayout.Label($"Provider: {(_provider != null ? _provider.name : "<null>")}");
            GUILayout.Label($"MineProvider bound: {(_mineProvider != null)}");

            var mouse = Input.mousePosition;
            bool overUI = UiPointerUtility.IsPointerOverUI(mouse);
            GUILayout.Label($"Pointer: {mouse} | OverUI={overUI}");

            GridCoord hovered = default;
            bool hasHovered = TryGetHoveredCoord(out hovered);
            GUILayout.Label($"HoveredCoord: {(hasHovered ? hovered.ToString() : "<none>")}");

            bool fogVisible = !hasHovered || _fog == null || _fog.IsVisible(hovered) || _fog.IsExplored(hovered) || _fog.Bounds.IsEmpty;
            GUILayout.Label($"Fog Visible: {fogVisible}");

            bool moveMode = _ctl != null && (bool)f_moveMode.GetValue(_ctl);
            GUILayout.Label($"MoveMode: {moveMode}");

            bool mineHere = false;
            if (hasHovered && _mineProvider is SevenCrowns.Map.Mines.IMineNodeProvider mp)
            {
                mineHere = mp.TryGetByCoord(hovered, out var desc) && desc.IsValid;
            }
            GUILayout.Label($"Mine under cursor: {mineHere}");
            GUILayout.EndArea();
        }

        private bool TryGetHoveredCoord(out GridCoord coord)
        {
            coord = default;
            if (_cam == null || _grid == null || _provider == null) return false;
            float depth = Mathf.Abs((_cam.transform.position - _grid.transform.position).z);
            var w = _cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
            bool inBounds;
            coord = _provider.WorldToCoordUnclamped(_grid, w, out inBounds);
            return inBounds;
        }
#endif
    }
}

