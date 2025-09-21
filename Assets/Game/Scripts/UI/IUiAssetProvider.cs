using UnityEngine;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Provides UI-facing access to preloaded assets without creating
    /// assembly cycles between Game.UI and Game.Core.
    /// Implemented in Game.Core and discovered at runtime by UI scripts.
    /// </summary>
    public interface IUiAssetProvider
    {
        bool TryGetSprite(string key, out Sprite sprite);
        bool TryGetAudioClip(string key, out AudioClip clip);
    }
}

