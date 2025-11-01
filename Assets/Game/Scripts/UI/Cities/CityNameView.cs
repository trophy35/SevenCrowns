using UnityEngine;
using TMPro;
using SevenCrowns.UI.Cities;
#if UNITY_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// Binds the current city's localized name to a TextMeshProUGUI in the City scene.
    /// Prefers a string table entry at World.Cities/{cityId}. Falls back to a readable title-case of the id.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CityNameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label; // Optional; defaults to self
        [SerializeField] private string _stringTable = "World.Cities";
        [Header("Debug")] [SerializeField] private bool _debugLogs;

        private void Awake()
        {
            if (_label == null)
            {
                TryGetComponent(out _label);
            }
        }

        private void OnEnable()
        {
            ResolveProvider();
            Apply();
        }

        public void Apply()
        {
            if (_label == null)
            {
                TryGetComponent(out _label);
                if (_label == null) return;
            }

            if (_provider == null) ResolveProvider();
            if (_provider == null)
            {
                if (_debugLogs)
                    Debug.LogWarning("[CityNameView] No ICityNameKeyProvider found in scene.", this);
            }

            string cityId = null;
            if (_provider == null || !_provider.TryGetCityId(out cityId))
            {
                if (_debugLogs)
                    Debug.LogWarning("[CityNameView] Provider missing or did not return a city id.", this);
                return; // no context to display
            }

            string keyCandidate = null;
            if (_provider != null && _provider.TryGetCityNameKey(out var explicitKey) && !string.IsNullOrEmpty(explicitKey))
                keyCandidate = explicitKey;
            else keyCandidate = cityId;

            var key = NormalizeId(keyCandidate);
            string display = null;

            if (_debugLogs)
                Debug.Log($"[CityNameView] Resolved keys: cityId='{cityId}' keyCandidate='{keyCandidate}' table='{_stringTable}'.", this);

#if UNITY_LOCALIZATION
            if (!string.IsNullOrEmpty(_stringTable))
            {
                try
                {
                    display = LocalizationSettings.StringDatabase.GetLocalizedString(_stringTable, key);
                }
                catch { /* non-fatal; fall back below */ }
            }
#endif

            if (string.IsNullOrEmpty(display))
            {
                display = ToTitleCaseFromId(key);
                if (_debugLogs)
                    Debug.Log($"[CityNameView] Using fallback display='{display}'.", this);
            }
            else if (_debugLogs)
            {
                Debug.Log($"[CityNameView] Using localized display='{display}'.", this);
            }

            _label.text = display ?? string.Empty;
        }

        private ICityNameKeyProvider _provider;
        private void ResolveProvider()
        {
            if (_provider != null) return;
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _provider == null; i++)
            {
                if (behaviours[i] is ICityNameKeyProvider p) _provider = p;
            }
            if (_debugLogs)
            {
                if (_provider != null) Debug.Log("[CityNameView] Found ICityNameKeyProvider.", this);
                else Debug.LogWarning("[CityNameView] ICityNameKeyProvider not found during ResolveProvider().", this);
            }
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            id = id.Trim();
            return id.Replace(' ', '.');
        }

        private static string ToTitleCaseFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            // Strip common prefixes
            const string prefix = "city.";
            if (id.StartsWith(prefix)) id = id.Substring(prefix.Length);
            var parts = id.Split(new[] { '.', '-', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p.Length == 0) continue;
                if (p.Length == 1) parts[i] = char.ToUpperInvariant(p[0]).ToString();
                else parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1);
            }
            return string.Join(" ", parts);
        }
    }
}
