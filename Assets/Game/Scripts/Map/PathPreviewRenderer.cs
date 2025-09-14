using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Renders a path preview as arrow sprites per step and a cross on the destination.
    /// Colors arrows green (within current MP) or red (beyond current MP).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PathPreviewRenderer : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Grid _grid;
        [SerializeField] private TilemapTileDataProvider _provider;
        [SerializeField] private Transform _container;

        [Header("Sprites - Straight (Green)")]
        [SerializeField] private Sprite _straightGreenN;
        [SerializeField] private Sprite _straightGreenE;
        [SerializeField] private Sprite _straightGreenS;
        [SerializeField] private Sprite _straightGreenW;

        [Header("Sprites - Straight (Red)")]
        [SerializeField] private Sprite _straightRedN;
        [SerializeField] private Sprite _straightRedE;
        [SerializeField] private Sprite _straightRedS;
        [SerializeField] private Sprite _straightRedW;

        [Header("Sprites - Turn (Green)")]
        [SerializeField] private Sprite _turnGreen_N_E; // N->E
        [SerializeField] private Sprite _turnGreen_E_S; // E->S
        [SerializeField] private Sprite _turnGreen_S_W; // S->W
        [SerializeField] private Sprite _turnGreen_W_N; // W->N
        [SerializeField] private Sprite _turnGreen_N_W; // N->W
        [SerializeField] private Sprite _turnGreen_W_S; // W->S
        [SerializeField] private Sprite _turnGreen_S_E; // S->E
        [SerializeField] private Sprite _turnGreen_E_N; // E->N

        [Header("Sprites - Turn (Red)")]
        [SerializeField] private Sprite _turnRed_N_E; // N->E
        [SerializeField] private Sprite _turnRed_E_S; // E->S
        [SerializeField] private Sprite _turnRed_S_W; // S->W
        [SerializeField] private Sprite _turnRed_W_N; // W->N
        [SerializeField] private Sprite _turnRed_N_W; // N->W
        [SerializeField] private Sprite _turnRed_W_S; // W->S
        [SerializeField] private Sprite _turnRed_S_E; // S->E
        [SerializeField] private Sprite _turnRed_E_N; // E->N

        [Header("Sprites - Markers")]
        [SerializeField] private Sprite _crossGreen;
        [SerializeField] private Sprite _crossRed;

        [Header("Render Settings")]
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _orderInLayer = 50;
        [SerializeField] private Material _material;

        private readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>(128);
        private SpriteRenderer _crossRenderer;

        private enum MoveDir { None = 0, N, E, S, W }

        public void Show(IReadOnlyList<GridCoord> path, int payableSteps)
        {
            if (_grid == null || _provider == null) return;
            if (path == null || path.Count < 2)
            {
                Clear();
                return;
            }

            // We render arrows on intermediate tiles only (no arrow on the final target tile).
            int needed = Mathf.Max(0, path.Count - 2);
            EnsurePool(needed);

            // Render arrows
            int used = 0;
            // i indexes the step we are entering; stop before the last tile so only the cross appears there.
            for (int i = 1; i < path.Count - 1; i++)
            {
                var from = path[i - 1];
                var to = path[i];
                var curr = Dir(from, to);
                var next = (i < path.Count - 1) ? Dir(to, path[i + 1]) : MoveDir.None;
                bool isStraight = next == MoveDir.None || next == curr;
                bool isGreen = i <= payableSteps;

                var sr = _pool[used++];
                sr.enabled = true;
                sr.sprite = SelectSprite(curr, next, isStraight, isGreen);
                var world = _provider.CoordToWorld(_grid, to);
                sr.transform.position = new Vector3(world.x, world.y, sr.transform.position.z);
            }

            // Disable extra pooled renderers
            for (int i = used; i < _pool.Count; i++)
            {
                if (_pool[i].enabled) _pool[i].enabled = false;
            }

            // Render cross on last tile
            EnsureCross();
            var last = path[path.Count - 1];
            var worldLast = _provider.CoordToWorld(_grid, last);
            bool canReachThisTurn = payableSteps >= (path.Count - 1);
            _crossRenderer.sprite = canReachThisTurn ? (_crossGreen != null ? _crossGreen : _crossRed) : (_crossRed != null ? _crossRed : _crossGreen);
            _crossRenderer.enabled = true;
            _crossRenderer.transform.position = new Vector3(worldLast.x, worldLast.y, _crossRenderer.transform.position.z);
        }

        public void Clear()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].enabled) _pool[i].enabled = false;
            }
            if (_crossRenderer != null) _crossRenderer.enabled = false;
        }

        private void EnsurePool(int needed)
        {
            if (_container == null) _container = this.transform;
            while (_pool.Count < needed)
            {
                var go = new GameObject("PathArrow", typeof(SpriteRenderer));
                go.transform.SetParent(_container, false);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.enabled = false;
                sr.sortingLayerName = _sortingLayerName;
                sr.sortingOrder = _orderInLayer;
                if (_material != null) sr.material = _material;
                _pool.Add(sr);
            }
        }

        private void EnsureCross()
        {
            if (_crossRenderer == null)
            {
                var go = new GameObject("PathEnd", typeof(SpriteRenderer));
                go.transform.SetParent(_container != null ? _container : this.transform, false);
                _crossRenderer = go.GetComponent<SpriteRenderer>();
                _crossRenderer.enabled = false;
                _crossRenderer.sortingLayerName = _sortingLayerName;
                _crossRenderer.sortingOrder = _orderInLayer + 1;
                if (_material != null) _crossRenderer.material = _material;
            }
        }

        private static MoveDir Dir(GridCoord a, GridCoord b)
        {
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            if (dx == 0 && dy == 1) return MoveDir.N;
            if (dx == 1 && dy == 0) return MoveDir.E;
            if (dx == 0 && dy == -1) return MoveDir.S;
            if (dx == -1 && dy == 0) return MoveDir.W;
            return MoveDir.None;
        }

        private Sprite SelectSprite(MoveDir curr, MoveDir next, bool isStraight, bool green)
        {
            if (isStraight)
            {
                return green ? StraightGreen(curr) : StraightRed(curr);
            }
            // Turn from curr -> next
            return green ? TurnGreen(curr, next) : TurnRed(curr, next);
        }

        private Sprite StraightGreen(MoveDir d) => d switch
        {
            MoveDir.N => _straightGreenN,
            MoveDir.E => _straightGreenE,
            MoveDir.S => _straightGreenS,
            MoveDir.W => _straightGreenW,
            _ => null,
        };

        private Sprite StraightRed(MoveDir d) => d switch
        {
            MoveDir.N => _straightRedN,
            MoveDir.E => _straightRedE,
            MoveDir.S => _straightRedS,
            MoveDir.W => _straightRedW,
            _ => null,
        };

        private Sprite TurnGreen(MoveDir from, MoveDir to)
        {
            if (from == MoveDir.N && to == MoveDir.E) return _turnGreen_N_E;
            if (from == MoveDir.E && to == MoveDir.S) return _turnGreen_E_S;
            if (from == MoveDir.S && to == MoveDir.W) return _turnGreen_S_W;
            if (from == MoveDir.W && to == MoveDir.N) return _turnGreen_W_N;
            if (from == MoveDir.N && to == MoveDir.W) return _turnGreen_N_W;
            if (from == MoveDir.W && to == MoveDir.S) return _turnGreen_W_S;
            if (from == MoveDir.S && to == MoveDir.E) return _turnGreen_S_E;
            if (from == MoveDir.E && to == MoveDir.N) return _turnGreen_E_N;
            return null;
        }

        private Sprite TurnRed(MoveDir from, MoveDir to)
        {
            if (from == MoveDir.N && to == MoveDir.E) return _turnRed_N_E;
            if (from == MoveDir.E && to == MoveDir.S) return _turnRed_E_S;
            if (from == MoveDir.S && to == MoveDir.W) return _turnRed_S_W;
            if (from == MoveDir.W && to == MoveDir.N) return _turnRed_W_N;
            if (from == MoveDir.N && to == MoveDir.W) return _turnRed_N_W;
            if (from == MoveDir.W && to == MoveDir.S) return _turnRed_W_S;
            if (from == MoveDir.S && to == MoveDir.E) return _turnRed_S_E;
            if (from == MoveDir.E && to == MoveDir.N) return _turnRed_E_N;
            return null;
        }
    }
}
