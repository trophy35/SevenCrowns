using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
// UI cursor binding is done via IWorldCursorHintSource to avoid Map -> UI dependency

namespace SevenCrowns.Map
{
    /// <summary>
    /// Handles click-to-move: left-click computes path and orders the hero; right-click cancels.
    /// Uses Grid.WorldToCell (no physics) and a cached A* pathfinder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClickToMoveController : MonoBehaviour, IWorldCursorHintSource
    {
        [Header("Wiring")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Grid _grid;
        [SerializeField] private TilemapTileDataProvider _provider;
        [SerializeField] private MonoBehaviour _occupancyBehaviour; // Optional; must implement IGridOccupancyProvider
        [SerializeField] private HeroAgentComponent _hero; // Fallback if no selection service assigned
        [SerializeField] private PathPreviewRenderer _preview;

        [Header("Selection")]
        [SerializeField] private MonoBehaviour _selectionBehaviour; // Optional; must implement ISelectedHeroAgentProvider
        [SerializeField] private LayerMask _heroLayer = 0;           // Set to your "Heroes" layer in Inspector

        private ISelectedHeroAgentProvider _selection;
        private string _currentHeroId;

        [Header("Movement")]
        [SerializeField] private EnterMask8 _allowedMoves = EnterMask8.N | EnterMask8.E | EnterMask8.S | EnterMask8.W; // 4-way

        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;
        [Header("Input")]
        [SerializeField] private bool _ignoreClicksOverUI = true;
        [Tooltip("When enabled, left-click previews a path and second click confirms movement. When disabled, no path preview is shown.")]
        private bool _moveModeEnabled = false; // controlled by UI button via SetMoveModeEnabled(bool)

        private AStarPathfinder _pf;
        private IGridOccupancyProvider _occupancy;
        private BlockingOverlayTileDataProvider _blockedProvider;
        private int _pfW, _pfH;
        private bool _hasPreview;
        private GridCoord _pendingGoal;
        private GridCoord _pendingStart;
        private List<GridCoord> _pendingPath;
        private readonly List<int> _costBuffer = new List<int>(64);
        private bool _isMoving;
        private readonly System.Collections.Generic.Dictionary<string, GridCoord> _lastGoalByHeroId = new System.Collections.Generic.Dictionary<string, GridCoord>(8);

        // Cursor hint caching to avoid per-frame heavy work
        private bool _lastHoverHero;
        private bool _lastMoveHint;
        private bool _hasCachedGoal;
        private GridCoord _cachedGoalCell;
        private bool _cachedMoveHint;
        public bool HoveringHero => _lastHoverHero;
        public bool MoveHint => _lastMoveHint;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) Debug.LogWarning("ClickToMoveController: No camera assigned and Camera.main was null.");
            if (_grid == null) Debug.LogError("ClickToMoveController requires a Grid reference.");
            if (_provider == null) Debug.LogError("ClickToMoveController requires a TilemapTileDataProvider.");

            // Bind selection provider if supplied or discoverable
            if (_selectionBehaviour != null && _selectionBehaviour is ISelectedHeroAgentProvider p1)
            {
                _selection = p1;
            }
            else
            {
                // Try find any selection provider in the scene
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && _selection == null; i++)
                {
                    if (behaviours[i] is ISelectedHeroAgentProvider p) _selection = p;
                }
            }

            if (_selection != null)
            {
                _selection.SelectedHeroChanged += OnSelectedHeroChanged;
                OnSelectedHeroChanged(_selection.CurrentHero);
            }
            else
            {
                if (_hero == null) Debug.LogError("ClickToMoveController requires a HeroAgentComponent when no selection provider is assigned.");
                else _hero.AgentInitialized += OnHeroAgentInitialized;
            }

            // Bind occupancy provider if supplied or discoverable
            if (_occupancyBehaviour != null && _occupancyBehaviour is IGridOccupancyProvider occ)
            {
                _occupancy = occ;
            }
            else
            {
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && _occupancy == null; i++)
                {
                    if (behaviours[i] is IGridOccupancyProvider o) _occupancy = o;
                }
            }

            if (_debugLogs)
            {
                Debug.Log($"[ClickToMove] Initialized. Bounds={_provider.Bounds}, Diagonal=false, Mask={_allowedMoves}");
            }
        }

        private void Start()
        {
            // Prepare blocking overlay provider (wrap base provider)
            _blockedProvider = new BlockingOverlayTileDataProvider(_provider, _occupancy);
            if (_hero != null)
            {
                var id = _hero.GetComponent<HeroIdentity>();
                _blockedProvider.SetExcluded(id);
            }

            // Build pathfinder after provider has baked (its Awake runs before Start).
            BuildPathfinderIfNeeded(log: true);
        }

        private void OnDestroy()
        {
            if (_selection != null) _selection.SelectedHeroChanged -= OnSelectedHeroChanged;
            UnsubscribeHero();
            // UI binder will clear any cursor overrides on unsubscribe
        }

        private void OnSelectedHeroChanged(HeroAgentComponent hero)
        {
            UnsubscribeHero();
            _hero = hero;
            _currentHeroId = null;
            if (_hero != null)
            {
                var id = _hero.GetComponent<HeroIdentity>();
                if (id != null && !string.IsNullOrWhiteSpace(id.HeroId))
                {
                    _currentHeroId = id.HeroId;
                    // Update blocker exclusion to allow starting tile
                    if (_blockedProvider != null)
                    {
                        _blockedProvider.SetExcluded(id);
                    }
                }
            }
            if (_debugLogs) Debug.Log($"[ClickToMove] Selected hero changed. HasHero={(_hero!=null)}");
            if (_hero != null)
            {
                _hero.AgentInitialized += OnHeroAgentInitialized;
                if (_hero.Agent != null)
                {
                    _hero.Agent.Started += OnMovementStarted;
                    _hero.Agent.Stopped += OnMovementStopped;
                }
                if (_hero.Movement != null)
                    _hero.Movement.Changed += OnMovementPointsChanged;
            }
            _preview?.Clear();
            _hasPreview = false;
        }

        private void OnHeroAgentInitialized()
        {
            _hero.Agent.Started += OnMovementStarted;
            _hero.Agent.Stopped += OnMovementStopped;
            // Re-evaluate current preview when MP changes (e.g., after End Turn reset).
            if (_hero.Movement != null)
                _hero.Movement.Changed += OnMovementPointsChanged;
        }

        private void UnsubscribeHero()
        {
            if (_hero == null) return;
            _hero.AgentInitialized -= OnHeroAgentInitialized;
            if (_hero.Agent != null)
            {
                _hero.Agent.Started -= OnMovementStarted;
                _hero.Agent.Stopped -= OnMovementStopped;
            }
            if (_hero.Movement != null)
                _hero.Movement.Changed -= OnMovementPointsChanged;
        }

        private void OnMovementStarted()
        {
            _isMoving = true;
        }

        private void OnMovementStopped(StopReason reason)
        {
            _isMoving = false;
        }

        private void OnMovementPointsChanged(int current, int max)
        {
            if (!_moveModeEnabled)
            {
                // Keep the last preview data in memory but do not render while disabled.
                _preview?.Clear();
                return;
            }
            if (_hasPreview && _pendingPath != null && _pendingPath.Count > 1)
            {
                int payableSteps = ComputePayableSteps(_pendingPath);
                _preview?.Show(_pendingPath, payableSteps);
            }
        }

        private void Update()
        {
            if (_camera == null || _grid == null || _provider == null) return;

            // If configured, ignore world clicks when the pointer is over any UI element (robust raycast across input modules).
            if (_ignoreClicksOverUI && IsPointerOverUI())
            {
                // Report no hints while over UI
                NotifyCursorHints(false, false);
                return;
            }

            // Compute and publish hints each frame
            var hover = EvaluateHoverHero();
            var move = EvaluateMoveHint();
            NotifyCursorHints(hover, move);

            if (Input.GetMouseButtonDown(0))
            {
                // Try selecting a hero first if a selection service is present
                if (_selection != null && TryPickHeroUnderMouse(out var heroId))
                {
                    _selection.SelectById(heroId);
                    return;
                }

                if (_hero == null || _isMoving) return;
                if (!_moveModeEnabled) return; // Do not preview/move when move mode is disabled

                BuildPathfinderIfNeeded(log: _debugLogs);
                if (_pf == null)
                {
                    if (_debugLogs) Debug.Log("[ClickToMove] Pathfinder not ready: provider bounds are empty. Is the ground Tilemap empty?");
                    return;
                }
                HandleLeftClick();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (_hero == null) return;
                HandleRightClick();
            }
        }

        /// <summary>
        /// Enables move mode. If a previous path preview exists, it will be shown again.
        /// </summary>
        public void EnableMoveMode()
        {
            _moveModeEnabled = true;
            // If we have a pending preview from last time, re-show it now.
            if (_hasPreview && _pendingPath != null && _pendingPath.Count > 1)
            {
                int payableSteps = ComputePayableSteps(_pendingPath);
                _preview?.Show(_pendingPath, payableSteps);
            }
            else if (!string.IsNullOrEmpty(_currentHeroId) && _lastGoalByHeroId.TryGetValue(_currentHeroId, out var lastGoal))
            {
                BuildPathfinderIfNeeded(log: _debugLogs);
                if (_pf != null && _hero != null && _hero.Agent != null)
                {
                    var start = _hero.Agent.Position;
                    if (!start.Equals(lastGoal))
                    {
                        var path = _pf.GetPath(start, lastGoal, _allowedMoves);
                        if (path != null && path.Count > 0)
                        {
                            int payableSteps = ComputePayableSteps(path);
                            _preview?.Show(path, payableSteps);
                            _pendingPath = path;
                            _pendingGoal = lastGoal;
                            _pendingStart = start;
                            _hasPreview = true;
                            if (_debugLogs) Debug.Log($"[ClickToMove] Restored preview for hero={_currentHeroId} to lastGoal={lastGoal}");
                        }
                    }
                }
            }
            if (_debugLogs) Debug.Log("[ClickToMove] Move mode ENABLED");
        }

        /// <summary>
        /// Disables move mode and hides any currently rendered preview (but keeps the last preview data in memory).
        /// </summary>
        public void DisableMoveMode()
        {
            _moveModeEnabled = false;
            _preview?.Clear();
            if (_debugLogs) Debug.Log("[ClickToMove] Move mode DISABLED");
        }

        /// <summary>
        /// Sets move mode enabled/disabled.
        /// </summary>
        public void SetMoveModeEnabled(bool enabled)
        {
            if (enabled) EnableMoveMode(); else DisableMoveMode();
        }

        /// <summary>
        /// Toggles move mode state.
        /// </summary>
        public void ToggleMoveMode()
        {
            SetMoveModeEnabled(!_moveModeEnabled);
        }

        private bool TryPickHeroUnderMouse(out string heroId)
        {
            heroId = null;

            var mouse = Input.mousePosition;
            float depth = Mathf.Abs((_camera.transform.position - _grid.transform.position).z);
            var world = _camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, depth));
            var p = new Vector2(world.x, world.y);

            Collider2D[] hits;
            if (_heroLayer == 0)
            {
                // If no layer set, search all layers to be more forgiving in setup.
                hits = Physics2D.OverlapPointAll(p);
            }
            else
            {
                hits = Physics2D.OverlapPointAll(p, _heroLayer);
            }
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null) continue;
                var id = h.GetComponentInParent<HeroIdentity>();
                if (id != null && !string.IsNullOrWhiteSpace(id.HeroId))
                {
                    heroId = id.HeroId;
                    return true;
                }
            }
            if (_debugLogs) Debug.Log("[ClickToMove] No hero detected under cursor.");
            return false;
        }

        private static readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(16);
        private static bool IsPointerOverUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            // Quick path
            if (es.IsPointerOverGameObject()) return true;
            // Robust path
            var ped = new PointerEventData(es) { position = Input.mousePosition };
            _uiRaycastResults.Clear();
            es.RaycastAll(ped, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        private void HandleLeftClick()
        {
            var mouse = Input.mousePosition;
            // Account for perspective cameras by providing depth to ScreenToWorldPoint
            float depth = Mathf.Abs((_camera.transform.position - _grid.transform.position).z);
            var world = _camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, depth));
            var goal = _provider.WorldToCoord(_grid, world);
            var start = _hero.Agent.Position;
            if (_debugLogs)
            {
                Debug.Log($"[ClickToMove] Click at world={world:F2} mouse={mouse} -> start={start} goal={goal}");
                DebugLogTile("Start", start);
                DebugLogTile("Goal", goal);
                DebugLogNeighbors("StartNbrs", start);
                DebugLogNeighbors("GoalNbrs", goal);
            }
            if (start.Equals(goal))
            {
                if (_debugLogs) Debug.Log("[ClickToMove] Start == Goal. Ignored.");
                return;
            }

            var path = _pf.GetPath(start, goal, _allowedMoves);
            if (_debugLogs)
            {
                Debug.Log($"[ClickToMove] Path length={path.Count}");
            }
            if (path.Count == 0)
            {
                if (_debugLogs)
                {
                    if (_provider.TryGet(goal, out var td))
                    {
                        Debug.Log($"[ClickToMove] No path. Goal TileData: type={td.terrainType} passable={td.IsPassable} mask={td.enterMask}");
                    }
                    else
                    {
                        Debug.Log("[ClickToMove] No path. Provider could not resolve goal tile.");
                    }
                    // Probe a simple cardinal line from start to goal to find the first blocked step
                    DebugProbeLine(start, goal);
                }
                _preview?.Clear();
                _hasPreview = false;
                return;
            }

            // If we already previewed this exact goal and the hero hasn't moved since, confirm and move
            if (_hasPreview && goal.Equals(_pendingGoal) && start.Equals(_pendingStart))
            {
                if (_hero.Agent.SetPath(path))
                {
                    if (_debugLogs) Debug.Log("[ClickToMove] Confirmed click. Moving hero.");
                    _preview?.Clear();
                    _hasPreview = false;
                    _hero.Agent.AdvanceAllAvailable();
                }
                else if (_debugLogs)
                {
                    Debug.Log("[ClickToMove] Failed to confirm path (invalid start or adjacency).");
                }
            }
            else
            {
                int payableSteps = ComputePayableSteps(path);
                _preview?.Show(path, payableSteps);
                _pendingPath = path;
                _pendingGoal = goal;
                _pendingStart = start;
                _hasPreview = true;
                if (!string.IsNullOrEmpty(_currentHeroId))
                {
                    _lastGoalByHeroId[_currentHeroId] = goal;
                }
                if (_debugLogs) Debug.Log($"[ClickToMove] Preview shown. PayableSteps={payableSteps}");
            }
        }

        private void HandleRightClick()
        {
            _hero.Agent.ClearPath();
            if (_debugLogs) Debug.Log("[ClickToMove] Cancelled current order.");
            // optional: clear preview/highlights
            _preview?.Clear();
            _hasPreview = false;
        }

        /// <summary>
        /// Compute and apply cursor hints based on current hover and move possibility.
        /// Uses CursorManager with priorities so hero hover overrides move hint.
        /// </summary>
        private void NotifyCursorHints(bool hoverHero, bool moveHint)
        {
            if (hoverHero == _lastHoverHero && moveHint == _lastMoveHint)
                return;
            _lastHoverHero = hoverHero;
            _lastMoveHint = moveHint;
            CursorHintsChanged?.Invoke(_lastHoverHero, _lastMoveHint);
        }

        public event System.Action<bool, bool> CursorHintsChanged;

        /// <summary>
        /// Returns true if the mouse is currently hovering a hero (eligible for selection).
        /// </summary>
        private bool EvaluateHoverHero()
        {
            if (_selection == null)
                return false;
            return TryPickHeroUnderMouse(out _);
        }

        /// <summary>
        /// Returns true if move mode is enabled, a hero is ready, and the tile under mouse has a valid path from hero.
        /// Uses a cached result for the last hovered goal cell.
        /// </summary>
        private bool EvaluateMoveHint()
        {
            if (!_moveModeEnabled || _hero == null || _isMoving)
                return false;

            // Determine goal cell under mouse
            var mouse = Input.mousePosition;
            float depth = Mathf.Abs((_camera.transform.position - _grid.transform.position).z);
            var world = _camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, depth));
            var goal = _provider.WorldToCoord(_grid, world);

            if (_hasCachedGoal && goal.Equals(_cachedGoalCell))
                return _cachedMoveHint;

            BuildPathfinderIfNeeded(log: false);
            bool hint = false;
            if (_pf != null && _hero.Agent != null)
            {
                var start = _hero.Agent.Position;
                if (!start.Equals(goal))
                {
                    var path = _pf.GetPath(start, goal, _allowedMoves);
                    hint = path != null && path.Count > 0;
                }
            }

            _cachedGoalCell = goal;
            _cachedMoveHint = hint;
            _hasCachedGoal = true;
            return hint;
        }

        private int ComputePayableSteps(System.Collections.Generic.IReadOnlyList<GridCoord> path)
        {
            // Build per-step costs (4-way)
            _costBuffer.Clear();
            for (int i = 1; i < path.Count; i++)
            {
                var to = path[i];
                if (_provider.TryGet(to, out var td))
                {
                    _costBuffer.Add(td.GetMoveCost(false));
                }
                else
                {
                    _costBuffer.Add(int.MaxValue / 4);
                }
            }
            int payable;
            _hero.Movement.PreviewSequenceCost(_costBuffer, out payable);
            return payable;
        }

        // --- Debug helpers ---
        private void DebugLogTile(string label, GridCoord c)
        {
            if (_provider.TryGet(c, out var td))
            {
                Debug.Log($"[ClickToMove][{label}] {c} -> type={td.terrainType} passable={td.IsPassable} mask={td.enterMask}");
            }
            else
            {
                Debug.Log($"[ClickToMove][{label}] {c} -> no TileData");
            }
        }

        private void DebugLogNeighbors(string label, GridCoord c)
        {
            var north = new GridCoord(c.X, c.Y + 1);
            var east  = new GridCoord(c.X + 1, c.Y);
            var south = new GridCoord(c.X, c.Y - 1);
            var west  = new GridCoord(c.X - 1, c.Y);
            DebugLogTile(label + ":N", north);
            DebugLogTile(label + ":E", east);
            DebugLogTile(label + ":S", south);
            DebugLogTile(label + ":W", west);
        }

        private void DebugProbeLine(GridCoord start, GridCoord goal)
        {
            int sx = start.X, sy = start.Y;
            int gx = goal.X, gy = goal.Y;
            int dx = gx > sx ? 1 : gx < sx ? -1 : 0;
            int dy = gy > sy ? 1 : gy < sy ? -1 : 0;

            var cur = start;
            // Move in X until aligned, then Y (4-way like our pathfinder config)
            while (cur.X != gx)
            {
                var next = new GridCoord(cur.X + dx, cur.Y);
                var enter = (dx > 0) ? EnterMask8.E : EnterMask8.W;
                if (!_provider.TryGet(next, out var td) || !td.IsPassable || !td.CanEnterFrom(enter))
                {
                    Debug.Log($"[ClickToMove][Probe] Blocked at {next} passable={(td!=null && td.IsPassable)} mask={(td!=null ? td.enterMask.ToString() : "<null>")}, needed={enter}");
                    return;
                }
                cur = next;
            }
            while (cur.Y != gy)
            {
                var next = new GridCoord(cur.X, cur.Y + dy);
                var enter = (dy > 0) ? EnterMask8.N : EnterMask8.S;
                if (!_provider.TryGet(next, out var td) || !td.IsPassable || !td.CanEnterFrom(enter))
                {
                    Debug.Log($"[ClickToMove][Probe] Blocked at {next} passable={(td!=null && td.IsPassable)} mask={(td!=null ? td.enterMask.ToString() : "<null>")}, needed={enter}");
                    return;
                }
                cur = next;
            }
            Debug.Log("[ClickToMove][Probe] Cardinal line from start to goal has no immediate blockers. Path may be blocked by alternative route or overlay/ground mapping inconsistencies.");
        }

        private void BuildPathfinderIfNeeded(bool log)
        {
            var b = _provider.Bounds;
            if (b.Width <= 0 || b.Height <= 0)
            {
                _pf = null;
                return;
            }
            if (_pf != null && _pfW == b.Width && _pfH == b.Height) return;

            var cfg = new AStarPathfinder.Config
            {
                AllowDiagonal = false,
                DisallowCornerCutting = true,
                HeuristicCardinalBase = 8,
                HeuristicDiagonalBase = 11
            };
            var providerForPath = (ITileDataProvider)_blockedProvider ?? _provider;
            _pf = new AStarPathfinder(providerForPath, b, cfg);
            _pfW = b.Width; _pfH = b.Height;
            if (log) Debug.Log($"[ClickToMove] Pathfinder built with bounds {b}.");
        }
    }
}
