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
        [SerializeField, Tooltip("Buy button root (optional). If assigned, its visuals will reflect affordability.")]
        private Button _buyButton;
        [SerializeField, Tooltip("Button label Text (TMP). Localized via _buyLabelEntry.")]
        private TextMeshProUGUI _buyLabelText;
        [SerializeField, Tooltip("Optional state image on the button to swap enabled/disabled sprites.")]
        private Image _buyStateImage;
        [SerializeField, Tooltip("Sprite used when the player can afford all costs.")]
        private Sprite _buyEnabledSprite;
        [SerializeField, Tooltip("Sprite used when the player cannot afford all costs.")]
        private Sprite _buyDisabledSprite;
        [SerializeField, Tooltip("Deprecated: use Button Color Tint instead. Kept for backward compatibility.")]
        private Image _buyTintTarget;
        [SerializeField, Tooltip("Tint color when affordable.")]
        private Color _buyAffordableColor = new Color(0.25f, 0.9f, 0.25f, 1f);
        [SerializeField, Tooltip("Tint color when not affordable.")]
        private Color _buyNotAffordableColor = new Color(0.9f, 0.25f, 0.25f, 1f);

        [Header("Localization")]
        [SerializeField] private LocalizedString _nameEntry;
        [SerializeField] private LocalizedString _descriptionEntry;
        [SerializeField, Tooltip("Localized label for the Buy button (UI.Common/City.Buildings.Buy by default).")]
        private LocalizedString _buyLabelEntry;

        [Header("Assets")]
        [SerializeField] private MonoBehaviour _assetProviderBehaviour; // IUiAssetProvider

        private SevenCrowns.UI.IUiAssetProvider _assets;
        private LocalizedString.ChangeHandler _nameHandler;
        private LocalizedString.ChangeHandler _descHandler;
        private LocalizedString.ChangeHandler _buyLabelHandler;
        private string _pendingNameTable;
        private string _pendingNameEntry;
        private string _pendingDescTable;
        private string _pendingDescEntry;
        private string _pendingBuyTable;
        private string _pendingBuyEntry;
        [SerializeField, Tooltip("Enable verbose debug logs for binding and dependency state.")]
        private bool _debugLogs = false;
        [SerializeField, Min(0f), Tooltip("Seconds to retry resolving the icon sprite after auto-load.")]
        private float _lateBindIconTimeout = 2.0f;
        private string _lastIconKey;
        private Coroutine _lateIconRoutine;
        private UiBuildingEntry _boundEntry;
        private SevenCrowns.Map.Resources.IResourceWallet _wallet;

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
            // Default buy label key if not set in inspector
            if (string.IsNullOrEmpty(_pendingBuyEntry))
            {
                SetBuyLabelEntry("UI.Common", "City.Buildings.Buy");
            }
        }

        private void OnEnable()
        {
            ApplyPendingLocalization();
            RefreshLocalizedTexts();
            ResolveWallet();
            SubscribeWallet();
            RefreshBuyAffordability();
        }

        private void OnDestroy()
        {
            UnhookLocalization();
        }

        private void OnDisable()
        {
            UnsubscribeWallet();
        }

        public void Bind(UiBuildingEntry entry, SevenCrowns.UI.IUiAssetProvider assets,
            ICityBuildingStateProvider stateProvider, IResearchStateProvider researchProvider)
        {
            _boundEntry = entry;
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

            // Refresh buy button visuals based on affordability
            RefreshBuyAffordability();
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
            if (_buyLabelText != null)
            {
                if (Application.isPlaying)
                {
                    _buyLabelHandler = value => { if (_buyLabelText != null) _buyLabelText.text = value ?? string.Empty; };
                    try { _buyLabelEntry.StringChanged += _buyLabelHandler; } catch { /* ignore */ }
                }
            }
        }

        private void UnhookLocalization()
        {
            if (_nameHandler != null) { _nameEntry.StringChanged -= _nameHandler; _nameHandler = null; }
            if (_descHandler != null) { _descriptionEntry.StringChanged -= _descHandler; _descHandler = null; }
            if (_buyLabelHandler != null) { _buyLabelEntry.StringChanged -= _buyLabelHandler; _buyLabelHandler = null; }
        }

        private void ApplyPendingLocalization()
        {
            try { _nameEntry.TableReference = string.IsNullOrEmpty(_pendingNameTable) ? "UI.Common" : _pendingNameTable; } catch { }
            try { _nameEntry.TableEntryReference = _pendingNameEntry ?? string.Empty; } catch { }
            try { _descriptionEntry.TableReference = string.IsNullOrEmpty(_pendingDescTable) ? "UI.Common" : _pendingDescTable; } catch { }
            try { _descriptionEntry.TableEntryReference = _pendingDescEntry ?? string.Empty; } catch { }
            try { _buyLabelEntry.TableReference = string.IsNullOrEmpty(_pendingBuyTable) ? "UI.Common" : _pendingBuyTable; } catch { }
            try { _buyLabelEntry.TableEntryReference = _pendingBuyEntry ?? string.Empty; } catch { }
        }

        private void RefreshLocalizedTexts()
        {
            if (!Application.isPlaying)
            {
                if (_nameText != null) _nameText.text = _pendingNameEntry ?? string.Empty;
                if (_descriptionText != null) _descriptionText.text = _pendingDescEntry ?? string.Empty;
                if (_buyLabelText != null) _buyLabelText.text = _pendingBuyEntry ?? string.Empty;
                return;
            }
            _nameEntry.RefreshString();
            _descriptionEntry.RefreshString();
            _buyLabelEntry.RefreshString();
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

        public void SetBuyLabelEntry(string table, string entry)
        {
            _pendingBuyTable = string.IsNullOrEmpty(table) ? "UI.Common" : table;
            _pendingBuyEntry = entry ?? string.Empty;
            ApplyPendingLocalization();
            RefreshLocalizedTexts();
        }

        private void ResolveWallet()
        {
            if (_wallet != null) return;
            // Prefer an explicit ICityWalletProvider in scene (authoritative city wallet)
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _wallet == null; i++)
            {
                if (behaviours[i] is SevenCrowns.UI.Cities.ICityWalletProvider p && p.TryGetWallet(out var cw) && cw != null)
                {
                    _wallet = cw;
                }
            }
            if (_wallet == null)
            {
                for (int i = 0; i < behaviours.Length && _wallet == null; i++)
                {
                    if (behaviours[i] is SevenCrowns.Map.Resources.IResourceWallet w)
                    {
                        _wallet = w;
                    }
                }
            }
        }

        private void SubscribeWallet()
        {
            if (_wallet == null) return;
            _wallet.ResourceChanged += OnWalletChanged;
        }

        private void UnsubscribeWallet()
        {
            if (_wallet == null) return;
            _wallet.ResourceChanged -= OnWalletChanged;
        }

        private void OnWalletChanged(SevenCrowns.Map.Resources.ResourceChange change)
        {
            // If any cost depends on this resource, refresh button
            if (_boundEntry == null || _boundEntry.costs == null || _boundEntry.costs.Length == 0) return;
            for (int i = 0; i < _boundEntry.costs.Length; i++)
            {
                if (string.Equals(_boundEntry.costs[i].resourceId, change.ResourceId, System.StringComparison.Ordinal))
                {
                    RefreshBuyAffordability();
                    return;
                }
            }
        }

        private void RefreshBuyAffordability()
        {
            if (_boundEntry == null) return;
            if (_buyStateImage == null && _buyTintTarget == null) return;
            ResolveWallet();
            bool affordable = true;
            var costs = _boundEntry.costs;
            if (costs != null && costs.Length > 0)
            {
                if (_wallet == null)
                {
                    affordable = false; // No wallet to evaluate against
                }
                else
                {
                    for (int i = 0; i < costs.Length; i++)
                    {
                        var c = costs[i];
                        if (string.IsNullOrEmpty(c.resourceId) || c.amount <= 0) continue;
                        int have = _wallet.GetAmount(c.resourceId);
                        if (have < c.amount) { affordable = false; break; }
                    }
                }
            }

            if (_buyStateImage != null)
            {
                if (_buyEnabledSprite != null && _buyDisabledSprite != null)
                {
                    _buyStateImage.sprite = affordable ? _buyEnabledSprite : _buyDisabledSprite;
                    _buyStateImage.enabled = true;
                }
                else
                {
                    // If sprites not assigned, do not force-enable the image
                    _buyStateImage.enabled = _buyStateImage.sprite != null;
                }
            }
            // Apply color tint on the Button component
            if (_buyButton != null)
            {
                var colors = _buyButton.colors; // struct
                var baseColor = affordable ? _buyAffordableColor : _buyNotAffordableColor;
                colors.highlightedColor = baseColor;
                _buyButton.colors = colors;
                if (_buyButton.transition == UnityEngine.UI.Selectable.Transition.None)
                    _buyButton.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            }
            // Back-compat: also tint optional target image if assigned
            if (_buyTintTarget != null)
            {
                _buyTintTarget.color = affordable ? _buyAffordableColor : _buyNotAffordableColor;
            }
            if (_debugLogs)
            {
                Debug.Log($"[BuildingItem] Buy affordability for '{_boundEntry.buildingId}': {affordable}", this);
            }
        }
    }
}
