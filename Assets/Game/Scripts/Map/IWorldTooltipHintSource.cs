using System;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Abstraction allowing UI systems to consume world tooltip hints without creating Map -> UI dependencies.
    /// </summary>
    public interface IWorldTooltipHintSource
    {
        /// <summary>
        /// Raised whenever the active tooltip hint changes (including when it hides).
        /// </summary>
        event Action<WorldTooltipHint> TooltipHintChanged;

        /// <summary>
        /// Current tooltip hint exposed by the world.
        /// </summary>
        WorldTooltipHint CurrentTooltipHint { get; }
    }
}
