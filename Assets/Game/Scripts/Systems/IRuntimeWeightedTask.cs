// Assets/Game/Scripts/Systems/IRuntimeWeightedTask.cs
using System;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Optional runtime weighting for preload tasks (used by BootLoaderController).
    /// Implement on top of BasePreloadTask to override the serialized Weight at runtime.
    /// </summary>
    public interface IRuntimeWeightedTask
    {
        /// <summary>Return the effective weight for this task at runtime.</summary>
        float GetRuntimeWeight();
    }
}