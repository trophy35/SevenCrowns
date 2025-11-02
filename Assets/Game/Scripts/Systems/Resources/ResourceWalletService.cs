using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map.Resources;
using SevenCrowns.Map;
using SevenCrowns.UI;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Runtime implementation of <see cref="IResourceWallet"/> storing resource amounts for the current profile/session.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class ResourceWalletService : MonoBehaviour, IResourceWallet, SevenCrowns.Systems.Save.IResourceWalletSnapshotProvider
    {
        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;
        [Serializable]
        private sealed class CollectSfxConfig
        {
            [Tooltip("Resource id (e.g., 'resource.wood').")]
            public string resourceId;
            [Tooltip("Addressables key of the AudioClip to play when collecting this resource.")]
            public string clipKey;
            [Range(0f, 1f)] public float volume = 1f;
            [Min(0f)] public float warmupTimeout = 2f;
        }

        private sealed class SfxState
        {
            public string Key;
            public float Volume;
            public float WarmupTimeout;
            public AudioClip Clip;
            public Coroutine Warmup;
        }
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

        [Header("Audio Per Resource (Optional)")]
        [SerializeField]
        private List<CollectSfxConfig> _collectSfx = new();

        private readonly Dictionary<string, int> _amounts = new(StringComparer.Ordinal);

        private IUiAssetProvider _assetProvider;
        private AudioClip _goldCollectClip; // kept for backward compatibility
        private Coroutine _goldWarmupRoutine; // kept for backward compatibility
        private readonly Dictionary<string, SfxState> _sfxByResourceId = new(StringComparer.Ordinal);

        public event Action<ResourceChange> ResourceChanged;

        private void Awake()
        {
            _goldResourceId = NormalizeResourceId(_goldResourceId);
            EnsureAudioSource();
            ResolveAssetProvider();
            RebuildSfxStates();
            InitializeFromStartingResources();
            if (_debugLogs)
            {
                Debug.Log($"[Wallet] Awake initialized. Entries={_amounts.Count}", this);
                if (_amounts.Count > 0)
                {
                    foreach (var kv in _amounts)
                        Debug.Log($"[Wallet] Init amount {kv.Key}={kv.Value}", this);
                }
            }
        }

        private void Start()
        {
            WarmupAllSfx();
        }

        private void OnEnable()
        {
            ResolveAssetProvider();
            WarmupAllSfx();
        }

        private void OnDisable()
        {
            if (_goldWarmupRoutine != null) { StopCoroutine(_goldWarmupRoutine); _goldWarmupRoutine = null; }
            // Stop all active warmups
            foreach (var kvp in _sfxByResourceId)
            {
                var state = kvp.Value;
                if (state != null && state.Warmup != null)
                {
                    StopCoroutine(state.Warmup);
                    state.Warmup = null;
                }
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
            RebuildSfxStates();
        }
#endif

        public int GetAmount(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return 0;

            var key = NormalizeResourceId(resourceId);
            int value = _amounts.TryGetValue(key, out var v) ? v : 0;
            if (_debugLogs)
            {
                Debug.Log($"[Wallet] GetAmount('{key}') -> {value}", this);
            }
            return value;
        }

        public void Add(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount == 0)
                return;

            var key = NormalizeResourceId(resourceId);
            int current = GetAmount(key);
            int newAmount = current + amount;
            _amounts[key] = newAmount;

            if (_debugLogs)
            {
                Debug.Log($"[Wallet] Add key='{key}' amount={amount} current={current} new={newAmount}", this);
            }

            if (amount > 0)
            {
                TryPlayCollectSfxGeneric(key);
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

        // IResourceWalletSnapshotProvider
        System.Collections.Generic.IReadOnlyDictionary<string, int> SevenCrowns.Systems.Save.IResourceWalletSnapshotProvider.GetAllAmountsSnapshot()
        {
            return new System.Collections.Generic.Dictionary<string, int>(_amounts, System.StringComparer.Ordinal);
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
            if (WorldMapRestoreScope.IsRestoring)
                return;
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

        private void RebuildSfxStates()
        {
            _sfxByResourceId.Clear();
            if (_collectSfx != null)
            {
                for (int i = 0; i < _collectSfx.Count; i++)
                {
                    var cfg = _collectSfx[i];
                    if (cfg == null) continue;
                    var rid = NormalizeResourceId(cfg.resourceId);
                    var key = string.IsNullOrWhiteSpace(cfg.clipKey) ? string.Empty : cfg.clipKey.Trim();
                    if (string.IsNullOrEmpty(rid) || string.IsNullOrEmpty(key)) continue;

                    _sfxByResourceId[rid] = new SfxState
                    {
                        Key = key,
                        Volume = Mathf.Clamp01(cfg.volume),
                        WarmupTimeout = Mathf.Max(0.1f, cfg.warmupTimeout),
                        Clip = null,
                        Warmup = null
                    };
                }
            }

            // Backward compatibility: synthesize a state for gold if configured and not already present
            if (!string.IsNullOrEmpty(_goldCollectSfxKey))
            {
                var rid = NormalizeResourceId(_goldResourceId);
                if (!string.IsNullOrEmpty(rid) && !_sfxByResourceId.ContainsKey(rid))
                {
                    _sfxByResourceId[rid] = new SfxState
                    {
                        Key = _goldCollectSfxKey,
                        Volume = Mathf.Clamp01(_goldSfxVolume),
                        WarmupTimeout = Mathf.Max(0.1f, _goldSfxWarmupTimeout),
                        Clip = _goldCollectClip,
                        Warmup = _goldWarmupRoutine
                    };
                }
            }
        }

        private void WarmupAllSfx()
        {
            // Warmup old gold path
            WarmupGoldSfx();
            // Warmup any configured resources
            foreach (var kvp in _sfxByResourceId)
            {
                var rid = kvp.Key;
                var state = kvp.Value;
                if (state == null || !isActiveAndEnabled)
                    continue;
                if (string.IsNullOrEmpty(state.Key))
                    continue;
                if (state.Clip != null || state.Warmup != null)
                    continue;
                state.Warmup = StartCoroutine(WarmupSfxRoutine(rid, state));
            }
        }

        private System.Collections.IEnumerator WarmupSfxRoutine(string resourceId, SfxState state)
        {
            ResolveAssetProvider();
            if (_assetProvider == null)
            {
                state.Warmup = null;
                yield break;
            }

            float elapsed = 0f;
            float timeout = Mathf.Max(0.1f, state.WarmupTimeout);
            while (elapsed < timeout && state.Clip == null)
            {
                if (PreloadRegistry.TryGet<AudioClip>(state.Key, out var preloadClip) && preloadClip != null)
                {
                    state.Clip = preloadClip;
                    state.Warmup = null;
                    yield break;
                }
                if (_assetProvider.TryGetAudioClip(state.Key, out var providerClip) && providerClip != null)
                {
                    state.Clip = providerClip;
                    state.Warmup = null;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            state.Warmup = null;
        }

        private void TryPlayCollectSfxGeneric(string resourceId)
        {
            if (WorldMapRestoreScope.IsRestoring)
                return;
            // Prefer configured per-resource mapping
            if (_sfxByResourceId.TryGetValue(resourceId, out var state) && state != null)
            {
                EnsureAudioSource();
                if (state.Clip == null)
                {
                    if (PreloadRegistry.TryGet<AudioClip>(state.Key, out var preloadClip) && preloadClip != null)
                    {
                        state.Clip = preloadClip;
                    }
                    else if (_assetProvider != null && _assetProvider.TryGetAudioClip(state.Key, out var providerClip) && providerClip != null)
                    {
                        state.Clip = providerClip;
                    }
                    else if (state.Warmup == null && isActiveAndEnabled)
                    {
                        state.Warmup = StartCoroutine(WarmupSfxRoutine(resourceId, state));
                    }
                }

                if (state.Clip != null)
                {
                    _audioSource.PlayOneShot(state.Clip, Mathf.Clamp01(state.Volume));
                }
                return;
            }

            // Fallback to legacy gold behavior
            TryPlayCollectSfx(resourceId, 1);
        }

        private void InitializeFromStartingResources()
        {
            // Do not clear existing runtime values. In City scene, transfer may have already
            // populated the wallet before our Awake runs (execution order differences).
            // Only seed/accumulate configured starting entries.
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
            if (_debugLogs)
            {
                Debug.Log($"[Wallet] InitializeFromStartingResources completed. Entries={_amounts.Count}", this);
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
