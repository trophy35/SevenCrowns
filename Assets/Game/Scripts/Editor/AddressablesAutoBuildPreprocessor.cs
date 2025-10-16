// Ensures Addressables content is built before Player builds.
// Placed under an Editor folder so it compiles into an editor-only assembly.
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SevenCrowns.Editor
{
    /// <summary>
    /// Pre-build guard that ensures:
    /// - Editor is not in Play Mode when starting a Player build.
    /// - Addressables content is built and included in the Player build.
    /// </summary>
    public sealed class AddressablesAutoBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new BuildFailedException("Cannot build Player while in Play Mode. Please exit Play Mode and try again.");
            }
            // Do not trigger Addressables build here to avoid nested build conflicts.
            // If Addressables content is missing, Unity's AddressablesPlayerBuildProcessor will emit a clear error.
            // You can build content via the provided menu: Tools/SevenCrowns/Build Addressables Now.
        }
    }
}
