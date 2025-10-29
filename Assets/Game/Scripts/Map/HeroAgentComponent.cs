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
        private static readonly (int dx, int dy, EnterMask8 direction)[] NeighborOffsets = new (int, int, EnterMask8)[]
        {
            (0, 1, EnterMask8.N),
            (1, 0, EnterMask8.E),
            (0, -1, EnterMask8.S),
            (-1, 0, EnterMask8.W),
            (1, 1, EnterMask8.NE),
            (1, -1, EnterMask8.SE),
            (-1, -1, EnterMask8.SW),
            (-1, 1, EnterMask8.NW)
        };

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
        public event Action<GridCoord> VisualStepCompleted;

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
        public GridCoord GridPosition => _previousPosition;
        public IMapMovementService Movement => _movement;

        private IHeroMapAgent _agent;
        private IMapMovementService _movement;
        private GridCoord _previousPosition;
        private IGridOccupancyProvider _occupancy;
        private HeroIdentity _identity;

        private readonly Queue<VisualMoveStep> _visualMoveQueue = new Queue<VisualMoveStep>();
        private bool _isVisualMoving;
        private Vector2 _currentFacingDirection;
        private bool _autoAdvance;

        private void Awake()
        {
            if (_provider == null) throw new InvalidOperationException("HeroAgentComponent requires a TilemapTileDataProvider.");
            if (_grid == null) throw new InvalidOperationException("HeroAgentComponent requires a Grid.");
            if (_character4D == null) throw new InvalidOperationException("HeroAgentComponent requires a Character4D.");
            if (_character4D.AnimationManager == null) throw new InvalidOperationException("AnimationManager is not assigned on the Character4D component in the Inspector. Please drag the AnimationManager component into this slot.");

            _identity = GetComponent<HeroIdentity>();
            // Discover occupancy provider once
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _occupancy == null; i++)
            {
                if (behaviours[i] is IGridOccupancyProvider occ)
                {
                    _occupancy = occ;
                }
            }
        }

        private void Start()
        {
            var start = _provider.WorldToCoord(_grid, transform.position);
            InitializeAgentAt(start);

            if (_debugLogs && _agent != null)
            {
                _agent.Started += () => Debug.Log("[HeroAgent] Movement started.");
                _agent.StepCommitted += (from, to, cost) => Debug.Log($"[HeroAgent] Step {from}->{to} cost={cost}");
                _agent.Stopped += reason => Debug.Log($"[HeroAgent] Stopped: {reason}");
                _movement.Changed += (cur, max) => Debug.Log($"[HeroAgent] MP changed: {cur}/{max}");
            }

            _character4D.SetDirection(Vector2.down);
            _currentFacingDirection = Vector2.down;

            // Snap to grid on start
            MoveTransformToCell(start, true);

            AgentInitialized?.Invoke();
            VisualStepCompleted?.Invoke(start);

            if (_autoAdvance)
            {
                AdvanceNextStep();
            }
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
            _autoAdvance = false;
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

            // Update occupancy service if available
            if (_occupancy != null && _identity != null)
            {
                _occupancy.TryGetOccupant(_previousPosition, out _); // ensure map initialized
                if (_previousPosition.X != pos.X || _previousPosition.Y != pos.Y)
                {
                    if (_occupancy is GridOccupancyService service)
                    {
                        service.UpdateHeroPosition(_identity, _previousPosition, pos);
                    }
                }
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
            VisualStepCompleted?.Invoke(targetCoord);
            _isVisualMoving = false;

            if (_autoAdvance && _visualMoveQueue.Count == 0)
            {
                AdvanceNextStep();
            }

            if (_visualMoveQueue.Count == 0)
            {
                _character4D.AnimationManager.SetState(CharacterState.Idle);
            }
        }

        public bool TryGetCheapestStepCost(out int cost)
        {
            cost = int.MaxValue;
            if (_provider == null)
            {
                cost = -1;
                return false;
            }

            bool found = false;
            var origin = _previousPosition;

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                var (dx, dy, direction) = NeighborOffsets[i];
                if ((_allowedMoves & direction) == 0)
                {
                    continue;
                }

                var next = new GridCoord(origin.X + dx, origin.Y + dy);
                if (!_provider.TryGet(next, out var tileData))
                {
                    continue;
                }

                if (!tileData.IsPassable || !tileData.CanEnterFrom(direction))
                {
                    continue;
                }

                if (_occupancy != null && _identity != null && _occupancy.IsOccupiedByOther(next, _identity))
                {
                    continue;
                }

                bool isDiagonal = TileData.IsDiagonalStep(dx, dy);
                int stepCost = tileData.GetMoveCost(isDiagonal);
                if (stepCost <= 0)
                {
                    stepCost = 1;
                }

                if (stepCost < cost)
                {
                    cost = stepCost;
                    found = true;
                }
            }

            if (!found)
            {
                cost = -1;
            }

            return found;
        }
        public void BeginAutoTraversal()
        {
            if (_agent == null)
                return;
            _autoAdvance = true;
            AdvanceNextStep();
        }

        public void StopAutoTraversal()
        {
            _autoAdvance = false;
            _visualMoveQueue.Clear();
            if (_isVisualMoving)
            {
                StopAllCoroutines();
                _isVisualMoving = false;
                MoveTransformToCell(_previousPosition, true);
                VisualStepCompleted?.Invoke(_previousPosition);
                _character4D.AnimationManager.SetState(CharacterState.Idle);
            }
        }

        private void AdvanceNextStep()
        {
            if (!_autoAdvance || _agent == null || _isVisualMoving)
                return;

            var result = _agent.AdvanceSteps(1);
            if (result.StepsCommitted == 0 || result.Reason != StopReason.None)
            {
                _autoAdvance = false;
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

        /// <summary>
        /// Teleports the hero to the specified grid coordinate, rebuilding the internal agent and updating occupancy.
        /// </summary>
        public void TeleportTo(GridCoord target)
        {
            var from = _previousPosition;

            // Unsubscribe from previous agent events if any
            if (_agent != null)
            {
                _agent.PositionChanged -= OnAgentPositionChanged;
                _agent.Started -= OnMovementStarted;
                _agent.Stopped -= OnMovementStopped;
                _agent.ClearPath();
            }

            InitializeAgentAt(target);

            // Snap visuals immediately
            MoveTransformToCell(target, true);

            // Update occupancy immediately
            if (_occupancy is GridOccupancyService service && _identity != null)
            {
                service.UpdateHeroPosition(_identity, from, target);
            }
        }

        private void InitializeAgentAt(GridCoord start)
        {
            _movement = new MapMovementService(_maxDailyMp);
            _previousPosition = start;
            System.Func<GridCoord, bool> isBlocked = null;
            if (_occupancy != null)
            {
                var self = _identity; // capture
                isBlocked = c => _occupancy.IsOccupiedByOther(c, self);
            }
            var agent = new HeroMapAgent(_provider, _movement, start, _allowedMoves, _validateSteps, isBlocked);
            agent.PositionChanged += OnAgentPositionChanged;
            agent.Started += OnMovementStarted;
            agent.Stopped += OnMovementStopped;
            _agent = agent;
        }
    }
}
