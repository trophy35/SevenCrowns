using UnityEngine;
using UnityEngine.Tilemaps;

namespace SevenCrowns.Map.FogOfWar
{
    /// <summary>
    /// Mirrors fog-of-war state onto a Tilemap overlay, using preconfigured tiles for each state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FogOfWarTilemapRenderer : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TilemapTileDataProvider _provider;
        [SerializeField] private MonoBehaviour _fogServiceBehaviour; // Optional; must implement IFogOfWarService

        [Header("Tiles")]
        [SerializeField] private TileBase _visibleTile;
        [SerializeField] private TileBase _exploredTile;
        [SerializeField] private TileBase _unknownTile;

        private IFogOfWarService _fog;

        private void Awake()
        {
            if (_tilemap == null)
            {
                _tilemap = GetComponent<Tilemap>();
            }

            _fog = ResolveFogService();
            if (_fog == null)
            {
                Debug.LogError("FogOfWarTilemapRenderer requires an IFogOfWarService.", this);
                enabled = false;
                return;
            }

            if (_provider == null)
            {
                _provider = FindObjectOfType<TilemapTileDataProvider>(true);
            }

            if (_provider == null)
            {
                Debug.LogError("FogOfWarTilemapRenderer requires a TilemapTileDataProvider.", this);
                enabled = false;
            }
        }

        private IFogOfWarService ResolveFogService()
        {
            if (_fogServiceBehaviour is IFogOfWarService explicitService)
            {
                return explicitService;
            }
            return FindObjectOfType<FogOfWarService>(true);
        }

        private void OnEnable()
        {
            if (_fog == null || _provider == null)
                return;

            _fog.CellChanged += OnCellChanged;
            _fog.VisibilityCleared += OnVisibilityCleared;
            RebuildAll();
        }

        private void OnDisable()
        {
            if (_fog == null)
                return;

            _fog.CellChanged -= OnCellChanged;
            _fog.VisibilityCleared -= OnVisibilityCleared;
        }

        private void OnCellChanged(GridCoord coord, FogOfWarState state)
        {
            ApplyTile(coord, state);
        }

        private void OnVisibilityCleared()
        {
            RebuildAll();
        }

        private void RebuildAll()
        {
            if (_fog == null || _provider == null)
                return;

            var bounds = _fog.Bounds;
            if (bounds.IsEmpty)
                return;

            for (int y = 0; y < bounds.Height; y++)
            {
                for (int x = 0; x < bounds.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    ApplyTile(coord, _fog.GetState(coord));
                }
            }
        }

        private void ApplyTile(GridCoord coord, FogOfWarState state)
        {
            if (_tilemap == null || _provider == null)
                return;

            var cell = _provider.CoordToCell(coord);
            TileBase tile = state switch
            {
                FogOfWarState.Visible => _visibleTile,
                FogOfWarState.Explored => _exploredTile,
                _ => _unknownTile,
            };
            _tilemap.SetTile(cell, tile);
        }
    }
}
