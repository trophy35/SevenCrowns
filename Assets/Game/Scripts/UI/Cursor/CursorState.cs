using System;

namespace SevenCrowns.UI.CursorSystem
{
    /// <summary>
    /// High-level cursor states. Extend as needed for gameplay contexts.
    /// </summary>
    [Serializable]
    public enum CursorState
    {
        Default = 0,
        Hover = 1,
        UI = 2,
        Move = 3,
        Collect = 4,
        Enter = 5,
        Attack = 6,
        Invalid = 7,
    }
}

