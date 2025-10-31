using System;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Optional extension interface for world cursor hints to expose a dedicated Enter hint (e.g., owned city entry).
    /// </summary>
    public interface IWorldCursorEnterHintSource
    {
        /// <summary>Raised when the dedicated Enter hint visibility changes.</summary>
        event Action<bool> EnterHintChanged;

        /// <summary>True when the cursor should use the Enter state (owned city entry available).</summary>
        bool EnterHint { get; }
    }
}

