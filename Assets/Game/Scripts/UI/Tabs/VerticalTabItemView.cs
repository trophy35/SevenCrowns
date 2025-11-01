using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace SevenCrowns.UI.Tabs
{
    /// <summary>
    /// Visual and interaction logic for a single vertical tab item.
    /// Displays an icon, a localized label and a focus line on the left when selected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerticalTabItemView : MonoBehaviour
    {
        private const string DefaultTable = "UI.Common";

        [Header("Identity")]
        [Tooltip("Unique id for this tab (e.g., 'city.building').")]
        [SerializeField] private string _tabId = string.Empty;

        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _label;
        [Tooltip("Focus line GameObject shown on the left when selected.")]
        [SerializeField] private GameObject _focusLine;

        [Header("Localization")]
        [Tooltip("Localized label entry for this tab (table defaults to UI.Common).")]
        [SerializeField] private LocalizedString _labelEntry;

        private LocalizedString.ChangeHandler _labelHandler;
        private string _pendingTable;
        private string _pendingEntry;

        public string TabId => _tabId;
        public Button Button => _button;
        public TextMeshProUGUI Label => _label;
        public Image Icon => _iconImage;

        private void Awake()
        {
            // Only ensure component references in Edit Mode and runtime; avoid touching Localization here.
            EnsureRefsOnly();
            HookLabel();
            ApplyLabelEntrySafe();
        }

        private void OnEnable()
        {
            ApplyLabelEntrySafe();
            RefreshLabel();
        }

        private void OnDestroy()
        {
            UnhookLabel();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // In Edit Mode validation, avoid touching Localization structures to prevent editor-time assertions.
            EnsureRefsOnly();
        }
#endif

        /// <summary>
        /// Applies selected/unselected visuals.
        /// </summary>
        public void SetFocused(bool focused, Color32 focusedColor, Color32 unfocusedColor)
        {
            if (_focusLine != null)
            {
                if (_focusLine.activeSelf != focused)
                    _focusLine.SetActive(focused);
            }

            var color = focused ? (Color)focusedColor : (Color)unfocusedColor;
            if (_label != null)
            {
                _label.color = color;
            }
            if (_iconImage != null)
            {
                _iconImage.color = color;
            }
        }

        private void EnsureDefaults()
        {
            // Historical name kept for compatibility; now only ensures refs.
            EnsureRefsOnly();
        }

        private void EnsureRefsOnly()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_iconImage == null)
                _iconImage = GetComponentInChildren<Image>(true);
            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void HookLabel()
        {
            if (_label == null)
                return;
            if (Application.isPlaying)
            {
                _labelHandler = value =>
                {
                    if (_label != null)
                        _label.text = value ?? string.Empty;
                };
                try
                {
                    _labelEntry.StringChanged += _labelHandler;
                }
                catch
                {
                    // Ignore localization hookup issues in unexpected states.
                }
            }
            else
            {
                // In Edit Mode tests, set immediate text without subscribing to localization events.
                _label.text = _pendingEntry ?? string.Empty;
            }
        }

        private void UnhookLabel()
        {
            if (_labelHandler != null)
            {
                _labelEntry.StringChanged -= _labelHandler;
                _labelHandler = null;
            }
        }

        private void RefreshLabel()
        {
            if (_label == null)
                return;

            // In Edit Mode tests, LocalizationSettings may not be initialized; avoid throwing and display the entry key.
            if (!Application.isPlaying)
            {
                _label.text = _pendingEntry ?? string.Empty;
                return;
            }

            _labelEntry.RefreshString();
        }

        public void SetLabelEntry(string table, string entry)
        {
            _pendingTable = string.IsNullOrEmpty(table) ? DefaultTable : table;
            _pendingEntry = entry ?? string.Empty;
            ApplyLabelEntrySafe();
            RefreshLabel();
        }

        public void SetId(string id)
        {
            _tabId = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        private void ApplyLabelEntrySafe()
        {
            // Apply pending values to the LocalizedString if possible; guard to avoid Edit Mode NREs.
            var table = string.IsNullOrEmpty(_pendingTable) ? DefaultTable : _pendingTable;
            var entry = _pendingEntry ?? string.Empty;
            try
            {
                _labelEntry.TableReference = table;
                _labelEntry.TableEntryReference = entry;
            }
            catch
            {
                // Swallow localization initialization issues in Edit Mode tests.
            }
        }
    }
}
