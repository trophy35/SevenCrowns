using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI.Tabs;

namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// Toggles City tab content panels based on the selected tab id from a VerticalTabsController.
    /// Keep this UI-only; external systems should not depend on it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityTabsContentController : MonoBehaviour
    {
        [Serializable]
        public sealed class TabContent
        {
            [Tooltip("Tab id to bind (e.g., 'city.building').")]
            public string tabId = string.Empty;
            [Tooltip("Root GameObject for this tab's content panel.")]
            public GameObject root;
        }

        [Header("Source")]
        [Tooltip("Tabs controller providing the current selection. If null, discovered in parents.")]
        [SerializeField] private VerticalTabsController _tabsController;

        [Header("Content Panels")]
        [Tooltip("Mappings from tab id to its content root GameObject.")]
        [SerializeField] private List<TabContent> _contents = new();

        private bool _wired;

        private void Awake()
        {
            if (_tabsController == null)
                _tabsController = GetComponentInParent<VerticalTabsController>();
        }

        private void OnEnable()
        {
            Wire();
            // Sync once on enable using the current selection.
            Apply(_tabsController != null ? _tabsController.SelectedTabId : string.Empty);
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void Wire()
        {
            if (_wired)
                return;
            if (_tabsController != null)
            {
                _tabsController.OnSelectionChanged.AddListener(Apply);
                _wired = true;
            }
        }

        private void Unwire()
        {
            if (!_wired)
                return;
            if (_tabsController != null)
            {
                _tabsController.OnSelectionChanged.RemoveListener(Apply);
            }
            _wired = false;
        }

        private void Apply(string selectedId)
        {
            if (_contents == null || _contents.Count == 0)
            {
                return;
            }

            bool anyMatched = false;
            string normalized = string.IsNullOrWhiteSpace(selectedId) ? string.Empty : selectedId.Trim();

            for (int i = 0; i < _contents.Count; i++)
            {
                var c = _contents[i];
                if (c.root == null)
                    continue;
                bool match = !string.IsNullOrEmpty(normalized) && string.Equals(c.tabId, normalized, StringComparison.Ordinal);
                if (match)
                    anyMatched = true;
                if (c.root.activeSelf != match)
                    c.root.SetActive(match);
            }

            if (!anyMatched)
            {
                // If selection is empty or does not match any mapping, deactivate all to avoid stale content.
                DeactivateAll();
            }
        }

        private void DeactivateAll()
        {
            if (_contents == null)
                return;
            for (int i = 0; i < _contents.Count; i++)
            {
                var c = _contents[i];
                if (c.root != null && c.root.activeSelf)
                    c.root.SetActive(false);
            }
        }
    }
}

