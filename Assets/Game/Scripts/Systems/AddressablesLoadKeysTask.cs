// Assets/Game/Scripts/Systems/AddressablesLoadKeysTask.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Systems; // PreloadRegistry lives here

namespace SevenCrowns.Boot
{
    /// <summary>
    /// Preload task that can load Addressables assets via:
    /// - Explicit keys
    /// - Labels (bulk)
    /// - Group names (bulk) mapped to labels of the form "group:<GroupName>"
    /// Loaded assets are registered into PreloadRegistry to keep them alive.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AddressablesLoadKeysTask",
        menuName = "SevenCrowns/Boot/Addressables Load Keys Task")]
    public class AddressablesLoadKeysTask : BasePreloadTask, IRuntimeWeightedTask
    {
        [SerializeField, Tooltip("Exact Addressables keys to preload (e.g., 'SFX/click', 'UI/AtlasMain')")]
        private List<string> _keys = new();

        [SerializeField, Tooltip("Addressables labels to bulk preload (e.g., 'ui.icons', 'sfx.ui').")]
        private List<string> _labels = new();

        [SerializeField, Tooltip("Addressables group names to bulk preload. Requires entries to have a label 'group:<GroupName>'. You can stamp these via an editor utility.")]
        private List<string> _groupNames = new();

        private const string GroupLabelPrefix = "group:";

        /// <summary>Runtime weight estimate; keys provide a stable count, labels/groups resolved at runtime.</summary>
        public float GetRuntimeWeight()
        {
            int k = Mathf.Max(0, _keys?.Count ?? 0);
            int l = Mathf.Max(0, _labels?.Count ?? 0);
            int g = Mathf.Max(0, _groupNames?.Count ?? 0);
            // Use a conservative estimate so task ordering remains reasonable.
            return Mathf.Max(1, k + l + g);
        }

        /// <summary>
        /// Executes the preload task, loading the specified Addressables assets.
        /// </summary>
        public override IEnumerator Run(Action<float> reportProgress)
        {
#if ADDRESSABLES
            // 1) Discover addresses to load from keys + labels + groups.
            var addresses = new HashSet<string>(StringComparer.Ordinal);

            // From explicit keys.
            if (_keys != null)
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    var key = _keys[i];
                    if (!string.IsNullOrWhiteSpace(key))
                        addresses.Add(key);
                }
            }

            // Build combined label list: explicit labels + labels mapped from groups.
            var labelsToQuery = new List<string>();
            if (_labels != null)
            {
                for (int i = 0; i < _labels.Count; i++)
                {
                    var lab = _labels[i];
                    if (!string.IsNullOrWhiteSpace(lab))
                        labelsToQuery.Add(lab);
                }
            }

            if (_groupNames != null)
            {
                for (int i = 0; i < _groupNames.Count; i++)
                {
                    var gname = _groupNames[i];
                    if (string.IsNullOrWhiteSpace(gname)) continue;
                    var mapped = GroupLabelPrefix + gname;
                    labelsToQuery.Add(mapped);
                }
            }

            // From labels (explicit + group-mapped).
            for (int i = 0; i < labelsToQuery.Count; i++)
            {
                var label = labelsToQuery[i];
                var hLoc = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(label, typeof(UnityEngine.Object));
                yield return hLoc;

                if (hLoc.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    var list = hLoc.Result;
                    for (int j = 0; j < list.Count; j++)
                    {
                        var loc = list[j];
                        var primary = loc?.PrimaryKey;
                        if (!string.IsNullOrWhiteSpace(primary))
                            addresses.Add(primary);
                    }
                }

                UnityEngine.AddressableAssets.Addressables.Release(hLoc);
                // Small bump to progress to reflect discovery work.
                reportProgress?.Invoke(Mathf.Clamp01(0.02f * (i + 1)));
            }

            // 2) Kick off loads for all discovered addresses.
            var handles = new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>(addresses.Count);
            foreach (var addr in addresses)
            {
                var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(addr);
                handles.Add(h);
                PreloadRegistry.Register(addr, h); // keep alive & retrievable by address
            }

            // 3) Progress loop.
            int n = Mathf.Max(1, handles.Count);
            bool allDone = false;
            while (!allDone)
            {
                float sum = 0f;
                allDone = true;
                for (int i = 0; i < handles.Count; i++)
                {
                    var h = handles[i];
                    sum += h.PercentComplete;
                    if (!h.IsDone) allDone = false;
                }
                reportProgress?.Invoke(sum / n);
                yield return null;
            }

            reportProgress?.Invoke(1f);
#else
            // If Addressables is not enabled, report completion immediately.
            reportProgress?.Invoke(1f);
            yield return null;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-time validation: try to estimate weight using keys + editor info for labels/groups.
        /// </summary>
        public override void OnValidate()
        {
            try
            {
                // Base invariants from parent.
                base.OnValidate();
                // Keep the estimation simple and editor-API free to avoid editor-only assembly references.
                int estimated = 0;
                estimated += Mathf.Max(0, _keys?.Count ?? 0);
                estimated += Mathf.Max(0, _labels?.Count ?? 0);
                estimated += Mathf.Max(0, _groupNames?.Count ?? 0);

                var so = new UnityEditor.SerializedObject(this);
                var weightProp = so.FindProperty("_weight"); // from BasePreloadTask
                if (weightProp != null)
                {
                    weightProp.floatValue = Mathf.Max(1, estimated);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            catch { /* ignore editor reload edge cases */ }
        }
#endif
    }
}
