using UnityEngine;

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
        [SerializeField] private HeroAgentComponent _hero;

        [Header("Movement")]
        [SerializeField] private EnterMask8 _allowedMoves = EnterMask8.N | EnterMask8.E | EnterMask8.S | EnterMask8.W; // 4-way

        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;

        private AStarPathfinder _pf;
        private int _pfW, _pfH;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) Debug.LogWarning("ClickToMoveController: No camera assigned and Camera.main was null.");
            if (_grid == null) Debug.LogError("ClickToMoveController requires a Grid reference.");
            if (_provider == null) Debug.LogError("ClickToMoveController requires a TilemapTileDataProvider.");
            if (_hero == null) Debug.LogError("ClickToMoveController requires a HeroAgentComponent.");

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

        private void Update()
        {
            if (_camera == null || _grid == null || _provider == null || _hero == null) return;

            if (Input.GetMouseButtonDown(0))
            {
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
                HandleRightClick();
            }
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
                return;
            }

            if (_hero.Agent.SetPath(path))
            {
                if (_debugLogs) Debug.Log("[ClickToMove] Path set. Advancing all available steps.");
                _hero.Agent.AdvanceAllAvailable();
            }
            else if (_debugLogs)
            {
                Debug.Log("[ClickToMove] Failed to set path (invalid start or adjacency).");
            }
        }

        private void HandleRightClick()
        {
            _hero.Agent.ClearPath();
            if (_debugLogs) Debug.Log("[ClickToMove] Cancelled current order.");
            // optional: clear preview/highlights
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
