using System;
using System.Collections.Generic;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Tracks a daily pool of movement points with Spend/Refund APIs and UI-friendly events.
    /// Designed to be instantiated per-hero; set Max from hero stats and call ResetDaily() at dawn.
    /// </summary>
    public interface IMapMovementService
    {
        int Max { get; }
        int Current { get; }
        bool IsExhausted { get; }

        event Action<int, int> Changed;   // (current, max)
        event Action<int, int> Spent;     // (amount, currentAfter)
        event Action<int, int> Refunded;  // (amount, currentAfter)
        event Action<int, int> Refilled;  // (max, currentAfter)

        void ResetDaily();
        void SetMax(int newMax, bool refill);

        bool CanSpend(int amount);
        bool TrySpend(int amount);
        int  SpendUpTo(int amount);
        int  Refund(int amount);

        int PreviewSequenceCost(IReadOnlyList<int> stepCosts, out int payableSteps);
        int SpendSequence(IReadOnlyList<int> stepCosts, out int payableSteps);
    }
}

