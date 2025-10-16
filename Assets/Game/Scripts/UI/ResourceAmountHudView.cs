using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using SevenCrowns.Map.Resources;
using SevenCrowns.Map.Mines;
using UnityEngine.EventSystems;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Displays the current amount of a specific resource from IResourceWallet.
    /// Updates automatically when the wallet raises ResourceChanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceAmountHudView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string DefaultTable = "UI.Common";

        [Header("Wallet")]
        [SerializeField] private MonoBehaviour _walletBehaviour; // Optional; must implement IResourceWallet

        [Header("Resource")]
        [SerializeField] private string _resourceId = "resource.gold";

        [Header("Value")]
        [SerializeField] private TextMeshProUGUI _valueText;

        [Header("Label (Optional)")]
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private LocalizedString _labelEntry;

        private IResourceWallet _wallet;
        private readonly System.Globalization.CultureInfo _culture = System.Globalization.CultureInfo.InvariantCulture;
        private LocalizedString.ChangeHandler _labelHandler;

        [Header("Tooltip (Mines)")]
        [SerializeField] private MonoBehaviour _mineProviderBehaviour; // Optional; must implement IMineNodeProvider
        [SerializeField] private STController _tooltipController; // Optional; will be auto-instantiated if missing
        [SerializeField] private SimpleTooltipStyle _tooltipStyle; // Optional; default style is loaded when null
        [SerializeField, Min(0f)] private float _tooltipDelay = 2f;
        [SerializeField] private LocalizedString _minesOwnedFormat = new LocalizedString
        {
            TableReference = "UI.Common",
            TableEntryReference = "Tooltip.MinesOwnedForResource" // e.g., "Owned mines: {0}"
        };
        [SerializeField] private LocalizedString _minesOwnedAndYieldFormat = new LocalizedString
        {
            TableReference = "UI.Common",
            TableEntryReference = "Tooltip.MinesOwnedAndYield" // e.g., "Owned mines: {0} (+{1}/day)"
        };

        private IMineNodeProvider _mineProvider;

        private void Awake()
        {
            _resourceId = NormalizeId(_resourceId);
            ResolveWallet();
            HookLabel();
            ResolveMineProvider();
            EnsureTooltipDependencies();
        }

        private void OnEnable()
        {
            if (_wallet != null)
            {
                _wallet.ResourceChanged += OnResourceChanged;
                SetValue(_wallet.GetAmount(_resourceId));
            }
            else
            {
                SetValue(0);
            }

            RefreshLabel();
            EnsureTooltipDependencies();
        }

        private void OnDisable()
        {
            if (_wallet != null)
            {
                _wallet.ResourceChanged -= OnResourceChanged;
            }
        }

        private void OnDestroy()
        {
            UnhookLabel();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _resourceId = NormalizeId(_resourceId);
            // Avoid touching Localization data during OnValidate to prevent editor-time NREs in tests.
            _tooltipDelay = Mathf.Max(0f, _tooltipDelay);
        }
#endif

        private void OnResourceChanged(ResourceChange change)
        {
            if (!string.Equals(change.ResourceId, _resourceId, System.StringComparison.Ordinal))
                return;
            SetValue(change.NewAmount);
        }

        private void SetValue(int amount)
        {
            if (_valueText != null)
            {
                _valueText.text = amount.ToString(_culture);
            }
        }

        private void ResolveWallet()
        {
            if (_walletBehaviour != null && _walletBehaviour is IResourceWallet wb)
            {
                _wallet = wb;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IResourceWallet candidate)
                {
                    _wallet = candidate;
                    break;
                }
            }
        }

        private void ResolveMineProvider()
        {
            if (_mineProvider != null)
                return;

            if (_mineProviderBehaviour != null && _mineProviderBehaviour is IMineNodeProvider mp)
            {
                _mineProvider = mp;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMineNodeProvider candidate)
                {
                    _mineProvider = candidate;
                    break;
                }
            }
        }

        private void EnsureTooltipDependencies()
        {
            if (_tooltipController == null)
            {
                // Try to auto-instantiate via SimpleTooltip helper if available
                try
                {
                    var controller = SimpleTooltip.AddTooltipPrefabToScene();
                    if (controller != null)
                    {
                        _tooltipController = controller;
                    }
                }
                catch
                {
                    // Fallback: search existing controller in scene
                    _tooltipController = FindObjectOfType<STController>(true);
                }
            }

            if (_tooltipController != null)
            {
                _tooltipController.SetShowDelay(_tooltipDelay);
            }

            if (_tooltipStyle == null)
            {
                _tooltipStyle = Resources.Load<SimpleTooltipStyle>("STDefault");
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ResolveMineProvider();
            EnsureTooltipDependencies();
            if (_tooltipController == null)
                return;

            CountOwnedMinesAndYieldForResource(_resourceId, out int count, out int totalYield);
            // Prefer the detailed format when available
            _minesOwnedAndYieldFormat.Arguments = new object[] { count, totalYield };
            string body = _minesOwnedAndYieldFormat.GetLocalizedString();
            if (string.IsNullOrEmpty(body) || body == _minesOwnedAndYieldFormat.TableEntryReference)
            {
                _minesOwnedFormat.Arguments = new object[] { count };
                body = _minesOwnedFormat.GetLocalizedString();
            }
            _tooltipController.SetCustomStyledText(body, _tooltipStyle, STController.TextAlign.Left);
            _tooltipController.SetCustomStyledText(string.Empty, _tooltipStyle, STController.TextAlign.Right);
            _tooltipController.ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipController != null)
            {
                _tooltipController.HideTooltip();
            }
        }

        private int CountOwnedMinesForResource(string resourceId)
        {
            if (_mineProvider == null || string.IsNullOrEmpty(resourceId))
                return 0;

            var nodes = _mineProvider.Nodes;
            int count = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (!n.IsOwned)
                    continue;
                if (!string.Equals(n.ResourceId, resourceId, System.StringComparison.Ordinal))
                    continue;
                count++;
            }
            return count;
        }

        private void CountOwnedMinesAndYieldForResource(string resourceId, out int count, out int totalYield)
        {
            count = 0;
            totalYield = 0;
            if (_mineProvider == null || string.IsNullOrEmpty(resourceId))
                return;

            var nodes = _mineProvider.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (!n.IsOwned)
                    continue;
                if (!string.Equals(n.ResourceId, resourceId, System.StringComparison.Ordinal))
                    continue;
                count++;
                if (n.DailyYield > 0)
                    totalYield += n.DailyYield;
            }
        }

        private void HookLabel()
        {
            if (_labelText == null)
                return;

            EnsureLabelDefaults();
            _labelHandler = value =>
            {
                if (_labelText != null)
                {
                    _labelText.text = value;
                }
            };
            _labelEntry.StringChanged += _labelHandler;
        }

        private void UnhookLabel()
        {
            if (_labelHandler == null)
                return;
            _labelEntry.StringChanged -= _labelHandler;
            _labelHandler = null;
        }

        private void RefreshLabel()
        {
            if (_labelText == null)
                return;
            _labelEntry.RefreshString();
        }

#if UNITY_EDITOR
        private void EnsureLabelDefaults()
        {
            // In Edit Mode tests, OnValidate can run before Localization internals are fully ready.
            // Guard against NullReferenceException by using a safe try-catch and defaulting table.
            try
            {
                var tableName = _labelEntry.TableReference.TableCollectionName;
                if (string.IsNullOrEmpty(tableName))
                {
                    _labelEntry.TableReference = DefaultTable;
                }
            }
            catch
            {
                _labelEntry.TableReference = DefaultTable;
            }
            // Do not force a default entry name; let designers assign e.g., "Resources.GoldLabel"
        }
#else
        private void EnsureLabelDefaults() { }
#endif

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }
}
