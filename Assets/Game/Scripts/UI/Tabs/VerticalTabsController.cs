using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SevenCrowns.UI.Tabs
{
    /// <summary>
    /// Manages a vertical list of tab items (top-to-bottom), ensures single selection,
    /// updates focused visuals, and raises selection change events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerticalTabsController : MonoBehaviour
    {
        [Header("Tabs")]
        [Tooltip("Tab items managed by this controller. If empty, auto-discovers in children by hierarchy order.")]
        [SerializeField] private List<VerticalTabItemView> _tabs = new();

        [Tooltip("Select this tab id on start (overrides 'Select First on Start' when not empty).")]
        [SerializeField] private string _defaultTabId = string.Empty;
        [Tooltip("If true and no default id, selects the first tab on start.")]
        [SerializeField] private bool _selectFirstOnStart = true;

        [Header("Colors")]
        [Tooltip("Color for selected tab text and icon.")]
        [SerializeField] private Color32 _focusedColor = new Color32(246, 225, 156, 255); // yellow
        [Tooltip("Color for unfocused tab text and icon.")]
        [SerializeField] private Color32 _unfocusedColor = new Color32(190, 181, 182, 255); // grey

        [Header("Events")]
        [Tooltip("Raised when selection changes, with the new tab id.")]
        [SerializeField] private StringEvent _onSelectionChanged = new StringEvent();

        [Serializable]
        public sealed class StringEvent : UnityEvent<string> { }

        private string _selectedId = string.Empty;
        private bool _wired;

        public string SelectedTabId => _selectedId;
        public UnityEvent<string> OnSelectionChanged => _onSelectionChanged;

        private void Awake()
        {
            EnsureTabs();
        }

        private void OnEnable()
        {
            Wire();
            // Initial selection
            if (!string.IsNullOrWhiteSpace(_defaultTabId))
            {
                SelectById(_defaultTabId);
            }
            else if (_selectFirstOnStart && _tabs.Count > 0)
            {
                SelectById(_tabs[0].TabId);
            }
            else
            {
                // Apply unfocused visuals if none selected
                ApplyVisuals();
            }
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void EnsureTabs()
        {
            if (_tabs == null)
                _tabs = new List<VerticalTabItemView>(4);
            if (_tabs.Count == 0)
            {
                GetComponentsInChildren(true, _tabs);
            }
        }

        private void Wire()
        {
            if (_wired)
                return;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var item = _tabs[i];
                if (item != null && item.Button != null)
                {
                    var id = item.TabId; // capture
                    item.Button.onClick.AddListener(() => SelectById(id));
                }
            }
            _wired = true;
        }

        private void Unwire()
        {
            if (!_wired)
                return;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var item = _tabs[i];
                if (item != null && item.Button != null)
                {
                    item.Button.onClick.RemoveAllListeners();
                }
            }
            _wired = false;
        }

        public void SelectById(string id)
        {
            var normalized = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            if (string.Equals(_selectedId, normalized, System.StringComparison.Ordinal))
            {
                ApplyVisuals();
                return;
            }
            _selectedId = normalized;
            ApplyVisuals();
            _onSelectionChanged?.Invoke(_selectedId);
        }

        public void SelectByIndex(int index)
        {
            if (index < 0 || index >= _tabs.Count)
                return;
            SelectById(_tabs[index].TabId);
        }

        private void ApplyVisuals()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                var item = _tabs[i];
                if (item == null)
                    continue;
                bool focused = !string.IsNullOrEmpty(_selectedId) && string.Equals(item.TabId, _selectedId, StringComparison.Ordinal);
                item.SetFocused(focused, _focusedColor, _unfocusedColor);
            }
        }
    }
}

