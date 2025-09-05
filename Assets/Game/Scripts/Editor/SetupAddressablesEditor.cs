#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace SevenCrowns.EditorTools
{
    public static class SetupAddressablesEditor
    {
        private const string DevProfile   = "Development";
        private const string RelProfile   = "Release";
        private static readonly string[] Groups = {
            "Boot","Frontend","World","Combat","Shared","Audio_Music","Audio_SFX","Localization"
        };

        // Labels are orthogonal to groups; we’ll ensure they exist.
        private static readonly string[] Labels = {
            "boot","frontend","world","combat","shared","ui","audio","music","sfx","localization","gameplay","data"
        };

        [MenuItem("SevenCrowns/Addressables/Setup Lifetime Groups")]
        public static void SetupLifetimeGroups()
        {
            var settings = EnsureSettings();

            // Ensure profiles (Dev/Release) with distinct build/load subpaths
            EnsureProfiles(settings);

            // Ensure labels
            foreach (var label in Labels)
                if (!settings.GetLabels().Contains(label))
                    settings.AddLabel(label);

            // Create or update groups with recommended schemas
            foreach (var groupName in Groups)
            {
                var group = settings.FindGroup(groupName) ?? settings.CreateGroup(
                    groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

                var bundle = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
                var update = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();

                // Build/Load -> Local (profile-controlled); Bundle naming with hash; LZ4 compression
                bundle.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundle.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                bundle.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                bundle.UseAssetBundleCrc = true;
                bundle.UseAssetBundleCache = true;
                bundle.UseAssetBundleCrcForCachedBundles = true;
                bundle.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                //bundle.AssetBundleProviderType = typeof(UnityEngine.AddressableAssets.BundledAssetProvider);
                bundle.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
                bundle.IncludeInBuild = true;

                // Content update flags: mostly static for boot/frontend/shared/localization
                update.StaticContent = groupName is "Boot" or "Frontend" or "Shared" or "Localization";

                EditorUtility.SetDirty(group);
            }

            // Default active profile to Development for editor iteration
            settings.activeProfileId = settings.profileSettings.GetProfileId(DevProfile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Addressables: Settings, profiles, groups, and labels set up.");
        }

        [MenuItem("SevenCrowns/Addressables/Add Seed Entries (click SFX + UI strings)")]
        public static void AddSeedEntries()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found. Run 'Setup Lifetime Groups' first.");
                return;
            }

            // Example assets (adjust paths to your actual files)
            var clickSfxPath = "Assets/Game/Content/Audio/SFX/UI/click.wav";
            var uiStringTableCollectionPath = "Assets/Game/Content/Localization/StringTables/UI/UI_Common.asset"; // collection asset

            // Add click SFX to Audio_SFX group with labels audio,sfx,boot
            AddOrMoveEntry(settings, clickSfxPath, "Audio_SFX", new []{"audio","sfx","boot"});

            // Add UI string table collection to Localization group with labels localization,boot
            AddOrMoveEntry(settings, uiStringTableCollectionPath, "Localization", new []{"localization","boot"});

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Addressables: Seed entries added (click SFX, UI strings).");
        }

        private static AddressableAssetSettings EnsureSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                var settingsPath = "Assets/AddressableAssetsData";
                Directory.CreateDirectory(settingsPath);
                settings = AddressableAssetSettings.Create(settingsPath, "AddressableAssetSettings", true, true);
                AddressableAssetSettingsDefaultObject.Settings = settings;
                Debug.Log("ℹ️ Created Addressables settings at Assets/AddressableAssetsData.");
            }
            return settings;
        }

        private static void EnsureProfiles(AddressableAssetSettings settings)
        {
            var profiles = settings.profileSettings;
            string devId = profiles.GetProfileId(DevProfile);
            string relId = profiles.GetProfileId(RelProfile);

            if (string.IsNullOrEmpty(devId))
            {
                devId = profiles.AddProfile(DevProfile, profiles.GetProfileId("Default"));
                profiles.SetValue(devId, AddressableAssetSettings.kLocalBuildPath,
                    "[UnityEngine.AddressableAssets.Addressables.BuildPath]/Dev");
                profiles.SetValue(devId, AddressableAssetSettings.kLocalLoadPath,
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/Dev");
            }

            if (string.IsNullOrEmpty(relId))
            {
                relId = profiles.AddProfile(RelProfile, profiles.GetProfileId("Default"));
                profiles.SetValue(relId, AddressableAssetSettings.kLocalBuildPath,
                    "[UnityEngine.AddressableAssets.Addressables.BuildPath]/Rel");
                profiles.SetValue(relId, AddressableAssetSettings.kLocalLoadPath,
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/Rel");
            }
        }

        private static void AddOrMoveEntry(AddressableAssetSettings settings, string assetPath, string groupName, IEnumerable<string> labels)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogError($"Group '{groupName}' not found. Run setup first.");
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"Asset not found at path: {assetPath}. Skipping.");
                return;
            }

            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: true);
            }
            else
            {
                settings.MoveEntry(entry, group);
            }

            // Give a readable address (optional); default is the asset path
            entry.address = Path.GetFileNameWithoutExtension(assetPath);

            foreach (var label in labels)
                entry.SetLabel(label, true, true);

            EditorUtility.SetDirty(group);
        }
    }
}
#endif
