using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// Visual binding for a single building entry in the list.
    /// Name/Description use Localization; Edit Mode tests get fallback of entry keys.
    /// Icon resolved via IUiAssetProvider.
    /// Grays out when dependencies are not met.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingListItemView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Transform _costContainer;
        [SerializeField] private CostPillView _costPillPrefab;
        [SerializeField] private CanvasGroup _canvasGroup; // Optional: used to dim when locked

        [Header("Localization")]
        [SerializeField] private LocalizedString _nameEntry;
        [SerializeField] private LocalizedString _descriptionEntry;

        [Header("Assets")]
        [SerializeField] private MonoBehaviour _assetProviderBehaviour; // IUiAssetProvider

        private SevenCrowns.UI.IUiAssetProvider _assets;
        private LocalizedString.ChangeHandler _nameHandler;
        private LocalizedString.ChangeHandler _descHandler;
        private string _pendingNameTable;
        private string _pendingNameEntry;
        private string _pendingDescTable;
        private string _pendingDescEntry;
        [SerializeField, Tooltip("Enable verbose debug logs for binding and dependency state.")]
        private bool _debugLogs = false;
        [SerializeField, Min(0f), Tooltip("Seconds to retry resolving the icon sprite after auto-load.")]
        private float _lateBindIconTimeout = 2.0f;
        private string _lastIconKey;
        private Coroutine _lateIconRoutine;

        private void Awake()
        {
            if (_icon == null) _icon = GetComponentInChildren<Image>(true);
            if (_nameText == null) _nameText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (_descriptionText == null)
            {
                // When not present in prefab, tolerate null; description is optional in minimal test setups.
            }
            if (_assetProviderBehaviour != null && _assetProviderBehaviour is SevenCrowns.UI.IUiAssetProvider a)
                _assets = a;

            HookLocalization();
            ApplyPendingLocalization();
        }

        private void OnEnable()
        {
            ApplyPendingLocalization();
            RefreshLocalizedTexts();
        }

        private void OnDestroy()
        {
            UnhookLocalization();
        }

        public void Bind(UiBuildingEntry entry, SevenCrowns.UI.IUiAssetProvider assets,
            ICityBuildingStateProvider stateProvider, IResearchStateProvider researchProvider)
        {
            _assets = _assets ?? assets;

            // Name/Description localization keys
            SetNameEntry(entry.nameTable, entry.nameEntry);
            SetDescriptionEntry(entry.descriptionTable, entry.descriptionEntry);
            if (_debugLogs)
            {
                Debug.Log($"[BuildingItem] Bind buildingId='{entry?.buildingId}' name='{entry?.nameEntry}' desc='{entry?.descriptionEntry}' costs={(entry?.costs?.Length ?? 0)}", this);
            }

            // Icon
            if (_icon != null)
            {
                _lastIconKey = entry.iconKey;
                if (_assets != null && !string.IsNullOrEmpty(entry.iconKey) && _assets.TryGetSprite(entry.iconKey, out var sprite) && sprite != null)
                {
                    _icon.sprite = sprite;
                    _icon.enabled = true;
                    if (_debugLogs) Debug.Log($"[BuildingItem] Icon resolved for '{entry.buildingId}' key='{entry.iconKey}'.", this);
                }
                else
                {
                    _icon.enabled = false; // avoid showing placeholder if unresolved
                    if (_debugLogs) Debug.LogWarning($"[BuildingItem] Icon NOT resolved for '{entry.buildingId}'. key='{entry.iconKey}'.", this);
                    // Late-bind retry: allow Addressables auto-load to complete and then re-apply
                    if (Application.isPlaying && _lateBindIconTimeout > 0f && !string.IsNullOrEmpty(_lastIconKey))
                    {
                        if (_lateIconRoutine != null) StopCoroutine(_lateIconRoutine);
                        _lateIconRoutine = StartCoroutine(LateBindIcon());
                    }
                }
            }

            // Costs
            if (_costContainer != null && _costPillPrefab != null)
            {
                // Clear previous
                for (int i = _costContainer.childCount - 1; i >= 0; i--)
                {
                    var child = _costContainer.GetChild(i);
                    if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
                }

                var costs = entry.costs;
                if (costs != null)
                {
                    for (int i = 0; i < costs.Length; i++)
                    {
                        var c = costs[i];
                        var pill = Instantiate(_costPillPrefab, _costContainer);
                        pill.Bind(c.resourceId, c.amount);
                        if (_debugLogs)
                            Debug.Log($"[BuildingItem] Added cost pill: resource='{c.resourceId}' amount={c.amount}", this);
                    }
                }
            }

            // Lock state (dim when not buildable yet)
            bool locked = HasUnmetDependencies(entry, stateProvider, researchProvider);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = locked ? 0.5f : 1f;
                _canvasGroup.interactable = !locked;
                _canvasGroup.blocksRaycasts = !locked;
            }
            if (_debugLogs)
                Debug.Log($"[BuildingItem] Dependency state for '{entry.buildingId}': locked={locked}", this);
        }

        private System.Collections.IEnumerator LateBindIcon()
        {
            float t = 0f;
            while (t < _lateBindIconTimeout)
            {
                if (_assets != null && !string.IsNullOrEmpty(_lastIconKey) && _assets.TryGetSprite(_lastIconKey, out var sprite) && sprite != null)
                {
                    if (_icon != null)
                    {
                        _icon.sprite = sprite;
                        _icon.enabled = true;
                    }
                    if (_debugLogs) Debug.Log($"[BuildingItem] LateBind icon success for key='{_lastIconKey}' at t={t:0.00}s", this);
                    _lateIconRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_debugLogs) Debug.LogWarning($"[BuildingItem] LateBind icon timeout for key='{_lastIconKey}' after {t:0.00}s", this);
            _lateIconRoutine = null;
        }

        private static bool HasUnmetDependencies(UiBuildingEntry entry, ICityBuildingStateProvider state, IResearchStateProvider research)
        {
            if (entry == null) return false;
            // If no dependency, it is buildable
            bool hasDeps = (entry.requiredBuildingIds != null && entry.requiredBuildingIds.Length > 0)
                           || (entry.requiredResearchIds != null && entry.requiredResearchIds.Length > 0);
            if (!hasDeps) return false;

            // If dependencies exist, require all to be satisfied
            if (entry.requiredBuildingIds != null)
            {
                for (int i = 0; i < entry.requiredBuildingIds.Length; i++)
                {
                    var id = entry.requiredBuildingIds[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    if (state == null || !state.IsBuilt(id)) return true; // unmet
                }
            }
            if (entry.requiredResearchIds != null)
            {
                for (int i = 0; i < entry.requiredResearchIds.Length; i++)
                {
                    var rid = entry.requiredResearchIds[i];
                    if (string.IsNullOrEmpty(rid)) continue;
                    if (research == null || !research.IsCompleted(rid)) return true; // unmet
                }
            }
            return false;
        }

        private void HookLocalization()
        {
            if (_nameText != null)
            {
                if (Application.isPlaying)
                {
                    _nameHandler = value => { if (_nameText != null) _nameText.text = value ?? string.Empty; };
                    try { _nameEntry.StringChanged += _nameHandler; } catch { /* ignore */ }
                }
            }
            if (_descriptionText != null)
            {
                if (Application.isPlaying)
                {
                    _descHandler = value => { if (_descriptionText != null) _descriptionText.text = value ?? string.Empty; };
                    try { _descriptionEntry.StringChanged += _descHandler; } catch { /* ignore */ }
                }
            }
        }

        private void UnhookLocalization()
        {
            if (_nameHandler != null) { _nameEntry.StringChanged -= _nameHandler; _nameHandler = null; }
            if (_descHandler != null) { _descriptionEntry.StringChanged -= _descHandler; _descHandler = null; }
        }

        private void ApplyPendingLocalization()
        {
            try { _nameEntry.TableReference = string.IsNullOrEmpty(_pendingNameTable) ? "UI.Common" : _pendingNameTable; } catch { }
            try { _nameEntry.TableEntryReference = _pendingNameEntry ?? string.Empty; } catch { }
            try { _descriptionEntry.TableReference = string.IsNullOrEmpty(_pendingDescTable) ? "UI.Common" : _pendingDescTable; } catch { }
            try { _descriptionEntry.TableEntryReference = _pendingDescEntry ?? string.Empty; } catch { }
        }

        private void RefreshLocalizedTexts()
        {
            if (!Application.isPlaying)
            {
                if (_nameText != null) _nameText.text = _pendingNameEntry ?? string.Empty;
                if (_descriptionText != null) _descriptionText.text = _pendingDescEntry ?? string.Empty;
                return;
            }
            _nameEntry.RefreshString();
            _descriptionEntry.RefreshString();
        }

        public void SetNameEntry(string table, string entry)
        {
            _pendingNameTable = string.IsNullOrEmpty(table) ? "UI.Common" : table;
            _pendingNameEntry = entry ?? string.Empty;
            ApplyPendingLocalization();
            RefreshLocalizedTexts();
        }

        public void SetDescriptionEntry(string table, string entry)
        {
            _pendingDescTable = string.IsNullOrEmpty(table) ? "UI.Common" : table;
            _pendingDescEntry = entry ?? string.Empty;
            ApplyPendingLocalization();
            RefreshLocalizedTexts();
        }
    }
}
