using System;

namespace SevenCrowns.UI
{
    /// <summary>
    /// UI-facing provider for current hero identity and its portrait Addressables key.
    /// Implement this in Core; UI discovers it at runtime.
    /// </summary>
    public interface ICurrentHeroPortraitKeyProvider
    {
        string CurrentHeroId { get; }
        string CurrentPortraitKey { get; }
        event Action<string, string> CurrentHeroChanged; // (heroId, portraitKey)
    }
}

