using System;

namespace SevenCrowns.Map.Resources
{
    /// <summary>
    /// Describes a change in resource amounts within the resource wallet.
    /// </summary>
    public readonly struct ResourceChange
    {
        public ResourceChange(string resourceId, int delta, int newAmount)
        {
            ResourceId = resourceId;
            Delta = delta;
            NewAmount = newAmount;
        }

        public string ResourceId { get; }
        public int Delta { get; }
        public int NewAmount { get; }
    }

    /// <summary>
    /// Abstraction for managing player resources (gold, wood, etc.).
    /// Implemented in Core and consumed by map systems via dependency discovery.
    /// </summary>
    public interface IResourceWallet
    {
        event Action<ResourceChange> ResourceChanged;

        /// <summary>Returns the current amount for the specified resource id.</summary>
        int GetAmount(string resourceId);

        /// <summary>Adds the specified amount (positive or negative) to the resource.</summary>
        void Add(string resourceId, int amount);

        /// <summary>Attempts to spend the specified amount if available.</summary>
        bool TrySpend(string resourceId, int amount);
    }
}
