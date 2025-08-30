using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Boot
{
    [CreateAssetMenu(
        fileName = "AddressablesLoadKeysTask",
        menuName = "SevenCrowns/Boot/Addressables Load Keys Task")]
    public class AddressablesLoadKeysTask : BasePreloadTask, IRuntimeWeightedTask
    {
        [SerializeField, Tooltip("Exact Addressables keys to preload (e.g., 'SFX/click', 'UI/AtlasMain')")]
        private List<string> _keys = new();

        /// <summary>
        /// Runtime weight = number of keys (1 per asset).
        /// </summary>
        public float GetRuntimeWeight() => Mathf.Max(1, _keys?.Count ?? 0);

        public override IEnumerator Run(System.Action<float> reportProgress)
        {
#if ADDRESSABLES
            // Kick off all loads in parallel so we can smoothly average their progress.
            var handles = new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>();
            foreach (var key in _keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Object>(key);
                handles.Add(h);
                // Register now so assets stay alive when finished and can be reused later.
                PreloadRegistry.Register(key, h);
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

            // Ensure final progress is exactly 1
            reportProgress?.Invoke(1f);
#else
            // Addressables not enabled: no-op
            reportProgress?.Invoke(1f);
            yield return null;
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // (Editor only) Mirror the runtime weight into the serialized Weight
            // so the Inspector gives a hint of the effective weight.
            try
            {
                var so = new UnityEditor.SerializedObject(this);
                var weightProp = so.FindProperty("_weight");
                if (weightProp != null)
                {
                    weightProp.floatValue = Mathf.Max(1, _keys?.Count ?? 0);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            catch
            {
                // Ignore editor exceptions (e.g., during domain reload)
            }
        }
#endif
    }
}

