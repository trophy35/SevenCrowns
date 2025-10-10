using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map.Resources;
using SevenCrowns.UI;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Runtime implementation of <see cref="IResourceWallet"/> storing resource amounts for the current profile/session.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class ResourceWalletService : MonoBehaviour, IResourceWallet
    {
        [Serializable]
        private struct StartingResource
        {
            public string resourceId;
            public int amount;
        }

        [Header("Starting Resources")]
        [SerializeField]
        private List<StartingResource> _startingResources = new();

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private MonoBehaviour _assetProviderBehaviour;
        [SerializeField] private string _goldResourceId = "resource.gold";
        [SerializeField] private string _goldCollectSfxKey = "Audio/SFX/World/Collect-gold";
        [Range(0f, 1f)]
        [SerializeField] private float _goldSfxVolume = 1f;
        [SerializeField, Min(0f)] private float _goldSfxWarmupTimeout = 2f;

        private readonly Dictionary<string, int> _amounts = new(StringComparer.Ordinal);

        private IUiAssetProvider _assetProvider;
        private AudioClip _goldCollectClip;
        private Coroutine _goldWarmupRoutine;

        public event Action<ResourceChange> ResourceChanged;

        private void Awake()
        {
            _goldResourceId = NormalizeResourceId(_goldResourceId);
            EnsureAudioSource();
            ResolveAssetProvider();
            InitializeFromStartingResources();
        }

        private void Start()
        {
            WarmupGoldSfx();
        }

        private void OnEnable()
        {
            ResolveAssetProvider();
            WarmupGoldSfx();
        }

        private void OnDisable()
        {
            if (_goldWarmupRoutine != null)
            {
                StopCoroutine(_goldWarmupRoutine);
                _goldWarmupRoutine = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_startingResources == null)
            {
                _startingResources = new List<StartingResource>();
            }
            else
            {
                for (int i = 0; i < _startingResources.Count; i++)
                {
                    var entry = _startingResources[i];
                    entry.resourceId = NormalizeResourceId(entry.resourceId);
                    _startingResources[i] = entry;
                }
            }

            _goldResourceId = NormalizeResourceId(_goldResourceId);
            EnsureAudioSource();
            ResolveAssetProvider();
        }
#endif

        public int GetAmount(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return 0;

            var key = NormalizeResourceId(resourceId);
            return _amounts.TryGetValue(key, out var value) ? value : 0;
        }

        public void Add(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount == 0)
                return;

            var key = NormalizeResourceId(resourceId);
            int current = GetAmount(key);
            int newAmount = current + amount;
            _amounts[key] = newAmount;

            if (amount > 0)
            {
                TryPlayCollectSfx(key, amount);
            }

            ResourceChanged?.Invoke(new ResourceChange(key, amount, newAmount));
        }

        public bool TrySpend(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0)
                return false;

            var key = NormalizeResourceId(resourceId);
            int current = GetAmount(key);
            if (current < amount)
                return false;

            int newAmount = current - amount;
            _amounts[key] = newAmount;
            ResourceChanged?.Invoke(new ResourceChange(key, -amount, newAmount));
            return true;
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f;
                return;
            }

            if (!TryGetComponent(out _audioSource))
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            _audioSource.spatialBlend = 0f;
        }

        private void ResolveAssetProvider()
        {
            if (_assetProvider != null)
                return;

            if (_assetProviderBehaviour != null && _assetProviderBehaviour is IUiAssetProvider provider)
            {
                _assetProvider = provider;
                return;
            }

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IUiAssetProvider candidate)
                {
                    _assetProvider = candidate;
                    return;
                }
            }
        }

        private void WarmupGoldSfx()
        {
            if (!isActiveAndEnabled)
                return;
            if (string.IsNullOrEmpty(_goldCollectSfxKey))
                return;
            if (_goldCollectClip != null)
                return;

            ResolveAssetProvider();
            if (_assetProvider == null)
                return;

            if (_goldWarmupRoutine != null)
            {
                StopCoroutine(_goldWarmupRoutine);
                _goldWarmupRoutine = null;
            }

            _goldWarmupRoutine = StartCoroutine(WarmupGoldSfxRoutine());
        }

        private IEnumerator WarmupGoldSfxRoutine()
        {
            var timeout = Mathf.Max(0.1f, _goldSfxWarmupTimeout);
            var elapsed = 0f;
            while (elapsed < timeout && _goldCollectClip == null)
            {
                if (_assetProvider != null && _assetProvider.TryGetAudioClip(_goldCollectSfxKey, out var clip) && clip != null)
                {
                    _goldCollectClip = clip;
                    _goldWarmupRoutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _goldWarmupRoutine = null;
        }

        private void TryPlayCollectSfx(string resourceId, int amount)
        {
            if (amount <= 0)
                return;

            if (!string.Equals(resourceId, _goldResourceId, StringComparison.Ordinal))
                return;

            if (string.IsNullOrEmpty(_goldCollectSfxKey))
                return;

            EnsureAudioSource();

            if (_goldCollectClip == null)
            {
                if (PreloadRegistry.TryGet<AudioClip>(_goldCollectSfxKey, out var preloadClip) && preloadClip != null)
                {
                    _goldCollectClip = preloadClip;
                }
                else if (_assetProvider != null && _assetProvider.TryGetAudioClip(_goldCollectSfxKey, out var providerClip) && providerClip != null)
                {
                    _goldCollectClip = providerClip;
                }
                else
                {
                    WarmupGoldSfx();
                }
            }

            if (_goldCollectClip != null)
            {
                _audioSource.PlayOneShot(_goldCollectClip, Mathf.Clamp01(_goldSfxVolume));
            }
        }

        private void InitializeFromStartingResources()
        {
            _amounts.Clear();
            if (_startingResources == null)
                return;

            for (int i = 0; i < _startingResources.Count; i++)
            {
                var entry = _startingResources[i];
                if (string.IsNullOrWhiteSpace(entry.resourceId) || entry.amount == 0)
                    continue;

                var key = NormalizeResourceId(entry.resourceId);
                if (_amounts.TryGetValue(key, out var existing))
                {
                    _amounts[key] = existing + entry.amount;
                }
                else
                {
                    _amounts.Add(key, entry.amount);
                }
            }
        }

        private static string NormalizeResourceId(string resourceId)
        {
            return string.IsNullOrWhiteSpace(resourceId)
                ? string.Empty
                : resourceId.Trim();
        }
    }
}
