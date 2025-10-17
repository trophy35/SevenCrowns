using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using SevenCrowns.Map;
using SevenCrowns.Map.Resources;

namespace SevenCrowns.UI.Tooltips
{
    /// <summary>
    /// Binds world tooltip hints (resources, etc.) to the Simple Tooltip controller, as text-only.
    /// Displays "{amount} {resourceName}" without any icon markup or sprite assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapTooltipBinder : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private MonoBehaviour _hintSourceBehaviour; // Optional; must implement IWorldTooltipHintSource
        [SerializeField] private bool _autoDiscover = true;
        [SerializeField, Min(0f)] private float _discoverInterval = 0.5f;

        [Header("Tooltip")]
        [SerializeField] private STController _controller;
        [SerializeField] private bool _instantiateIfMissing = true;
        [SerializeField] private SimpleTooltipStyle _style;
        [SerializeField, Min(0f)] private float _showDelay = 2f;

        [Header("Formatting")]
        [SerializeField] private bool _useThousandsSeparators = true;
        [SerializeField] private Color _amountColor = new Color32(0xF4, 0xD3, 0x5E, 0xFF); // yellow

        [Header("Localization")]
        [SerializeField] private LocalizedString _resourceFormat = new LocalizedString
        {
            TableReference = "UI.Common",
            TableEntryReference = "Tooltip.ResourceSimple" // Expected format: "{0} {1}"
        };
        [SerializeField] private LocalizedString _farmFormat = new LocalizedString
        {
            TableReference = "UI.Common",
            TableEntryReference = "Tooltip.FarmPopulation" // Expected: "{0} people/week"
        };

        private IWorldTooltipHintSource _source;
        private float _discoverTimer;
        private Coroutine _applyRoutine;
        private int _requestId;
        private AsyncOperationHandle<string>? _nameHandle;
        private AsyncOperationHandle<string>? _formatHandle;

        private void OnEnable()
        {
            EnsureTooltipDependencies();
            TryBindSource();
            if (_source != null)
            {
                HandleHintChanged(_source.CurrentTooltipHint);
            }
            else
            {
                HideTooltip();
            }
        }

        private void OnDisable()
        {
            UnbindSource();
            StopActiveRoutine();
            ReleaseHandles();
            HideTooltip();
        }

        private void Update()
        {
            if (_source == null && _autoDiscover)
            {
                _discoverTimer += Time.unscaledDeltaTime;
                if (_discoverTimer >= Mathf.Max(0.1f, _discoverInterval))
                {
                    _discoverTimer = 0f;
                    TryBindSource();
                }
            }
        }

        private void TryBindSource()
        {
            if (_source != null)
                return;

            if (_hintSourceBehaviour != null && _hintSourceBehaviour is IWorldTooltipHintSource explicitSource)
            {
                _source = explicitSource;
            }
            else if (_autoDiscover)
            {
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && _source == null; i++)
                {
                    if (behaviours[i] is IWorldTooltipHintSource candidate)
                    {
                        _source = candidate;
                    }
                }
            }

            if (_source != null)
            {
                _source.TooltipHintChanged += OnTooltipHintChanged;
                HandleHintChanged(_source.CurrentTooltipHint);
            }
        }

        private void UnbindSource()
        {
            if (_source == null)
                return;

            _source.TooltipHintChanged -= OnTooltipHintChanged;
            _source = null;
        }

        private void OnTooltipHintChanged(WorldTooltipHint hint)
        {
            HandleHintChanged(hint);
        }

        private void HandleHintChanged(WorldTooltipHint hint)
        {
            StopActiveRoutine();
            ReleaseHandles();

            if (!hint.HasTooltip)
            {
                HideTooltip();
                return;
            }

            EnsureTooltipDependencies();
            if (_controller == null)
                return;

            _requestId++;
            if (hint.Kind == WorldTooltipKind.Resource)
            {
                _applyRoutine = StartCoroutine(ApplyResourceHintRoutine(hint, _requestId));
            }
            else if (hint.Kind == WorldTooltipKind.Farm)
            {
                _applyRoutine = StartCoroutine(ApplyFarmHintRoutine(hint, _requestId));
            }
        }

        private IEnumerator ApplyResourceHintRoutine(WorldTooltipHint hint, int requestId)
        {
            var descriptor = hint.Resource.Descriptor;
            string resourceName = string.Empty;

            if (descriptor.Resource != null)
            {
                _nameHandle = descriptor.Resource.DisplayName.GetLocalizedStringAsync();
                yield return _nameHandle.Value;

                if (requestId != _requestId)
                {
                    ReleaseHandles();
                    yield break;
                }

                resourceName = _nameHandle.Value.Status == AsyncOperationStatus.Succeeded
                    ? _nameHandle.Value.Result
                    : descriptor.Resource.DisplayName.ToString();
            }

            _nameHandle = null;

            // Bold + colored amount
            string amountRaw = FormatAmount(hint.Resource.Amount);
            string amountText = string.Concat("<b><color=#", ColorUtility.ToHtmlStringRGB(_amountColor), ">", amountRaw, "</color></b>");

            _resourceFormat.Arguments = new object[] { amountText, resourceName };
            _formatHandle = _resourceFormat.GetLocalizedStringAsync();
            yield return _formatHandle.Value;

            if (requestId != _requestId)
            {
                ReleaseHandles();
                yield break;
            }

            string body = _formatHandle.Value.Status == AsyncOperationStatus.Succeeded
                ? _formatHandle.Value.Result
                : string.Concat(amountText, " ", resourceName);

            _formatHandle = null;
            _resourceFormat.Arguments = null;

            ApplyTooltipText(body);
        }

        private IEnumerator ApplyFarmHintRoutine(WorldTooltipHint hint, int requestId)
        {
            string amountRaw = FormatAmount(Mathf.Max(0, hint.Farm.WeeklyPopulation));
            string amountText = string.Concat("<b><color=#", ColorUtility.ToHtmlStringRGB(_amountColor), ">", amountRaw, "</color></b>");

            _farmFormat.Arguments = new object[] { amountText };
            _formatHandle = _farmFormat.GetLocalizedStringAsync();
            yield return _formatHandle.Value;

            if (requestId != _requestId)
            {
                ReleaseHandles();
                yield break;
            }

            string body = _formatHandle.Value.Status == AsyncOperationStatus.Succeeded
                ? _formatHandle.Value.Result
                : string.Concat(amountText, " / week");

            _formatHandle = null;
            _farmFormat.Arguments = null;

            ApplyTooltipText(body);
        }

        private void ApplyTooltipText(string body)
        {
            EnsureTooltipDependencies();
            if (_controller == null)
                return;

            _controller.SetCustomStyledText(body, _style, STController.TextAlign.Left);
            _controller.SetCustomStyledText(string.Empty, _style, STController.TextAlign.Right);
            _controller.ShowTooltip();
        }

        private void HideTooltip()
        {
            if (_controller != null)
            {
                _controller.SetCustomStyledText(string.Empty, _style, STController.TextAlign.Left);
                _controller.SetCustomStyledText(string.Empty, _style, STController.TextAlign.Right);
                _controller.HideTooltip();
            }
        }

        private void EnsureTooltipDependencies()
        {
            if (_controller == null && _instantiateIfMissing)
            {
                var instantiated = SimpleTooltip.AddTooltipPrefabToScene();
                if (instantiated != null)
                {
                    _controller = instantiated;
                }
            }

            if (_controller != null)
            {
                _controller.SetShowDelay(Mathf.Max(0f, _showDelay));
            }

            if (_style == null)
            {
                _style = Resources.Load<SimpleTooltipStyle>("STDefault");
            }
        }

        private string FormatAmount(int amount)
        {
            if (!_useThousandsSeparators)
                return amount.ToString(CultureInfo.InvariantCulture);

            CultureInfo culture = CultureInfo.CurrentCulture;
            var locale = LocalizationSettings.SelectedLocale;
            if (locale != null)
            {
                try
                {
                    culture = CultureInfo.CreateSpecificCulture(locale.Identifier.Code);
                }
                catch (CultureNotFoundException)
                {
                    culture = CultureInfo.CurrentCulture;
                }
            }

            return amount.ToString("N0", culture);
        }

        private void StopActiveRoutine()
        {
            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
                _applyRoutine = null;
            }
        }

        private void ReleaseHandles()
        {
            _nameHandle = null;
            _formatHandle = null;
        }
    }
}
