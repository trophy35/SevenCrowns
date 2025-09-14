using System;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Scene adapter that constructs a HeroMapAgent + MapMovementService for this GameObject.
    /// Moves the transform to match grid Position changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroAgentComponent : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TilemapTileDataProvider _provider;
        [SerializeField] private Grid _grid;

        [Header("Movement")]
        [SerializeField] private int _maxDailyMp = 240;
        [SerializeField] private EnterMask8 _allowedMoves = EnterMask8.N | EnterMask8.E | EnterMask8.S | EnterMask8.W; // 4-way by default
        [SerializeField] private bool _validateSteps = true;
        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;

        public IHeroMapAgent Agent => _agent;
        public IMapMovementService Movement => _movement;

        private IHeroMapAgent _agent;
        private IMapMovementService _movement;

        private void Awake()
        {
            if (_provider == null) throw new InvalidOperationException("HeroAgentComponent requires a TilemapTileDataProvider.");
            if (_grid == null) throw new InvalidOperationException("HeroAgentComponent requires a Grid.");

            _movement = new MapMovementService(_maxDailyMp);
            var start = _provider.WorldToCoord(_grid, transform.position);
            var agent = new HeroMapAgent(_provider, _movement, start, _allowedMoves, _validateSteps);
            agent.PositionChanged += OnAgentPositionChanged;
            if (_debugLogs)
            {
                agent.Started += () => Debug.Log("[HeroAgent] Movement started.");
                agent.StepCommitted += (from, to, cost) => Debug.Log($"[HeroAgent] Step {from}->{to} cost={cost}");
                agent.Stopped += reason => Debug.Log($"[HeroAgent] Stopped: {reason}");
                _movement.Changed += (cur, max) => Debug.Log($"[HeroAgent] MP changed: {cur}/{max}");
            }
            _agent = agent;

            // Snap to grid on start
            MoveTransformToCell(start);
        }

        private void OnAgentPositionChanged(GridCoord pos)
        {
            MoveTransformToCell(pos);
        }

        private void MoveTransformToCell(GridCoord c)
        {
            // Convert provider-local coord back to the actual tilemap cell using provider origin.
            var world = _provider.CoordToWorld(_grid, c);
            transform.position = new Vector3(world.x, world.y, transform.position.z);
        }
    }
}
