using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.EventSystems;
using SevenCrowns.Systems;
using SevenCrowns.Map.Farms;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Displays the current available population (people per week) from IPopulationService.
    /// Resets weekly via FarmProductionService; this view just reflects the current amount.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PeopleAmountHudView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Service")]
        [SerializeField] private MonoBehaviour _populationBehaviour; // Optional; must implement IPopulationService

        [Header("Value")]
        [SerializeField] private TextMeshProUGUI _valueText;

        [Header("Label (Optional)")]
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private LocalizedString _labelEntry;
        
        [Header("Tooltip (Farms)")]
        [SerializeField] private MonoBehaviour _farmProviderBehaviour; // Optional; must implement IFarmNodeProvider
        [SerializeField] private STController _tooltipController; // Optional; will be auto-instantiated if missing
        [SerializeField] private SimpleTooltipStyle _tooltipStyle; // Optional; default style is loaded when null
        [SerializeField, Min(0f)] private float _tooltipDelay = 2f;
        [SerializeField] private LocalizedString _farmsOwnedFormat = new LocalizedString
        {
            TableReference = "UI.Common",
            TableEntryReference = "Tooltip.FarmsOwned" // e.g., "Owned farms: {0}"
        };
        [SerializeField] private LocalizedString _farmsOwnedAndYieldFormat = new LocalizedString
        {
            TableReference = "UI.Common",
            TableEntryReference = "Tooltip.FarmsOwnedAndYield" // e.g., "Owned farms: {0} (+{1}/week)"
        };

        private IPopulationService _population;
        private readonly System.Globalization.CultureInfo _culture = System.Globalization.CultureInfo.InvariantCulture;
        private LocalizedString.ChangeHandler _labelHandler;
        private IFarmNodeProvider _farmProvider;

        private void Awake()
        {
            ResolvePopulation();
            HookLabel();
        }

        private void OnEnable()
        {
            if (_population == null)
            {
                ResolvePopulation();
            }
            if (_population != null)
            {
                _population.PopulationChanged += OnPopulationChanged;
                SetValue(_population.GetAvailable());
            }
            else
            {
                SetValue(0);
            }
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (_population != null)
            {
                _population.PopulationChanged -= OnPopulationChanged;
            }
        }

        private void OnDestroy()
        {
            UnhookLabel();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Avoid touching Localization data during OnValidate to prevent editor-time NREs in tests.
            _tooltipDelay = Mathf.Max(0f, _tooltipDelay);
        }
#endif

        private void OnPopulationChanged(int amount)
        {
            SetValue(amount);
        }

        private void SetValue(int amount)
        {
            if (_valueText != null)
            {
                _valueText.text = amount.ToString(_culture);
            }
        }

        private void ResolvePopulation()
        {
            if (_populationBehaviour != null && _populationBehaviour is IPopulationService ps)
            {
                _population = ps;
                return;
            }
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPopulationService candidate)
                {
                    _population = candidate;
                    break;
                }
            }
        }

        private void ResolveFarmProvider()
        {
            if (_farmProvider != null)
                return;

            if (_farmProviderBehaviour != null && _farmProviderBehaviour is IFarmNodeProvider fp)
            {
                _farmProvider = fp;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IFarmNodeProvider candidate)
                {
                    _farmProvider = candidate;
                    break;
                }
            }
        }

        private void HookLabel()
        {
            if (_labelText == null)
                return;

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

        private void EnsureTooltipDependencies()
        {
            if (_tooltipController == null)
            {
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

        public void OnPointerEnter(PointerEventData _)
        {
            ResolveFarmProvider();
            EnsureTooltipDependencies();
            if (_tooltipController == null)
                return;

            CountOwnedFarmsAndYield(out int count, out int totalYield);
            _farmsOwnedAndYieldFormat.Arguments = new object[] { count, totalYield };
            string body = _farmsOwnedAndYieldFormat.GetLocalizedString();
            if (string.IsNullOrEmpty(body) || body == _farmsOwnedAndYieldFormat.TableEntryReference)
            {
                _farmsOwnedFormat.Arguments = new object[] { count };
                body = _farmsOwnedFormat.GetLocalizedString();
            }
            _tooltipController.SetCustomStyledText(body, _tooltipStyle, STController.TextAlign.Left);
            _tooltipController.SetCustomStyledText(string.Empty, _tooltipStyle, STController.TextAlign.Right);
            _tooltipController.ShowTooltip();
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_tooltipController != null)
            {
                _tooltipController.HideTooltip();
            }
        }

        private void CountOwnedFarmsAndYield(out int count, out int totalYield)
        {
            count = 0;
            totalYield = 0;
            if (_farmProvider == null)
                return;

            var nodes = _farmProvider.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (!n.IsOwned)
                    continue;
                count++;
                if (n.WeeklyPopulationYield > 0)
                    totalYield += n.WeeklyPopulationYield;
            }
        }
    }
}
