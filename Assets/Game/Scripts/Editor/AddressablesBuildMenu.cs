// Utility menu to build/clean Addressables content outside of Player build.
using UnityEditor;

#if ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SevenCrowns.Editor
{
    public static class AddressablesBuildMenu
    {
        [MenuItem("Tools/SevenCrowns/Build Addressables Now", priority = 200)]
        public static void BuildAddressables()
        {
#if ADDRESSABLES
            AddressableAssetSettings.BuildPlayerContent();
            UnityEngine.Debug.Log("[SevenCrowns] Addressables build completed.");
#else
            UnityEngine.Debug.LogWarning("[SevenCrowns] ADDRESSABLES define not set; cannot build Addressables.");
#endif
        }

        [MenuItem("Tools/SevenCrowns/Clean Addressables Build", priority = 201)]
        public static void CleanAddressables()
        {
#if ADDRESSABLES
            // Best-effort clean: run clean then build cache purge step if available in your version
            AddressableAssetSettings.CleanPlayerContent();
            UnityEngine.Debug.Log("[SevenCrowns] Addressables build cleaned.");
#else
            UnityEngine.Debug.LogWarning("[SevenCrowns] ADDRESSABLES define not set; cannot clean Addressables.");
#endif
        }
    }
}

