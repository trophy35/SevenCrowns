
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private readonly struct VisualMoveStep
        {
            public readonly GridCoord TargetCoord;
            public readonly Vector2 MoveDirection;

            public VisualMoveStep(GridCoord targetCoord, Vector2 moveDirection)
            {
                TargetCoord = targetCoord;
                MoveDirection = moveDirection;
            }
        }

        public event Action AgentInitialized;

        [Header("Wiring")]
        [SerializeField] private TilemapTileDataProvider _provider;
        [SerializeField] private Grid _grid;
        [SerializeField] private Character4D _character4D;

        [Header("Movement")]
        [SerializeField] private int _maxDailyMp = 240;
        [SerializeField] private EnterMask8 _allowedMoves = EnterMask8.N | EnterMask8.E | EnterMask8.S | EnterMask8.W; // 4-way by default
        [SerializeField] private bool _validateSteps = true;
        [SerializeField] private float _moveSpeed = 0.3f; // Seconds per tile

        [Header("Visuals")]
        [Tooltip("The default visual offset for the hero sprite.")]
        [SerializeField] private Vector3 _visualOffset;
        [Tooltip("A set of tile-specific offsets that override the default.")]
        [SerializeField] private TileVisualsSet _visualsSet;

        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;

        public IHeroMapAgent Agent => _agent;
        public IMapMovementService Movement => _movement;

        private IHeroMapAgent _agent;
        private IMapMovementService _movement;
        private GridCoord _previousPosition;

        private readonly Queue<VisualMoveStep> _visualMoveQueue = new Queue<VisualMoveStep>();
        private bool _isVisualMoving;
        private Vector2 _currentFacingDirection;

        private void Awake()
        {
            if (_provider == null) throw new InvalidOperationException("HeroAgentComponent requires a TilemapTileDataProvider.");
            if (_grid == null) throw new InvalidOperationException("HeroAgentComponent requires a Grid.");
            if (_character4D == null) throw new InvalidOperationException("HeroAgentComponent requires a Character4D.");
            if (_character4D.AnimationManager == null) throw new InvalidOperationException("AnimationManager is not assigned on the Character4D component in the Inspector. Please drag the AnimationManager component into this slot.");
        }

        private void Start()
        {
            _movement = new MapMovementService(_maxDailyMp);
            var start = _provider.WorldToCoord(_grid, transform.position);
            _previousPosition = start;
            var agent = new HeroMapAgent(_provider, _movement, start, _allowedMoves, _validateSteps);
            agent.PositionChanged += OnAgentPositionChanged;
            agent.Started += OnMovementStarted;
            agent.Stopped += OnMovementStopped;

            if (_debugLogs)
            {
                agent.Started += () => Debug.Log("[HeroAgent] Movement started.");
                agent.StepCommitted += (from, to, cost) => Debug.Log($"[HeroAgent] Step {from}->{to} cost={cost}");
                agent.Stopped += reason => Debug.Log($"[HeroAgent] Stopped: {reason}");
                _movement.Changed += (cur, max) => Debug.Log($"[HeroAgent] MP changed: {cur}/{max}");
            }
            _agent = agent;

            _character4D.SetDirection(Vector2.down);
            _currentFacingDirection = Vector2.down;

            // Snap to grid on start
            MoveTransformToCell(start, true);

            AgentInitialized?.Invoke();
        }

        private void Update()
        {
            if (!_isVisualMoving && _visualMoveQueue.Count > 0)
            {
                _character4D.AnimationManager.SetState(CharacterState.Walk);

                var moveStep = _visualMoveQueue.Dequeue();

                if (_currentFacingDirection != moveStep.MoveDirection)
                {
                    _character4D.SetDirection(moveStep.MoveDirection);
                    _currentFacingDirection = moveStep.MoveDirection;
                }

                StartCoroutine(MoveToTargetCoroutine(moveStep.TargetCoord));
            }
        }

        private void OnMovementStarted()
        {
            // Logic moved to be driven by visual queue.
        }

        private void OnMovementStopped(StopReason reason)
        {
            // Logic moved to be driven by visual queue.
        }

        private void OnAgentPositionChanged(GridCoord pos)
        {
            var directionX = pos.X - _previousPosition.X;
            var directionY = pos.Y - _previousPosition.Y;
            Vector2 moveDirection = Vector2.zero;

            if (directionX > 0) moveDirection = Vector2.right;
            else if (directionX < 0) moveDirection = Vector2.left;
            else if (directionY > 0) moveDirection = Vector2.up;
            else if (directionY < 0) moveDirection = Vector2.down;

            if (moveDirection != Vector2.zero)
            {
                _visualMoveQueue.Enqueue(new VisualMoveStep(pos, moveDirection));
            }

            _previousPosition = pos;
        }

        private IEnumerator MoveToTargetCoroutine(GridCoord targetCoord)
        {
            _isVisualMoving = true;

            Vector3 startPos = transform.position;
            Vector3 endPos = GetWorldPositionForCoord(targetCoord);

            float time = 0;
            while (time < _moveSpeed)
            {
                transform.position = Vector3.Lerp(startPos, endPos, time / _moveSpeed);
                time += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos; // Ensure final position is accurate
            _isVisualMoving = false;

            if (_visualMoveQueue.Count == 0)
            {
                _character4D.AnimationManager.SetState(CharacterState.Idle);
            }
        }

        private void MoveTransformToCell(GridCoord c, bool immediate = false)
        {
            if (immediate)
            {
                transform.position = GetWorldPositionForCoord(c);
            }
            else
            {
                // This branch is not used anymore, but kept for clarity.
            }
        }

        private Vector3 GetWorldPositionForCoord(GridCoord c)
        {
            var world = _provider.CoordToWorld(_grid, c);
            var finalOffset = _visualOffset;

            if (_visualsSet != null && _provider.TryGet(c, out var tileData))
            {
                if (_visualsSet.TryGetVisualOffset(tileData.terrainType, out var specificOffset))
                {
                    finalOffset = specificOffset;
                }
            }

            return new Vector3(world.x, world.y, transform.position.z) + finalOffset;
        }
    }
}
