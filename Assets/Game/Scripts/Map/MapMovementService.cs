using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Default implementation of IMapMovementService. Engine-agnostic and allocation-aware.
    /// Manages a pool of movement points, typically for a hero on the map.
    /// Instantiate per-hero; set Max from hero stats; call ResetDaily() at dawn.
    /// </summary>
    public sealed class MapMovementService : IMapMovementService
    {
        /// <inheritdoc />
        public int Max => _max;
        /// <inheritdoc />
        public int Current => _current;
        /// <inheritdoc />
        public bool IsExhausted => _current == 0;

        /// <inheritdoc />
        public event Action<int, int> Changed;
        /// <inheritdoc />
        public event Action<int, int> Spent;
        /// <inheritdoc />
        public event Action<int, int> Refunded;
        /// <inheritdoc />
        public event Action<int, int> Refilled;

        private int _max;
        private int _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapMovementService"/> class.
        /// </summary>
        /// <param name="max">The maximum movement points available. Defaults to 240. Must be non-negative.</param>
        /// <param name="current">The starting movement points. If null, defaults to the max value.</param>
        public MapMovementService(int max = 240, int? current = null)
        {
            if (max < 0) max = 0;
            _max = max;
            _current = current.HasValue ? Clamp(current.Value, 0, _max) : _max;
        }

        /// <summary>
        /// Resets the current movement points to the maximum, typically at the start of a new day or turn.
        /// </summary>
        public void ResetDaily()
        {
            UnityEngine.Debug.Log("ResetDaily");
            _current = _max;
            Refilled?.Invoke(_max, _current);
            Changed?.Invoke(_current, _max);
        }

        /// <summary>
        /// Sets a new maximum for movement points.
        /// </summary>
        /// <param name="newMax">The new maximum value. Must be non-negative.</param>
        /// <param name="refill">If true, the current points are refilled to the new maximum.</param>
        public void SetMax(int newMax, bool refill)
        {
            if (newMax < 0) newMax = 0;
            _max = newMax;
            if (refill)
            {
                _current = _max;
                Refilled?.Invoke(_max, _current);
            }
            else if (_current > _max)
            {
                // Clamp current points if they exceed the new max
                _current = _max;
            }
            Changed?.Invoke(_current, _max);
        }

        /// <summary>
        /// Checks if a given amount of movement points can be spent.
        /// </summary>
        /// <param name="amount">The amount to check.</param>
        /// <returns>True if the amount is positive and does not exceed the current points.</returns>
        public bool CanSpend(int amount)
        {
            return amount > 0 && amount <= _current;
        }

        /// <summary>
        /// Attempts to spend a specific amount of movement points.
        /// </summary>
        /// <param name="amount">The amount to spend.</param>
        /// <returns>True if the points were successfully spent, false otherwise.</returns>
        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount)) return false;
            _current -= amount;
            Spent?.Invoke(amount, _current);
            Changed?.Invoke(_current, _max);
            return true;
        }

        /// <summary>
        /// Spends up to a specified amount of movement points, consuming no more than what is available.
        /// </summary>
        /// <param name="amount">The desired amount to spend.</param>
        /// <returns>The actual amount of points spent.</returns>
        public int SpendUpTo(int amount)
        {
            if (amount <= 0 || _current == 0) return 0;
            int spent = amount <= _current ? amount : _current;
            _current -= spent;
            if (spent > 0)
            {
                Spent?.Invoke(spent, _current);
                Changed?.Invoke(_current, _max);
            }
            return spent;
        }

        /// <summary>
        /// Refunds a given amount of movement points, up to the maximum capacity.
        /// </summary>
        /// <param name="amount">The amount to refund.</param>
        /// <returns>The actual amount refunded, which may be less than requested if at max capacity.</returns>
        public int Refund(int amount)
        {
            if (amount <= 0) return 0;
            int space = _max - _current; // How much room is there to refund?
            if (space <= 0) return 0;
            int refunded = amount <= space ? amount : space;
            _current += refunded;
            Refunded?.Invoke(refunded, _current);
            Changed?.Invoke(_current, _max);
            return refunded;
        }

        /// <summary>
        /// Calculates the total cost of a sequence of steps and determines how many are affordable.
        /// This method does not alter the current movement points.
        /// </summary>
        /// <param name="stepCosts">A list of costs for each step in a potential path.</param>
        /// <param name="payableSteps">The number of steps from the start of the sequence that can be afforded.</param>
        /// <returns>The total cost for the sequence of payable steps.</returns>
        public int PreviewSequenceCost(IReadOnlyList<int> stepCosts, out int payableSteps)
        {
            if (stepCosts == null || stepCosts.Count == 0)
            {
                payableSteps = 0;
                return 0;
            }
            int total = 0;
            int steps = 0;
            int remaining = _current;
            for (int i = 0; i < stepCosts.Count; i++)
            {
                int c = stepCosts[i];
                if (c <= 0) continue; // Skip invalid/zero costs
                if (c > remaining) break; // Not enough points for this step
                remaining -= c;
                total += c;
                steps++;
            }
            payableSteps = steps;
            return total;
        }

        /// <summary>
        /// Spends movement points for a sequence of steps, consuming only what is affordable.
        /// </summary>
        /// <param name="stepCosts">A list of costs for each step in the path.</param>
        /// <param name="payableSteps">The number of steps that were actually paid for.</param>
        /// <returns>The total cost of the steps paid for.</returns>
        public int SpendSequence(IReadOnlyList<int> stepCosts, out int payableSteps)
        {
            // First, determine the affordable cost and number of steps without spending.
            int total = PreviewSequenceCost(stepCosts, out payableSteps);
            if (total > 0)
            {
                // If any steps are payable, commit the spending.
                _current -= total;
                Spent?.Invoke(total, _current);
                Changed?.Invoke(_current, _max);
            }
            return total;
        }

        /// <summary>
        /// Clamps an integer value to be within a specified range.
        /// </summary>
        /// <param name="v">The value to clamp.</param>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <returns>The clamped value.</returns>
        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}

