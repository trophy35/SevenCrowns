using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Binds a uGUI Image to the current hero's portrait Sprite (Addressables-backed).
    /// Finds ICurrentHeroPortraitKeyProvider and IUiAssetProvider in the scene.
    /// Uses late-binding retries to avoid first-use hitch if the Sprite is still loading.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroPortraitView : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image _image;
        [Tooltip("Fallback sprite if current portrait Sprite is not ready.")]
        [SerializeField] private Sprite _fallback;
        [SerializeField, Min(0f)] private float _lateBindTimeout = 2.0f;

        private IUiAssetProvider _assets;
        private ICurrentHeroPortraitKeyProvider _provider;
        private string _lastKey;
        private Coroutine _lateBindRoutine;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();

            // Discover services in scene (keeps UI decoupled from Core types)
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (_assets == null && behaviours[i] is IUiAssetProvider a) _assets = a;
                if (_provider == null && behaviours[i] is ICurrentHeroPortraitKeyProvider p) _provider = p;
                if (_assets != null && _provider != null) break;
            }
        }

        private void OnEnable()
        {
            if (_provider != null)
            {
                _provider.CurrentHeroChanged += OnCurrentHeroChanged;
            }
            // Initial bind
            TryBind(_provider != null ? _provider.CurrentPortraitKey : null, immediate: true);
        }

        private void OnDisable()
        {
            if (_provider != null)
            {
                _provider.CurrentHeroChanged -= OnCurrentHeroChanged;
            }
            if (_lateBindRoutine != null)
            {
                StopCoroutine(_lateBindRoutine);
                _lateBindRoutine = null;
            }
        }

        private void OnCurrentHeroChanged(string heroId, string portraitKey)
        {
            TryBind(portraitKey, immediate: false);
        }

        private void TryBind(string key, bool immediate)
        {
            _lastKey = key;
            if (_assets != null && !string.IsNullOrEmpty(key) && _assets.TryGetSprite(key, out var sprite) && sprite != null)
            {
                Apply(sprite);
                return;
            }

            // Fallback immediately for responsiveness
            if (_fallback != null) Apply(_fallback);

            // Late-bind if we expect Addressables to finish shortly (preload or auto-load)
            if (!string.IsNullOrEmpty(key) && _assets != null && !immediate)
            {
                if (_lateBindRoutine != null) StopCoroutine(_lateBindRoutine);
                _lateBindRoutine = StartCoroutine(LateBind(key));
            }
        }

        private System.Collections.IEnumerator LateBind(string key)
        {
            float t = 0f;
            while (t < _lateBindTimeout)
            {
                if (_assets.TryGetSprite(key, out var sprite) && sprite != null)
                {
                    Apply(sprite);
                    _lateBindRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            _lateBindRoutine = null;
        }

        private void Apply(Sprite s)
        {
            if (_image != null) _image.sprite = s;
        }
    }
}

