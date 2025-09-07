// Assets/Game/Scripts/Systems/LocalizationPreloadTask.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Boot
{
    [CreateAssetMenu(
        fileName = "LocalizationPreloadTask",
        menuName = "SevenCrowns/Boot/Localization Preload Task")]
    public sealed class LocalizationPreloadTask : SevenCrowns.Systems.BasePreloadTask, SevenCrowns.Systems.IRuntimeWeightedTask
    {
#if UNITY_LOCALIZATION
        [Header("Locale Selection")]
        [SerializeField, Tooltip("Use the system language to select the starting locale.")]
        private bool _useSystemLanguage = true;
        [SerializeField, Tooltip("Fallback locale code (BCP-47), e.g. 'en', 'fr'. Used when system language is disabled or unsupported.")]
        private string _fallbackLocaleCode = "en";

        [Header("Tables to Preload")]    
        [SerializeField, Tooltip("String table collection names to preload (e.g., 'Boot', 'UI.Common').")]
        private List<string> _stringTables = new() { "Boot", "UI.Common" };
        [SerializeField, Tooltip("Asset table collection names to preload (optional: localized sprites, audio, etc.).")]
        private List<string> _assetTables = new();

        public float GetRuntimeWeight()
        {
            int count = 1; // init step
            count += (_stringTables?.Count ?? 0);
            count += (_assetTables?.Count ?? 0);
            return Mathf.Max(1, count);
        }

        public override IEnumerator Run(Action<float> reportProgress)
        {
            reportProgress?.Invoke(0f);

            // Phase 1: Ensure Localization is initialized
            var init = UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation;
            const float initWeight = 0.1f; // 10% of the bar reserved for init
            while (!init.IsDone)
            {
                reportProgress?.Invoke(initWeight * Mathf.Clamp01(init.PercentComplete));
                yield return null;
            }
            reportProgress?.Invoke(initWeight);

            // Select locale
            try
            {
                var locales = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales;
                UnityEngine.Localization.Locale selected = null;
                if (_useSystemLanguage)
                {
                    selected = locales.GetLocale(Application.systemLanguage);
                }
                if (selected == null)
                {
                    var id = new UnityEngine.Localization.LocaleIdentifier(string.IsNullOrWhiteSpace(_fallbackLocaleCode) ? "en" : _fallbackLocaleCode);
                    selected = locales.GetLocale(id);
                }
                if (selected != null && UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != selected)
                {
                    UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = selected;
                }
            }
            catch (Exception)
            {
                // Non-fatal: continue without blocking boot
            }

            // Phase 2: Preload requested tables in parallel
            var handles = new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>();
            if (_stringTables != null)
            {
                foreach (var name in _stringTables)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var h = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetTableAsync(name);
                    handles.Add(h);
                }
            }
            if (_assetTables != null)
            {
                foreach (var name in _assetTables)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var h = UnityEngine.Localization.Settings.LocalizationSettings.AssetDatabase.GetTableAsync(name);
                    handles.Add(h);
                }
            }

            int n = Mathf.Max(1, handles.Count);
            bool allDone = n == 0;
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
                float phase2 = (n > 0) ? (sum / n) : 1f;
                float total = initWeight + (1f - initWeight) * phase2;
                reportProgress?.Invoke(Mathf.Clamp01(total));
                yield return null;
            }

            reportProgress?.Invoke(1f);
            yield return null;
        }

#else // UNITY_LOCALIZATION not defined: compile-safe no-op
        public float GetRuntimeWeight() => 1f;
        public override IEnumerator Run(Action<float> reportProgress)
        {
            reportProgress?.Invoke(1f);
            yield return null;
        }
#endif

#if UNITY_EDITOR
        public override void OnValidate()
        {
#if UNITY_LOCALIZATION
            // Trim and dedupe table names for hygiene
            try
            {
                if (_stringTables != null)
                {
                    var set = new HashSet<string>(StringComparer.Ordinal);
                    for (int i = _stringTables.Count - 1; i >= 0; i--)
                    {
                        var trimmed = _stringTables[i]?.Trim();
                        if (string.IsNullOrEmpty(trimmed) || set.Contains(trimmed))
                        {
                            _stringTables.RemoveAt(i);
                        }
                        else
                        {
                            _stringTables[i] = trimmed;
                            set.Add(trimmed);
                        }
                    }
                }
                if (_assetTables != null)
                {
                    var set = new HashSet<string>(StringComparer.Ordinal);
                    for (int i = _assetTables.Count - 1; i >= 0; i--)
                    {
                        var trimmed = _assetTables[i]?.Trim();
                        if (string.IsNullOrEmpty(trimmed) || set.Contains(trimmed))
                        {
                            _assetTables.RemoveAt(i);
                        }
                        else
                        {
                            _assetTables[i] = trimmed;
                            set.Add(trimmed);
                        }
                    }
                }
            }
            catch { /* ignore editor reload edge cases */ }
#endif
        }
#endif
    }
}

