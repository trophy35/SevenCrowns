using System;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Abstraction for world cursor hints so UI can bind without Map depending on UI.
    /// </summary>
    public interface IWorldCursorHintSource
    {
        /// <summary>
        /// Event fired when cursor hints change. Args: (hoverHero, moveHint).
        /// </summary>
        event Action<bool, bool> CursorHintsChanged;

        /// <summary>True when the pointer is over a hero selectable for current-hero switch.</summary>
        bool HoveringHero { get; }

        /// <summary>True when a click would preview/move to the hovered tile (move mode enabled and path exists).</summary>
        bool MoveHint { get; }
    }
}

