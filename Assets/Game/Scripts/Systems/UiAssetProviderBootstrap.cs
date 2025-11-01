using UnityEngine;
using SevenCrowns.UI;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Ensures an IUiAssetProvider exists at runtime. If none is found in the scene,
    /// creates a persistent GameObject with <see cref="PreloadRegistryAssetProvider"/>.
    /// Keeps UI decoupled by handling the bootstrap on the Core side.
    /// </summary>
    public static class UiAssetProviderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureOnLoad()
        {
            Ensure();
        }

        /// <summary>
        /// Ensures there is at least one active IUiAssetProvider in the scene.
        /// </summary>
        public static void Ensure()
        {
            if (!Application.isPlaying) return;

            // Already present?
            var behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
            int existing = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IUiAssetProvider)
                {
                    existing++;
                }
            }
            if (existing > 0)
            {
                Debug.Log($"[UiAssetProviderBootstrap] Found {existing} existing IUiAssetProvider instance(s).", behaviours.Length > 0 ? behaviours[0] : null);
                return;
            }

            // Create a persistent provider object
            var go = new GameObject("UiAssetProvider");
            var provider = go.AddComponent<PreloadRegistryAssetProvider>();
            Object.DontDestroyOnLoad(go);
            Debug.Log("[UiAssetProviderBootstrap] Created PreloadRegistryAssetProvider as fallback.", provider);
            // Default options are fine: auto-load is enabled in the provider
        }
    }
}
