using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace SevenCrowns.Map.Resources
{
    /// <summary>
    /// Defines gameplay and visual data for a strategic resource (e.g., gold, wood).
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceDefinition", menuName = "SevenCrowns/Resources/Resource Definition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        private const string DefaultTable = "Gameplay.Resources";

        [Header("Identity")]
        [SerializeField] private string _resourceId = "resource.gold";

        [Header("Localization")]
        [SerializeField] private LocalizedString _displayName;
        [SerializeField] private LocalizedString _description;

        [Header("Map Visuals")]
        [SerializeField] private List<ResourceVisualVariantConfig> _mapVariants = new();

        private readonly Dictionary<string, ResourceVisualVariantConfig> _variantById = new(StringComparer.Ordinal);
        private readonly List<ResourceVisualVariantConfig> _randomCandidates = new();

        public string ResourceId => _resourceId;
        public LocalizedString DisplayName => _displayName;
        public LocalizedString Description => _description;
        public IReadOnlyList<ResourceVisualVariantConfig> MapVariants => _mapVariants;

        private void OnEnable()
        {
            EnsureDefaults();
            RebuildCaches();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaults();
            RebuildCaches();
        }
#endif

        /// <summary>Attempts to resolve a specific visual variant by identifier.</summary>
        public bool TryGetVariant(string variantId, out ResourceVisualVariant variant)
        {
            if (string.IsNullOrWhiteSpace(variantId))
            {
                variant = default;
                return false;
            }

            if (_variantById.TryGetValue(variantId, out var config))
            {
                variant = config.ToRuntime();
                return variant.IsValid;
            }

            variant = default;
            return false;
        }

        /// <summary>Returns a random visual variant (filtered by IncludeInRandomSelection when available).</summary>
        public bool TryGetRandomVariant(out ResourceVisualVariant variant, System.Random random = null)
        {
            List<ResourceVisualVariantConfig> source = _randomCandidates;

            if (source.Count == 0)
            {
                if (_mapVariants == null || _mapVariants.Count == 0)
                {
                    variant = default;
                    return false;
                }

                source = _mapVariants;
            }

            int index = random != null
                ? random.Next(source.Count)
                : UnityEngine.Random.Range(0, source.Count);

            var config = source[index];
            if (config == null)
            {
                variant = default;
                return false;
            }

            variant = config.ToRuntime();
            return variant.IsValid;
        }

        private void EnsureDefaults()
        {
            _resourceId = string.IsNullOrWhiteSpace(_resourceId)
                ? name?.Replace(' ', '.').ToLowerInvariant() ?? "resource.unnamed"
                : _resourceId.Trim();

            if (string.IsNullOrEmpty(_displayName.TableReference.TableCollectionName))
            {
                _displayName.TableReference = DefaultTable;
            }

            if (string.IsNullOrEmpty(_displayName.TableEntryReference))
            {
                _displayName.TableEntryReference = _resourceId + ".Name";
            }

            if (string.IsNullOrEmpty(_description.TableReference.TableCollectionName))
            {
                _description.TableReference = DefaultTable;
            }

            if (string.IsNullOrEmpty(_description.TableEntryReference))
            {
                _description.TableEntryReference = _resourceId + ".Description";
            }
        }

        private void RebuildCaches()
        {
            _variantById.Clear();
            _randomCandidates.Clear();

            if (_mapVariants == null)
            {
                return;
            }

            for (int i = 0; i < _mapVariants.Count; i++)
            {
                var config = _mapVariants[i];
                if (config == null || config.Sprite == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(config.VariantId))
                {
                    Debug.LogWarning($"[ResourceDefinition] Entry at index {i} on '{name}' has no VariantId and will be ignored.", this);
                    continue;
                }

                _variantById[config.VariantId] = config;
                if (config.IncludeInRandomSelection)
                {
                    _randomCandidates.Add(config);
                }
            }
        }

        [Serializable]
        public sealed class ResourceVisualVariantConfig
        {
            [SerializeField] private string _variantId = "default";
            [SerializeField] private Sprite _sprite;
            [SerializeField] private Vector3 _localOffset;
            [SerializeField] private bool _includeInRandomSelection = true;

            public string VariantId => string.IsNullOrWhiteSpace(_variantId) ? string.Empty : _variantId.Trim();
            public Sprite Sprite => _sprite;
            public Vector3 LocalOffset => _localOffset;
            public bool IncludeInRandomSelection => _includeInRandomSelection;

            public ResourceVisualVariant ToRuntime()
            {
                return new ResourceVisualVariant(VariantId, _sprite, _localOffset, _includeInRandomSelection);
            }
        }
    }
}
