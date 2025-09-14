using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// A* pathfinder over a GridBounds using TileData movement rules and costs.
    /// Optimized to minimize allocations by reusing internal buffers.
    /// </summary>
    public sealed class AStarPathfinder
    {
        public sealed class Config
        {
            public bool AllowDiagonal = true;
            public bool DisallowCornerCutting = true;
            public int HeuristicCardinalBase = 8;  // Should be <= min cardinal move cost
            public int HeuristicDiagonalBase = 11; // Should be <= min diagonal move cost
            public int MaxIterations = 0;          // 0 => auto: bounds.Area * 8
        }

        private readonly ITileDataProvider _provider;
        private readonly GridBounds _bounds;
        private readonly int _w;
        private readonly int _h;
        private readonly Config _cfg;

        // Reusable buffers (size = w*h)
        private int[] _gCost;
        private int[] _fCost;
        private byte[] _state; // 0 = unseen, 1 = open, 2 = closed
        private GridCoord[] _cameFrom;

        // Min-heap for open set
        private int[] _heapIdxToCell; // heap array storing cell indices
        private int[] _cellToHeapIdx; // position of cell in heap (-1 if not in heap)
        private int _heapSize;

        // Reusable path buffer
        private readonly List<GridCoord> _pathBuffer = new List<GridCoord>(64);

        // Neighbor deltas (8-way)
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { 1, 1, 0, -1, -1, -1, 0, 1 };
        private static readonly EnterMask8[] DIR_MASK =
        {
            EnterMask8.N, EnterMask8.NE, EnterMask8.E, EnterMask8.SE,
            EnterMask8.S, EnterMask8.SW, EnterMask8.W, EnterMask8.NW
        };

        public AStarPathfinder(ITileDataProvider provider, GridBounds bounds, Config config = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _bounds = bounds;
            _w = bounds.Width;
            _h = bounds.Height;
            _cfg = config ?? new Config();
            EnsureCapacity();
        }

        public bool TryGetPath(GridCoord start, GridCoord goal, out List<GridCoord> path, EnterMask8 allowedMoves = EnterMask8.All)
        {
            _pathBuffer.Clear();
            if (FindPathInternal(start, goal, allowedMoves))
            {
                path = new List<GridCoord>(_pathBuffer);
                return true;
            }
            path = null;
            return false;
        }

        public List<GridCoord> GetPath(GridCoord start, GridCoord goal, EnterMask8 allowedMoves = EnterMask8.All)
        {
            _pathBuffer.Clear();
            if (!FindPathInternal(start, goal, allowedMoves))
                return new List<GridCoord>(0);
            return new List<GridCoord>(_pathBuffer);
        }

        private bool FindPathInternal(GridCoord start, GridCoord goal, EnterMask8 allowedMoves)
        {
            if (_bounds.IsEmpty) return false;

            // Clamp to bounds to avoid invalid indices
            start = _bounds.Clamp(start);
            goal = _bounds.Clamp(goal);

            if (start.Equals(goal))
            {
                _pathBuffer.Add(start);
                return true;
            }

            var area = _w * _h;
            var iterLimit = _cfg.MaxIterations > 0 ? _cfg.MaxIterations : Math.Max(1, area * 8);

            // Init arrays
            for (int i = 0; i < area; i++)
            {
                _gCost[i] = int.MaxValue;
                _fCost[i] = int.MaxValue;
                _state[i] = 0;
                _cameFrom[i] = default;
                _cellToHeapIdx[i] = -1;
            }
            _heapSize = 0;

            int startIdx = ToIndex(start);
            int goalIdx = ToIndex(goal);
            _gCost[startIdx] = 0;
            _fCost[startIdx] = Heuristic(start, goal);
            HeapPush(startIdx);
            _state[startIdx] = 1;

            int iterations = 0;
            while (_heapSize > 0)
            {
                if (++iterations > iterLimit) break;

                int current = HeapPop();
                if (current == goalIdx)
                {
                    Reconstruct(goalIdx, startIdx);
                    return true;
                }
                _state[current] = 2; // closed

                var cx = current % _w;
                var cy = current / _w;

                for (int dir = 0; dir < 8; dir++)
                {
                    // Respect allowed movement mask and diagonal toggle
                    var mask = DIR_MASK[dir];
                    bool isDiag = (dir % 2) == 1; // NE, SE, SW, NW indices are odd in our ordering
                    if ((_cfg.AllowDiagonal == false && isDiag) || (allowedMoves & mask) == 0)
                        continue;

                    int nx = cx + DX[dir];
                    int ny = cy + DY[dir];
                    if (nx < 0 || ny < 0 || nx >= _w || ny >= _h) continue;

                    int nidx = nx + ny * _w;
                    if (_state[nidx] == 2) continue; // already closed

                    // Check target tile legality
                    if (!_provider.TryGet(new GridCoord(nx, ny), out var nextTd)) continue;
                    if (!nextTd.IsPassable) continue;
                    if (!nextTd.CanEnterFrom(mask)) continue;

                    // Optional corner cutting rule
                    if (_cfg.DisallowCornerCutting && isDiag)
                    {
                        // Orthogonal neighbors must both permit entry
                        int ox = cx + DX[(dir + 7) & 7]; // previous cardinal in our ordering
                        int oy = cy + DY[(dir + 7) & 7];
                        int px = cx + DX[(dir + 1) & 7]; // next cardinal
                        int py = cy + DY[(dir + 1) & 7];

                        if (!IsCardinalEntryLegal(cx, cy, ox, oy) || !IsCardinalEntryLegal(cx, cy, px, py))
                            continue;
                    }

                    int stepCost = nextTd.GetMoveCost(isDiag);
                    long tentativeG = (long)_gCost[current] + stepCost; // prevent overflow
                    if (tentativeG >= _gCost[nidx]) continue;

                    _cameFrom[nidx] = new GridCoord(cx, cy);
                    _gCost[nidx] = (int)tentativeG;
                    _fCost[nidx] = _gCost[nidx] + Heuristic(new GridCoord(nx, ny), goal);

                    if (_state[nidx] == 0)
                    {
                        HeapPush(nidx);
                        _state[nidx] = 1;
                    }
                    else
                    {
                        HeapDecreaseKey(nidx);
                    }
                }
            }

            return false;
        }

        private bool IsCardinalEntryLegal(int fromX, int fromY, int toX, int toY)
        {
            if (toX < 0 || toY < 0 || toX >= _w || toY >= _h) return false;
            if (!_provider.TryGet(new GridCoord(toX, toY), out var td)) return false;
            if (!td.IsPassable) return false;

            int dx = toX - fromX;
            int dy = toY - fromY;
            var enterDir = TileData.DirectionFromDelta(dx, dy);
            return td.CanEnterFrom(enterDir);
        }

        private int Heuristic(GridCoord a, GridCoord b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            int dmin = Mathf.Min(dx, dy);
            int dmax = Mathf.Max(dx, dy);
            return _cfg.HeuristicCardinalBase * (dmax - dmin) + _cfg.HeuristicDiagonalBase * dmin;
        }

        private int ToIndex(GridCoord c) => c.X + c.Y * _w;

        private void Reconstruct(int goalIdx, int startIdx)
        {
            _pathBuffer.Clear();
            int idx = goalIdx;
            while (true)
            {
                int x = idx % _w;
                int y = idx / _w;
                _pathBuffer.Add(new GridCoord(x, y));
                if (idx == startIdx) break;

                var prev = _cameFrom[idx];
                idx = prev.X + prev.Y * _w;
            }
            _pathBuffer.Reverse();
        }

        private void EnsureCapacity()
        {
            int n = Math.Max(1, _w * _h);
            _gCost = new int[n];
            _fCost = new int[n];
            _state = new byte[n];
            _cameFrom = new GridCoord[n];
            _heapIdxToCell = new int[n];
            _cellToHeapIdx = new int[n];
        }

        // --- Binary heap operations (min-heap by fCost, tie-break with gCost) ---
        private void HeapPush(int cellIdx)
        {
            int i = _heapSize++;
            _heapIdxToCell[i] = cellIdx;
            _cellToHeapIdx[cellIdx] = i;
            HeapSiftUp(i);
        }

        private int HeapPop()
        {
            int rootCell = _heapIdxToCell[0];
            int last = --_heapSize;
            if (_heapSize > 0)
            {
                int lastCell = _heapIdxToCell[last];
                _heapIdxToCell[0] = lastCell;
                _cellToHeapIdx[lastCell] = 0;
                HeapSiftDown(0);
            }
            _cellToHeapIdx[rootCell] = -1;
            return rootCell;
        }

        private void HeapDecreaseKey(int cellIdx)
        {
            int i = _cellToHeapIdx[cellIdx];
            if (i >= 0) HeapSiftUp(i);
        }

        private void HeapSiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (!HeapLess(i, p)) break;
                HeapSwap(i, p);
                i = p;
            }
        }

        private void HeapSiftDown(int i)
        {
            while (true)
            {
                int l = (i << 1) + 1;
                if (l >= _heapSize) break;
                int r = l + 1;
                int m = (r < _heapSize && HeapLess(r, l)) ? r : l;
                if (!HeapLess(m, i)) break;
                HeapSwap(i, m);
                i = m;
            }
        }

        private bool HeapLess(int hi, int hj)
        {
            int ci = _heapIdxToCell[hi];
            int cj = _heapIdxToCell[hj];
            int fi = _fCost[ci];
            int fj = _fCost[cj];
            if (fi != fj) return fi < fj;
            int gi = _gCost[ci];
            int gj = _gCost[cj];
            return gi < gj;
        }

        private void HeapSwap(int a, int b)
        {
            int ca = _heapIdxToCell[a];
            int cb = _heapIdxToCell[b];
            _heapIdxToCell[a] = cb;
            _heapIdxToCell[b] = ca;
            _cellToHeapIdx[cb] = a;
            _cellToHeapIdx[ca] = b;
        }
    }
}

