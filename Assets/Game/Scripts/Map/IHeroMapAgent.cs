using System;
using System.Collections.Generic;

namespace SevenCrowns.Map
{
    public enum StopReason
    {
        None = 0,
        ReachedGoal,
        InsufficientMP,
        BlockedByTerrain,
        InvalidPath,
    }

    public readonly struct AdvanceResult
    {
        public readonly int StepsCommitted;
        public readonly int MpSpent;
        public readonly GridCoord NewPosition;
        public readonly StopReason Reason;

        public AdvanceResult(int stepsCommitted, int mpSpent, GridCoord newPosition, StopReason reason)
        {
            StepsCommitted = stepsCommitted;
            MpSpent = mpSpent;
            NewPosition = newPosition;
            Reason = reason;
        }
    }

    public readonly struct PreviewResult
    {
        public readonly int StepsPayable;
        public readonly int MpNeeded;

        public PreviewResult(int stepsPayable, int mpNeeded)
        {
            StepsPayable = stepsPayable;
            MpNeeded = mpNeeded;
        }
    }

    public interface IHeroMapAgent
    {
        GridCoord Position { get; }
        GridCoord Goal { get; }
        int RemainingMP { get; }

        bool HasPath { get; }
        IReadOnlyList<GridCoord> Path { get; }
        int PathIndex { get; }

        event Action<GridCoord> PositionChanged;
        event Action<int, int> RemainingMPChanged; // (current,max) relayed from movement service
        event Action Started;
        event Action<GridCoord, GridCoord, int> StepCommitted; // (from,to,cost)
        event Action<StopReason> Stopped;

        bool SetPath(IReadOnlyList<GridCoord> path);
        void ClearPath();

        PreviewResult Preview();
        AdvanceResult AdvanceAllAvailable();
        AdvanceResult AdvanceSteps(int maxSteps);
    }
}

