// Assets/Game/Scripts/Systems/AddressablesLoadKeysTask.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Systems; // PreloadRegistry lives here

namespace SevenCrowns.Boot
{
    /// <summary>
    /// A preload task that loads specific Addressables assets by their keys.
    /// This task allows you to specify a list of Addressables keys, which will be loaded
    /// during the preload phase. The loaded assets are then registered with the PreloadRegistry
    /// to keep them alive and retrievable throughout the application's lifecycle.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AddressablesLoadKeysTask",
        menuName = "SevenCrowns/Boot/Addressables Load Keys Task")]
    public class AddressablesLoadKeysTask : BasePreloadTask, IRuntimeWeightedTask
    {
        [SerializeField, Tooltip("Exact Addressables keys to preload (e.g., 'SFX/click', 'UI/AtlasMain')")]
        private List<string> _keys = new();

        /// <summary>Runtime weight = number of keys (1 per asset).</summary>
        public float GetRuntimeWeight() => Mathf.Max(1, _keys?.Count ?? 0);

        /// <summary>
        /// Executes the preload task, loading the specified Addressables assets.
        /// </summary>
        /// <param name="reportProgress">An Action delegate that can be called to report progress.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        public override IEnumerator Run(Action<float> reportProgress)
        {
#if ADDRESSABLES
            var handles = new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>();
            foreach (var key in _keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(key);
                handles.Add(h);
                PreloadRegistry.Register(key, h); // keep alive & retrievable
            }

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
        /// Called during editor validation to update the weight based on the number of keys.
        /// </summary>
        public override void OnValidate()
        {
            try
            {
                var so = new UnityEditor.SerializedObject(this);
                var weightProp = so.FindProperty("_weight"); // from BasePreloadTask
                if (weightProp != null)
                {
                    weightProp.floatValue = Mathf.Max(1, _keys?.Count ?? 0);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            catch { /* ignore editor reload edge cases */ }
        }
#endif
    }
}