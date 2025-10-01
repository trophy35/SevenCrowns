using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map.FogOfWar
{
    /// <summary>
    /// Stores and mutates fog of war state for the strategic grid. Uses the baked TileData grid for vision blocking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FogOfWarService : MonoBehaviour, IFogOfWarService
    {
        private static readonly GridCoord[] NeighborOffsets =
        {
            new GridCoord(1, 0),
            new GridCoord(-1, 0),
            new GridCoord(0, 1),
            new GridCoord(0, -1),
            new GridCoord(1, 1),
            new GridCoord(1, -1),
            new GridCoord(-1, 1),
            new GridCoord(-1, -1),
        };

        [Header("Wiring")]
        [SerializeField] private MonoBehaviour _providerBehaviour; // Optional; must implement ITileDataProvider

        [Header("Settings")]
        [SerializeField, Min(0)] private int _defaultVisionRadius = 3;
        [SerializeField] private bool _debugLogs = false;

        public GridBounds Bounds => _bounds;
        public event Action<GridCoord, FogOfWarState> CellChanged;
        public event Action VisibilityCleared;

        private ITileDataProvider _provider;
        private GridBounds _bounds;
        private FogOfWarState[] _states = Array.Empty<FogOfWarState>();
        private int[] _visitStamp = Array.Empty<int>();
        private Queue<GridCoord> _queue;
        private int _stamp;
        private bool _initialized;

        private void Awake()
        {
            _queue = new Queue<GridCoord>(64);
            if (_provider == null)
            {
                _provider = ResolveProvider();
            }
            EnsureInitialized();
        }

        private ITileDataProvider ResolveProvider()
        {
            if (_providerBehaviour != null)
            {
                if (_providerBehaviour is ITileDataProvider provider)
                {
                    return provider;
                }
                Debug.LogError("Assigned provider behaviour does not implement ITileDataProvider.", this);
            }
            return FindObjectOfType<TilemapTileDataProvider>(true);
        }

        public void Configure(ITileDataProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            EnsureInitialized(force: true);
        }

        public FogOfWarState GetState(GridCoord coord)
        {
            if (!_initialized || _bounds.IsEmpty || !_bounds.Contains(coord))
                return FogOfWarState.Unknown;
            return _states[ToIndex(coord)];
        }

        public bool IsVisible(GridCoord coord) => GetState(coord) == FogOfWarState.Visible;

        public bool IsExplored(GridCoord coord)
        {
            var state = GetState(coord);
            return state == FogOfWarState.Visible || state == FogOfWarState.Explored;
        }

        public void RevealArea(GridCoord center, int radius)
        {
            if (!EnsureInitialized())
                return;

            if (!_bounds.Contains(center))
            {
                center = _bounds.Clamp(center);
            }

            radius = ResolveRadius(radius);
            int radiusSq = radius * radius;

            _stamp++;
            _queue ??= new Queue<GridCoord>(64);
            _queue.Clear();
            _queue.Enqueue(center);

            while (_queue.Count > 0)
            {
                var coord = _queue.Dequeue();
                if (!_bounds.Contains(coord))
                    continue;

                int idx = ToIndex(coord);
                if (_visitStamp[idx] == _stamp)
                    continue;

                _visitStamp[idx] = _stamp;

                int dx = coord.X - center.X;
                int dy = coord.Y - center.Y;
                if ((dx * dx) + (dy * dy) > radiusSq)
                    continue;

                if (!HasLineOfSight(center, coord))
                    continue;

                SetState(coord, FogOfWarState.Visible);

                if (IsVisionBlocked(coord))
                    continue;

                for (int i = 0; i < NeighborOffsets.Length; i++)
                {
                    var offset = NeighborOffsets[i];
                    var next = new GridCoord(coord.X + offset.X, coord.Y + offset.Y);
                    if (_bounds.Contains(next))
                    {
                        _queue.Enqueue(next);
                    }
                }
            }
        }

        public void RevealCells(IReadOnlyList<GridCoord> cells)
        {
            if (cells == null || !EnsureInitialized())
                return;

            for (int i = 0; i < cells.Count; i++)
            {
                var coord = cells[i];
                if (!_bounds.Contains(coord))
                {
                    coord = _bounds.Clamp(coord);
                }
                SetState(coord, FogOfWarState.Visible);
            }
        }

        public void ClearTransientVisibility()
        {
            if (!EnsureInitialized())
                return;

            bool anyChanged = false;
            for (int y = 0; y < _bounds.Height; y++)
            {
                for (int x = 0; x < _bounds.Width; x++)
                {
                    int idx = x + y * _bounds.Width;
                    if (_states[idx] == FogOfWarState.Visible)
                    {
                        _states[idx] = FogOfWarState.Explored;
                        CellChanged?.Invoke(new GridCoord(x, y), FogOfWarState.Explored);
                        anyChanged = true;
                    }
                }
            }

            if (anyChanged)
            {
                VisibilityCleared?.Invoke();
            }
        }

        private bool EnsureInitialized(bool force = false)
        {
            if (_provider == null)
            {
                _provider = ResolveProvider();
            }

            if (_provider == null)
            {
                return false;
            }

            var bounds = _provider.Bounds;
            if (bounds.IsEmpty)
            {
                return false;
            }

            if (_initialized && !force && bounds.Width == _bounds.Width && bounds.Height == _bounds.Height)
            {
                _bounds = bounds;
                return true;
            }

            _bounds = bounds;
            var area = _bounds.Area;
            _states = new FogOfWarState[area];
            _visitStamp = new int[area];
            _stamp = 0;
            _initialized = true;

            if (_debugLogs)
            {
                Debug.Log($"[FogOfWar] Initialized bounds={_bounds} area={area}.", this);
            }

            return true;
        }

        private void SetState(GridCoord coord, FogOfWarState newState)
        {
            if (_bounds.IsEmpty)
                return;

            int idx = ToIndex(coord);
            var current = _states[idx];
            if (current == newState)
                return;

            switch (newState)
            {
                case FogOfWarState.Visible:
                    _states[idx] = FogOfWarState.Visible;
                    CellChanged?.Invoke(coord, FogOfWarState.Visible);
                    break;
                case FogOfWarState.Explored:
                    if (current != FogOfWarState.Explored)
                    {
                        _states[idx] = FogOfWarState.Explored;
                        CellChanged?.Invoke(coord, FogOfWarState.Explored);
                    }
                    break;
                default:
                    _states[idx] = FogOfWarState.Unknown;
                    CellChanged?.Invoke(coord, FogOfWarState.Unknown);
                    break;
            }
        }

        private bool HasLineOfSight(GridCoord origin, GridCoord target)
        {
            if (origin.Equals(target))
                return true;

            int x0 = origin.X;
            int y0 = origin.Y;
            int x1 = target.X;
            int y1 = target.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int x = x0;
            int y = y0;

            while (x != x1 || y != y1)
            {
                int e2 = err << 1;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }

                if (x == x1 && y == y1)
                    return true;

                var coord = new GridCoord(x, y);
                if (_provider != null && _provider.TryGet(coord, out var tile) && tile != null && (tile.flags & TileFlags.BlocksVision) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsVisionBlocked(GridCoord coord)
        {
            if (_provider != null && _provider.TryGet(coord, out var tile) && tile != null)
            {
                return (tile.flags & TileFlags.BlocksVision) != 0;
            }
            return false;
        }

        private int ResolveRadius(int radius)
        {
            if (radius < 0)
            {
                radius = _defaultVisionRadius;
            }
            return Mathf.Max(0, radius);
        }

        private int ToIndex(GridCoord coord)
        {
            return coord.X + coord.Y * _bounds.Width;
        }

        private static int ChebyshevDistance(GridCoord a, GridCoord b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            return Mathf.Max(dx, dy);
        }
    }
}
