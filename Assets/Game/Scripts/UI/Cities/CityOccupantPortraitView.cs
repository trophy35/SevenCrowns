using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI.Cities
{
    /// <summary>
    /// Displays the avatar (portrait Sprite) of the hero occupying the current city.
    /// - Hides the Image when there is no occupant.
    /// - Resolves the Sprite via IUiAssetProvider using the provided portrait Addressables key.
    /// Requires a Systems-side provider implementing ICityOccupantHeroProvider (e.g., CityHudInitializer).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityOccupantPortraitView : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image _image; // Disabled by default in scene
        [SerializeField, Tooltip("Optional fallback Sprite used when the portrait key cannot be resolved.")]
        private Sprite _fallback;

        [Header("Assets")]
        [SerializeField, Tooltip("Optional explicit provider. When null, auto-discovers a PreloadRegistryAssetProvider.")]
        private MonoBehaviour _assetProviderBehaviour; // IUiAssetProvider
        [Header("Debug")]
        [SerializeField] private bool _debugLogs;

        private ICityOccupantHeroProvider _provider;
        private IUiAssetProvider _assets;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            ResolveServices();
        }

        private void OnEnable()
        {
            TryBind(immediate: !Application.isPlaying);
        }

        private void ResolveServices()
        {
            if (_assetProviderBehaviour != null && _assetProviderBehaviour is IUiAssetProvider a)
                _assets = a;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && (_assets == null || _provider == null); i++)
            {
                var mb = behaviours[i];
                if (_assets == null && mb is IUiAssetProvider ap) _assets = ap;
                if (_provider == null && mb is ICityOccupantHeroProvider p) _provider = p;
            }

            if (_debugLogs)
            {
                Debug.Log($"[CityOccupantPortraitView] ResolveServices: assetsBound={_assets!=null} providerBound={_provider!=null}", this);
            }
        }

        private void TryBind(bool immediate)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_provider == null || _assets == null)
            {
                ResolveServices();
                if (_provider == null)
                {
                    // No provider => hide
                    if (_image != null) _image.enabled = false;
                    if (_debugLogs) Debug.LogWarning("[CityOccupantPortraitView] No ICityOccupantHeroProvider found. Hiding image.", this);
                    return;
                }
            }

            if (!_provider.TryGetOccupantHero(out var heroId, out var key) || string.IsNullOrEmpty(heroId))
            {
                if (_image != null) _image.enabled = false;
                if (_debugLogs) Debug.Log("[CityOccupantPortraitView] No occupant hero provided. Hiding image.", this);
                return;
            }

            if (_assets != null && !string.IsNullOrEmpty(key) && _assets.TryGetSprite(key, out var sprite) && sprite != null)
            {
                _image.sprite = sprite;
                _image.enabled = true;
                if (!_image.gameObject.activeSelf) _image.gameObject.SetActive(true);
                if (_debugLogs)
                {
                    Debug.Log($"[CityOccupantPortraitView] Bound occupant portrait. heroId='{heroId}' key='{key}'.", this);
                    var rt = _image.rectTransform;
                    var rect = rt != null ? rt.rect : new Rect(0,0,0,0);
                    var col = _image.color;
                    bool activeSelf = _image.gameObject.activeSelf;
                    bool activeHierarchy = _image.gameObject.activeInHierarchy;
                    CanvasGroup cg = null;
                    var groups = _image.GetComponentsInParent<CanvasGroup>(true);
                    if (groups != null && groups.Length > 0) cg = groups[0];
                    var canvas = _image.GetComponentInParent<Canvas>(true);
                    Debug.Log($"[CityOccupantPortraitView] Diagnostics: Image.enabled={_image.enabled} activeSelf={activeSelf} activeInHierarchy={activeHierarchy} colorA={col.a:F2} rect=({rect.width}x{rect.height})", this);
                    if (cg != null)
                    {
                        Debug.Log($"[CityOccupantPortraitView] Parent CanvasGroup: alpha={cg.alpha:F2} interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts}", this);
                    }
                    if (canvas != null)
                    {
                        Debug.Log($"[CityOccupantPortraitView] Canvas: renderMode={canvas.renderMode} sortingOrder={canvas.sortingOrder} targetDisplay={canvas.targetDisplay}", this);
                    }
                }
                return;
            }

            // Fallback path: if no assets or key not found, use fallback sprite if provided; otherwise hide
            if (_fallback != null)
            {
                _image.sprite = _fallback;
                _image.enabled = true;
                if (!_image.gameObject.activeSelf) _image.gameObject.SetActive(true);
                if (_debugLogs) Debug.Log($"[CityOccupantPortraitView] Portrait key not resolved (key='{key}'). Applied fallback sprite.", this);
            }
            else
            {
                _image.enabled = false;
                if (_image.gameObject.activeSelf) _image.gameObject.SetActive(false);
                if (_debugLogs)
                {
                    if (_assets == null) Debug.LogWarning("[CityOccupantPortraitView] No IUiAssetProvider available and no fallback sprite. Hiding image.", this);
                    else Debug.LogWarning($"[CityOccupantPortraitView] Failed to resolve portrait for heroId='{heroId}' key='{key}'. No fallback assigned. Hiding image.", this);
                }
            }
        }
    }
}
