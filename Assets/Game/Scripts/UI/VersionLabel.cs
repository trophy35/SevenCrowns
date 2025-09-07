using TMPro;
using UnityEngine;

namespace SevenCrowns.UI
{
    [DisallowMultipleComponent]
    public sealed class VersionLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField, Tooltip("Format tokens: {version}, {platform}, {dev}")]
        private string _format = "v{version} • {platform}{dev}";

        [Header("Optional Build Info (if present)")]
        [SerializeField, Tooltip("If true, append branch/commit/date from Resources/BuildInfo (optional)")]
        private bool _useBuildInfo = true;

        private void Reset()
        {
            if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Awake()
        {
            if (_text == null) return;

            var version = Application.version;                  // From Player Settings > Version
            var platform = Application.platform.ToString();     // e.g., WindowsPlayer
            var dev = Debug.isDebugBuild ? "Dev" : string.Empty;

            string label = _format
                .Replace("{version}", version)
                .Replace("{platform}", platform)
                .Replace("{dev}", dev);

            if (_useBuildInfo)
            {
                var extra = BuildInfoProvider.GetSuffix();
                if (!string.IsNullOrEmpty(extra))
                    label += " " + extra;                      // e.g., (main abc123 2025-09-07)
            }

            _text.text = label;
        }
    }

    // Optional helper; returns empty if no asset exists.
    internal static class BuildInfoProvider
    {
        [System.Serializable]
        private class BuildInfo { public string branch; public string commit; public string date; }

        private static string _cached;

        public static string GetSuffix()
        {
            if (_cached != null) return _cached;
            var json = Resources.Load<TextAsset>("BuildInfo");
            if (json == null) { _cached = string.Empty; return _cached; }

            try
            {
                var info = JsonUtility.FromJson<BuildInfo>(json.text);
                if (info == null) { _cached = string.Empty; return _cached; }

                var parts = new System.Collections.Generic.List<string>(3);
                if (!string.IsNullOrWhiteSpace(info.branch)) parts.Add(info.branch);
                if (!string.IsNullOrWhiteSpace(info.commit)) parts.Add(info.commit);
                if (!string.IsNullOrWhiteSpace(info.date)) parts.Add(info.date);
                _cached = parts.Count > 0 ? "(" + string.Join(" ", parts) + ")" : string.Empty;
            }
            catch { _cached = string.Empty; }
            return _cached;
        }
    }
}

