using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// Displays the current city's faction icon by resolving a Sprite via IUiAssetProvider.
    /// Builds the key as string.Format(spriteKeyFormat, factionId) by default.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityFactionIconView : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _fallback;

        [Header("Assets")]
        [SerializeField, Tooltip("Optional explicit provider. When null, auto-discovers a PreloadRegistryAssetProvider.")]
        private MonoBehaviour _assetProviderBehaviour; // IUiAssetProvider
        [SerializeField, Tooltip("Key format used to resolve the faction Sprite. Use {0} for faction id.")]
        private string _spriteKeyFormat = "UI/Factions/{0}";
        [SerializeField, Tooltip("When true, trims and replaces spaces in the faction id with dots.")]
        private bool _normalizeFactionId = true;
        [Header("Local Fallback Mapping (Editor-Friendly)")]
        [SerializeField, Tooltip("Optional local mapping used when no IUiAssetProvider is available (e.g., when debugging scenes directly). Keys are compared after normalization.")]
        private FactionSprite[] _fallbackSprites = System.Array.Empty<FactionSprite>();
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private IUiAssetProvider _assets;
        private ICityFactionIdProvider _provider;
        [SerializeField, Min(0f)] private float _lateBindTimeout = 2f;
        [SerializeField, Min(0f), Tooltip("Seconds to wait for services (IUiAssetProvider/ICityFactionIdProvider) to appear.")]
        private float _waitForProviderTimeout = 2f;
        private Coroutine _lateBindRoutine;
        private Coroutine _waitProviderRoutine;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            ResolveServices();
        }

        private void OnEnable()
        {
            // In Edit Mode tests, avoid coroutines and bind immediately to prevent editor assertions.
            bool immediate = !Application.isPlaying;
            TryBind(immediate: immediate);
            if (!immediate)
            {
                if ((_assets == null || _provider == null) && _waitProviderRoutine == null && _waitForProviderTimeout > 0f)
                {
                    _waitProviderRoutine = StartCoroutine(WaitForServicesThenBind());
                }
            }
        }

        private void OnDisable()
        {
            if (_lateBindRoutine != null)
            {
                StopCoroutine(_lateBindRoutine);
                _lateBindRoutine = null;
            }
            if (_waitProviderRoutine != null)
            {
                StopCoroutine(_waitProviderRoutine);
                _waitProviderRoutine = null;
            }
        }

        private void ResolveServices()
        {
            if (_assetProviderBehaviour != null && _assetProviderBehaviour is IUiAssetProvider a)
                _assets = a;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            int total = behaviours != null ? behaviours.Length : 0;
            int candidates = 0;
            for (int i = 0; i < total && (_assets == null || _provider == null); i++)
            {
                var mb = behaviours[i];
                if (mb is IUiAssetProvider ap)
                {
                    candidates++;
                    if (_assets == null) _assets = ap;
                }
                if (_provider == null && mb is ICityFactionIdProvider p)
                {
                    _provider = p;
                }
            }

            // Note: Do not reference concrete providers from UI (assembly boundary). Interface discovery only.

            if (_debugLogs)
            {
                Debug.Log($"[CityFactionIconView] ResolveServices: totalMB={total} iUiAssetProvidersFound={candidates} AssetsBound={(_assets!=null)} ProviderBound={(_provider!=null)}", this);
            }
        }

        private void TryBind(bool immediate)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_assets == null || _provider == null)
            {
                ResolveServices();
                if (_assets == null || _provider == null)
                {
                    // Try to use local mapping if we at least have a faction id
                    string idForMap = null;
                    if (_provider != null && _provider.TryGetFactionId(out var fid) && !string.IsNullOrEmpty(fid))
                    {
                        idForMap = _normalizeFactionId ? NormalizeId(fid) : fid;
                    }
                    if (!string.IsNullOrEmpty(idForMap) && TryResolveFromLocalMapping(idForMap, out var mapped) && mapped != null)
                    {
                        _image.sprite = mapped;
                        if (_debugLogs) Debug.Log($"[CityFactionIconView] No IUiAssetProvider; used local mapping for faction='{idForMap}'.", this);
                        return;
                    }

                    if (_fallback != null) _image.sprite = _fallback;
                    if (_debugLogs) Debug.LogWarning("[CityFactionIconView] Missing provider or assets; applied generic fallback.", this);
                    if (_waitProviderRoutine == null && _waitForProviderTimeout > 0f)
                    {
                        _waitProviderRoutine = StartCoroutine(WaitForServicesThenBind());
                    }
                    return;
                }
            }

            if (!_provider.TryGetFactionId(out var factionId) || string.IsNullOrEmpty(factionId))
            {
                if (_fallback != null) _image.sprite = _fallback;
                if (_debugLogs) Debug.LogWarning("[CityFactionIconView] No faction id from provider; applied fallback.", this);
                return;
            }

            var id = _normalizeFactionId ? NormalizeId(factionId) : factionId;
            var key = string.Format(string.IsNullOrEmpty(_spriteKeyFormat) ? "{0}" : _spriteKeyFormat, id);
            if (_debugLogs) Debug.Log($"[CityFactionIconView] Resolving sprite key='{key}' (factionId='{factionId}', normalized='{id}')", this);

            // In Edit Mode (not playing), prefer local mapping first to avoid cross-test leakage of providers.
            if (!Application.isPlaying)
            {
                if (TryResolveFromLocalMapping(id, out var localSpriteEdit) && localSpriteEdit != null)
                {
                    _image.sprite = localSpriteEdit;
                    if (_debugLogs) Debug.Log($"[CityFactionIconView] (EditMode) Using local fallback mapping for factionId='{id}'.", this);
                    return;
                }
                if (_assets != null && _assets.TryGetSprite(key, out var spriteEdit) && spriteEdit != null)
                {
                    _image.sprite = spriteEdit;
                    return;
                }
            }
            else
            {
                if (_assets != null && _assets.TryGetSprite(key, out var sprite) && sprite != null)
                {
                    _image.sprite = sprite;
                    return;
                }
                // Try local mapping as a no-provider/editor-friendly fallback
                if (TryResolveFromLocalMapping(id, out var localSprite) && localSprite != null)
                {
                    _image.sprite = localSprite;
                    if (_debugLogs) Debug.Log($"[CityFactionIconView] Using local fallback mapping for factionId='{id}'.", this);
                    return;
                }
            }

            if (_fallback != null) _image.sprite = _fallback;
            if (_debugLogs) Debug.LogWarning($"[CityFactionIconView] Sprite not found for key='{key}'. Applied generic fallback. Will late-bind if possible.", this);

            if (!immediate)
            {
                if (_lateBindRoutine != null) StopCoroutine(_lateBindRoutine);
                _lateBindRoutine = StartCoroutine(LateBind(key));
            }
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            id = id.Trim();
            return id.Replace(' ', '.');
        }

        private System.Collections.IEnumerator WaitForServicesThenBind()
        {
            if (_debugLogs) Debug.Log($"[CityFactionIconView] Waiting for services up to {_waitForProviderTimeout:0.00}s", this);
            float t = 0f;
            while (t < _waitForProviderTimeout && (_assets == null || _provider == null))
            {
                ResolveServices();
                if (_assets != null && _provider != null)
                {
                    if (_debugLogs) Debug.Log("[CityFactionIconView] Services available. Rebinding.", this);
                    TryBind(immediate: false);
                    _waitProviderRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_debugLogs) Debug.LogWarning("[CityFactionIconView] Services not found within wait timeout.", this);
            _waitProviderRoutine = null;
        }

        [System.Serializable]
        private struct FactionSprite
        {
            public string factionId;
            public Sprite sprite;
        }

        private bool TryResolveFromLocalMapping(string normalizedId, out Sprite sprite)
        {
            sprite = null;
            if (_fallbackSprites == null || _fallbackSprites.Length == 0)
                return false;

            var target = string.IsNullOrEmpty(normalizedId) ? string.Empty : normalizedId;
            for (int i = 0; i < _fallbackSprites.Length; i++)
            {
                var entry = _fallbackSprites[i];
                var key = _normalizeFactionId ? NormalizeId(entry.factionId) : (entry.factionId ?? string.Empty);
                if (string.Equals(key, target, System.StringComparison.Ordinal))
                {
                    sprite = entry.sprite;
                    return sprite != null;
                }
            }
            return false;
        }

        private System.Collections.IEnumerator LateBind(string key)
        {
            if (_debugLogs) Debug.Log($"[CityFactionIconView] LateBind start for key='{key}', timeout={_lateBindTimeout:0.00}s", this);
            float t = 0f;
            while (t < _lateBindTimeout)
            {
                if (_assets != null && _assets.TryGetSprite(key, out var sprite) && sprite != null)
                {
                    _image.sprite = sprite;
                    if (_debugLogs) Debug.Log($"[CityFactionIconView] LateBind success for key='{key}' at t={t:0.00}s", this);
                    _lateBindRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_debugLogs) Debug.LogWarning($"[CityFactionIconView] LateBind timeout for key='{key}' after {t:0.00}s", this);
            _lateBindRoutine = null;
        }
    }
}
