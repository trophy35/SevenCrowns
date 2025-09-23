using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Handles click-to-move: left-click computes path and orders the hero; right-click cancels.
    /// Uses Grid.WorldToCell (no physics) and a cached A* pathfinder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClickToMoveController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Grid _grid;
        [SerializeField] private TilemapTileDataProvider _provider;
        [SerializeField] private HeroAgentComponent _hero; // Fallback if no selection service assigned
        [SerializeField] private PathPreviewRenderer _preview;

        [Header("Selection")]
        [SerializeField] private MonoBehaviour _selectionBehaviour; // Optional; must implement ISelectedHeroAgentProvider
        [SerializeField] private LayerMask _heroLayer = 0;           // Set to your "Heroes" layer in Inspector

        private ISelectedHeroAgentProvider _selection;

        [Header("Movement")]
        [SerializeField] private EnterMask8 _allowedMoves = EnterMask8.N | EnterMask8.E | EnterMask8.S | EnterMask8.W; // 4-way

        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;
        [Header("Input")]
        [SerializeField] private bool _ignoreClicksOverUI = true;

        private AStarPathfinder _pf;
        private int _pfW, _pfH;
        private bool _hasPreview;
        private GridCoord _pendingGoal;
        private GridCoord _pendingStart;
        private List<GridCoord> _pendingPath;
        private readonly List<int> _costBuffer = new List<int>(64);
        private bool _isMoving;

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

            if (_debugLogs)
            {
                Debug.Log($"[ClickToMove] Initialized. Bounds={_provider.Bounds}, Diagonal=false, Mask={_allowedMoves}");
            }
        }

        private void Start()
        {
            // Build pathfinder after provider has baked (its Awake runs before Start).
            BuildPathfinderIfNeeded(log: true);
        }

        private void OnDestroy()
        {
            if (_selection != null) _selection.SelectedHeroChanged -= OnSelectedHeroChanged;
            UnsubscribeHero();
        }

        private void OnSelectedHeroChanged(HeroAgentComponent hero)
        {
            UnsubscribeHero();
            _hero = hero;
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
                return;

            if (Input.GetMouseButtonDown(0))
            {
                // Try selecting a hero first if a selection service is present
                if (_selection != null && TryPickHeroUnderMouse(out var heroId))
                {
                    _selection.SelectById(heroId);
                    return;
                }

                if (_hero == null || _isMoving) return;

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
            _pf = new AStarPathfinder(_provider, b, cfg);
            _pfW = b.Width; _pfH = b.Height;
            if (log) Debug.Log($"[ClickToMove] Pathfinder built with bounds {b}.");
        }
    }
}
