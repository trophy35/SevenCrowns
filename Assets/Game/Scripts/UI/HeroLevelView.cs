using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Binds a localized TextMeshPro label to the current hero level.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroLevelView : MonoBehaviour
    {
        private const string DefaultTable = "UI.Common";
        private const string DefaultEntry = "Hero.LevelFormat";

        [Header("Target")]
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private LocalizedString _formatEntry;

        private ICurrentHeroPortraitKeyProvider _provider;
        private LocalizedString.ChangeHandler _handler;
        private readonly object[] _arguments = new object[1];

        private void Awake()
        {
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            EnsureDefaults();
            ResolveProvider();
            HookLocalizedString();
        }

        private void OnEnable()
        {
            if (_provider == null)
            {
                UpdateLabel(1);
                return;
            }

            _provider.CurrentHeroChanged += OnCurrentHeroChanged;
            _provider.CurrentHeroLevelChanged += OnCurrentHeroLevelChanged;
            UpdateLabel(_provider.CurrentLevel);
        }

        private void OnDisable()
        {
            if (_provider != null)
            {
                _provider.CurrentHeroChanged -= OnCurrentHeroChanged;
                _provider.CurrentHeroLevelChanged -= OnCurrentHeroLevelChanged;
            }
        }

        private void OnDestroy()
        {
            if (_handler != null)
            {
                _formatEntry.StringChanged -= _handler;
                _handler = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaults();
        }
#endif

        private void OnCurrentHeroChanged(string heroId, string portraitKey)
        {
            if (_provider != null)
            {
                UpdateLabel(_provider.CurrentLevel);
            }
        }

        private void OnCurrentHeroLevelChanged(string heroId, int level)
        {
            UpdateLabel(level);
        }

        private void UpdateLabel(int level)
        {
            level = Mathf.Max(1, level);
            _arguments[0] = level;
            _formatEntry.Arguments = _arguments;
            _formatEntry.RefreshString();
        }

        private void ResolveProvider()
        {
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICurrentHeroPortraitKeyProvider provider)
                {
                    _provider = provider;
                    break;
                }
            }
        }

        private void HookLocalizedString()
        {
            if (_handler != null) return;

            _handler = value =>
            {
                if (_label != null)
                {
                    _label.text = value;
                }
            };

            _formatEntry.StringChanged += _handler;
        }

        private void EnsureDefaults()
        {
            if (string.IsNullOrEmpty(_formatEntry.TableReference.TableCollectionName))
            {
                _formatEntry.TableReference = DefaultTable;
            }

            if (string.IsNullOrEmpty(_formatEntry.TableEntryReference))
            {
                _formatEntry.TableEntryReference = DefaultEntry;
            }
        }
    }
}

