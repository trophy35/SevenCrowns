using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Bakes a TileData array from a Tilemap using a TilesetBinding for fast pathfinding lookups.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TilemapTileDataProvider : MonoBehaviour, ITileDataProvider
    {
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private TilesetBinding _binding;
        [Header("Overlay (optional)")]
        [SerializeField] private Tilemap _overlayTilemap; // e.g., roads
        [SerializeField] private TilesetBinding _overlayBinding;
        [Tooltip("If true, overlay is ignored on impassable ground (e.g., water/mountain).")]
        [SerializeField] private bool _overlayRequiresGroundPassable = true;
        [Header("Bounds")]
        [Tooltip("Expands baked bounds around the painted ground area (in cells) without requiring tiles.")]
        [Min(0)]
        [SerializeField] private int _boundsPadding = 0;

        private GridBounds _bounds;
        private TileData[] _data;
        private int _w, _h, _ox, _oy;

        public GridBounds Bounds => _bounds;
        public Grid GroundGrid => _groundTilemap != null ? _groundTilemap.layoutGrid : null;

        private void Awake()
        {
            Bake();
        }

        public void Bake()
        {
            if (_groundTilemap == null)
                throw new InvalidOperationException("TilemapTileDataProvider requires a Ground Tilemap reference.");
            if (_binding == null)
                throw new InvalidOperationException("TilemapTileDataProvider requires a TilesetBinding.");

            var origin = _groundTilemap.origin; // cell coordinates from painted bounds
            var size = _groundTilemap.size;

            // Expand bounds symmetrically if padding is configured
            int pad = Mathf.Max(0, _boundsPadding);
            if (pad > 0)
            {
                origin.x -= pad;
                origin.y -= pad;
                size.x += pad * 2;
                size.y += pad * 2;
            }
            _w = Mathf.Max(0, size.x);
            _h = Mathf.Max(0, size.y);
            _ox = origin.x;
            _oy = origin.y;
            _bounds = new GridBounds(_w, _h);
            _data = new TileData[_w * _h];
            Debug.Log($"[TileDataProvider] Baking from ground size=({_w},{_h}) origin=({_ox},{_oy}) overlay={( _overlayTilemap != null ? "on" : "off")}");

            var pos = new Vector3Int();
            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    pos.x = _ox + x;
                    pos.y = _oy + y;
                    // Ground
                    var gTile = _groundTilemap.GetTile(pos);
                    if (!_binding.TryResolve(gTile, out var groundTd))
                    {
                        groundTd = GetSharedImpassable();
                    }

                    TileData finalTd = groundTd;

                    // Overlay (e.g., road) may override movement if present
                    if (_overlayTilemap != null && _overlayBinding != null)
                    {
                        var oTile = _overlayTilemap.GetTile(pos);
                        if (oTile != null && _overlayBinding.TryResolve(oTile, out var overlayTd))
                        {
                            bool allowOverlay = !_overlayRequiresGroundPassable || (groundTd != null && groundTd.IsPassable);
                            if (allowOverlay)
                            {
                                // Prefer overlay TileData semantics for movement; keep ground when it blocks.
                                finalTd = overlayTd;
                            }
                        }
                    }

                    _data[x + y * _w] = finalTd ?? GetSharedImpassable();
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep bounds in sync in the Editor so authoring tools (e.g., ResourceNodeAuthoring.OnValidate)
            // get up-to-date in-bounds checks even before a runtime Bake() happens.
            try
            {
                if (_groundTilemap == null)
                    return;

                var origin = _groundTilemap.origin;
                var size = _groundTilemap.size;

                int pad = Mathf.Max(0, _boundsPadding);
                if (pad > 0)
                {
                    origin.x -= pad;
                    origin.y -= pad;
                    size.x += pad * 2;
                    size.y += pad * 2;
                }

                _w = Mathf.Max(0, size.x);
                _h = Mathf.Max(0, size.y);
                _ox = origin.x;
                _oy = origin.y;
                _bounds = new GridBounds(_w, _h);
                // Do not allocate or fill _data here; full Bake() will do that at runtime.
            }
            catch
            {
                // Ignore transient editor errors (domain reloads, prefab stages, etc.).
            }
        }
#endif

        public bool TryGet(GridCoord c, out TileData data)
        {
            if (c.X < 0 || c.Y < 0 || c.X >= _w || c.Y >= _h)
            {
                data = null;
                return false;
            }
            data = _data[c.X + c.Y * _w];
            return data != null;
        }

        /// <summary>
        /// Convert a world position to the baked GridCoord relative to Bounds.
        /// </summary>
        public GridCoord WorldToCoord(Grid grid, Vector3 world)
        {
            var cell = grid.WorldToCell(world);
            int x = cell.x - _ox;
            int y = cell.y - _oy;
            return _bounds.Clamp(x, y);
        }

        /// <summary>
        /// Convert a world position to the provider-local GridCoord without clamping to Bounds.
        /// Also reports whether the resulting coordinate lies within Bounds.
        /// </summary>
        public GridCoord WorldToCoordUnclamped(Grid grid, Vector3 world, out bool inBounds)
        {
            var cell = grid.WorldToCell(world);
            int x = cell.x - _ox;
            int y = cell.y - _oy;
            inBounds = _bounds.Contains(x, y);
            return new GridCoord(x, y);
        }

        /// <summary>
        /// Convert a provider-local coord (0..W-1, 0..H-1) back to the Tilemap cell position.
        /// </summary>
        public Vector3Int CoordToCell(GridCoord c)
        {
            return new Vector3Int(_ox + c.X, _oy + c.Y, 0);
        }

        /// <summary>
        /// Convert a provider-local coord to the world position at the center of the cell.
        /// </summary>
        public Vector3 CoordToWorld(Grid grid, GridCoord c)
        {
            var cell = CoordToCell(c);
            return grid.GetCellCenterWorld(cell);
        }

        private static TileData s_Impassable;
        private static TileData GetSharedImpassable()
        {
            if (s_Impassable == null)
            {
                s_Impassable = ScriptableObject.CreateInstance<TileData>();
                s_Impassable.flags = 0; // not passable
                s_Impassable.enterMask = EnterMask8.None;
                s_Impassable.moveCostCardinal = 1;
                s_Impassable.moveCostDiagonal = 1;
                s_Impassable.terrainType = TerrainType.Mountain; // arbitrary
            }
            return s_Impassable;
        }
    }
}
