using System;
using System.Collections.Generic;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Consumes a precomputed path, spending MP per step via IMapMovementService.
    /// Stops on insufficient MP or illegal steps; exposes RemainingMP.
    /// </summary>
    public sealed class HeroMapAgent : IHeroMapAgent
    {
        public GridCoord Position { get; private set; }
        public GridCoord Goal { get; private set; }
        public int RemainingMP => _mp.Current;

        public bool HasPath => _path != null && _path.Count > 0;
        public IReadOnlyList<GridCoord> Path => _path;
        public int PathIndex => _pathIndex;

        public event Action<GridCoord> PositionChanged;
        public event Action<int, int> RemainingMPChanged;
        public event Action Started;
        public event Action<GridCoord, GridCoord, int> StepCommitted;
        public event Action<StopReason> Stopped;

        private readonly ITileDataProvider _provider;
        private readonly IMapMovementService _mp;
        private readonly EnterMask8 _allowedMoves;
        private readonly bool _validateSteps;

        private List<GridCoord> _path;
        private int _pathIndex; // index of current position in path
        private bool _startedOnCurrentPath;

        // Reusable buffer to avoid allocations per call
        private readonly List<int> _costBuffer = new List<int>(64);

        public HeroMapAgent(ITileDataProvider provider, IMapMovementService movementService, GridCoord start,
            EnterMask8 allowedMoves = EnterMask8.All, bool validateSteps = true)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _mp = movementService ?? throw new ArgumentNullException(nameof(movementService));
            Position = start;
            Goal = start;
            _allowedMoves = allowedMoves;
            _validateSteps = validateSteps;
            _mp.Changed += OnMpChanged;
        }

        public bool SetPath(IReadOnlyList<GridCoord> path)
        {
            if (path == null || path.Count == 0) { ClearPath(); return false; }
            if (!path[0].Equals(Position)) { ClearPath(); Stopped?.Invoke(StopReason.InvalidPath); return false; }

            // Validate adjacency if requested
            if (_validateSteps)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    int dx = b.X - a.X;
                    int dy = b.Y - a.Y;
                    int adx = dx < 0 ? -dx : dx;
                    int ady = dy < 0 ? -dy : dy;
                    if (adx > 1 || ady > 1 || (adx == 0 && ady == 0))
                    {
                        ClearPath();
                        Stopped?.Invoke(StopReason.InvalidPath);
                        return false;
                    }
                }
            }

            if (_path == null) _path = new List<GridCoord>(path.Count);
            _path.Clear();
            for (int i = 0; i < path.Count; i++) _path.Add(path[i]);
            _pathIndex = 0;
            Goal = _path[^1];
            _startedOnCurrentPath = false;
            return true;
        }

        public void ClearPath()
        {
            _pathIndex = 0;
            _path?.Clear();
            Goal = Position;
            _startedOnCurrentPath = false;
        }

        public PreviewResult Preview()
        {
            if (!HasPath || _pathIndex >= _path.Count - 1)
                return new PreviewResult(0, 0);

            _costBuffer.Clear();
            int legalSteps = CollectLegalStepCosts(_costBuffer, int.MaxValue, out _);
            int total = _mp.PreviewSequenceCost(_costBuffer, out int payable);
            // payable cannot exceed legalSteps by construction
            return new PreviewResult(payable, total);
        }

        public AdvanceResult AdvanceAllAvailable() => AdvanceSteps(int.MaxValue);

        public AdvanceResult AdvanceSteps(int maxSteps)
        {
            if (!HasPath || _pathIndex >= _path.Count - 1)
            {
                return new AdvanceResult(0, 0, Position, StopReason.ReachedGoal);
            }

            if (!_startedOnCurrentPath)
            {
                _startedOnCurrentPath = true;
                Started?.Invoke();
            }

            _costBuffer.Clear();
            int legalSteps = CollectLegalStepCosts(_costBuffer, maxSteps, out bool blocked);

            // Spend MP for legal segment
            int spent = _mp.SpendSequence(_costBuffer, out int payable);

            // Commit payable steps
            int committed = 0;
            for (; committed < payable; committed++)
            {
                var from = _path[_pathIndex + committed];
                var to = _path[_pathIndex + committed + 1];
                int dx = to.X - from.X;
                int dy = to.Y - from.Y;
                bool diag = (dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy) == 2;

                // cost from buffer is the cost of entering 'to'
                int stepCost = _costBuffer[committed];
                StepCommitted?.Invoke(from, to, stepCost);
                Position = to;
                PositionChanged?.Invoke(Position);
            }

            _pathIndex += committed;

            // Determine stop reason
            StopReason reason = StopReason.None;
            if (_pathIndex >= _path.Count - 1)
            {
                reason = StopReason.ReachedGoal;
            }
            else if (committed < legalSteps)
            {
                // MP ran out before completing all legal steps
                reason = StopReason.InsufficientMP;
            }
            else if (blocked)
            {
                reason = StopReason.BlockedByTerrain;
            }

            if (reason != StopReason.None)
            {
                Stopped?.Invoke(reason);
            }

            return new AdvanceResult(committed, spent, Position, reason);
        }

        private int CollectLegalStepCosts(List<int> costs, int maxSteps, out bool blocked)
        {
            blocked = false;
            int collected = 0;
            int end = _path.Count - 1;
            int remainingStepsBudget = maxSteps;

            for (int i = _pathIndex; i < end && remainingStepsBudget > 0; i++)
            {
                var from = _path[i];
                var to = _path[i + 1];
                int dx = to.X - from.X;
                int dy = to.Y - from.Y;
                int adx = dx < 0 ? -dx : dx;
                int ady = dy < 0 ? -dy : dy;
                if (adx > 1 || ady > 1 || (adx == 0 && ady == 0))
                {
                    // Non-adjacent or zero step → invalid path
                    ClearPath();
                    Stopped?.Invoke(StopReason.InvalidPath);
                    return 0;
                }

                var enterDir = TileData.DirectionFromDelta(dx, dy);
                if ((_allowedMoves & enterDir) == 0)
                {
                    blocked = true;
                    break;
                }

                if (!_provider.TryGet(to, out var nextTd) || !nextTd.IsPassable || !nextTd.CanEnterFrom(enterDir))
                {
                    blocked = true;
                    break;
                }

                bool isDiag = TileData.IsDiagonalStep(dx, dy);
                int stepCost = nextTd.GetMoveCost(isDiag);
                if (stepCost <= 0) stepCost = 1; // guardrail
                costs.Add(stepCost);
                collected++;
                remainingStepsBudget--;
            }

            return collected;
        }

        private void OnMpChanged(int current, int max)
        {
            RemainingMPChanged?.Invoke(current, max);
        }
    }
}

