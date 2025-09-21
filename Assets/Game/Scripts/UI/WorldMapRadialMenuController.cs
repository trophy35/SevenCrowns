using System;
using UnityEngine;
using UnityEngine.Events;

// External UI component from Tank & Healer Studio (third-party plugin)
// We reference it via the assembly definition: TankAndHealer.UltimateRadialMenu
// Note: Keep this script minimal and focused on wiring, localization, and SFX.

#if UNITY_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif

namespace SevenCrowns.UI
{
    /// <summary>
    /// Populates the UltimateRadialMenu in the World Map with an "End Turn" button.
    /// - Uses Unity Localization for name/description (no hardcoded UI strings).
    /// - Uses an injected asset provider for icon and SFX (with optional fallbacks).
    /// - Raises a UnityEvent to delegate actual end-turn logic to higher-level systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapRadialMenuController : MonoBehaviour
    {
        [Header("Radial Menu")]
        [SerializeField] private UltimateRadialMenu _menu;
        [Header("Localization")]
        [Tooltip("String table name holding UI common strings (e.g., 'UI.Common').")]
        [SerializeField] private string _uiStringTable = "UI.Common";
        [Tooltip("Key for the End Turn button label in the string table.")]
        [SerializeField] private string _endTurnNameKey = "EndTurn";
        [Tooltip("Key for the End Turn button description in the string table.")]
        [SerializeField] private string _endTurnDescKey = "EndTurnDesc";

        [Header("Assets (Addressables Keys)")]
        [Tooltip("Addressables key for the End Turn icon sprite (preload recommended).")]
        [SerializeField] private string _endTurnSpriteKey = "UI/Icons/EndTurn";
        [Tooltip("Addressables key for the End Turn SFX (preload recommended).")]
        [SerializeField] private string _endTurnSfxKey = "SFX/UI/EndTurn";

        [Header("Fallback Assets (Optional)")]
        [Tooltip("Fallback icon sprite if Addressables not preloaded.")]
        [SerializeField] private Sprite _endTurnSpriteFallback;
        [Tooltip("Fallback SFX if Addressables not preloaded.")]
        [SerializeField] private AudioClip _endTurnSfxFallback;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

        [Header("Events")]
        [Tooltip("Invoked when the End Turn button is pressed.")]
        [SerializeField] private UnityEvent _onEndTurnRequested;

        private UltimateRadialButtonInfo _endTurnInfo;
        private bool _registered;
        private AudioSource _audio;
        private IUiAssetProvider _assets;
        private AudioClip _cachedEndTurnClip;
        [SerializeField, Min(0f)] private float _sfxWarmupTimeout = 2.0f;

#if UNITY_LOCALIZATION
        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale _)
        {
            RefreshLocalization();
        }
#endif

        private void Awake()
        {
            if (_menu == null)
                _menu = GetComponent<UltimateRadialMenu>();

            // Ensure the radial menu is configured to use per-button icons before any button is created.
            if (_menu != null)
            {
                _menu.useButtonIcon = true;
                // Keep global icon size reasonable; per-button override is applied after registration.
                _menu.iconSize = Mathf.Max(0.01f, 0.15f);
                _menu.allowMultipleSelected = false;
                _menu.selectButtonOnInteract = false; // keep hover feedback after a click
                _menu.minRange = 0.0f;
            }

            _audio = GetComponent<AudioSource>();
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
            }

            // Configure radial menu input behavior to trigger on press and avoid toggle/hold gating.
            var inputMgr = FindObjectOfType<UltimateRadialMenuInputManager>(true);
            if (inputMgr != null)
            {
                inputMgr.invokeAction = UltimateRadialMenuInputManager.InvokeAction.OnButtonDown;
                inputMgr.enableMenuSetting = UltimateRadialMenuInputManager.EnableMenuSetting.Manual;
                inputMgr.disableOnInteract = false;
                inputMgr.onMenuRelease = false;
            }

            // Resolve a UI asset provider from the scene (implemented in Game.Core)
            _assets = null;
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IUiAssetProvider p)
                {
                    _assets = p;
                    break;
                }
            }
        }

        private void Start()
        {
            EnsureEndTurnButton();
            _menu.Enable();
            // Proactively warm up SFX so first click can play immediately even if Addressables were not preloaded.
            StartCoroutine(WarmupEndTurnSfx());
        }

        /// <summary>
        /// Creates and registers the End Turn radial button if not already registered.
        /// </summary>
        public void EnsureEndTurnButton()
        {
            if (_registered || _menu == null)
                return;

            var name = GetLocalized(_uiStringTable, _endTurnNameKey, string.Empty);
            var desc = GetLocalized(_uiStringTable, _endTurnDescKey, string.Empty);

            var icon = ResolveSprite();
            _endTurnInfo = new UltimateRadialButtonInfo
            {
                name = name,
                description = desc,
                icon = icon,
                key = "end_turn",
                id = 0
            };
            _menu.RegisterButton(OnEndTurnPressed, _endTurnInfo, -1);
            _registered = true;

            // If the icon wasn't available yet (e.g., Addressable not ready), try binding it shortly after.
            if (icon == null && !string.IsNullOrEmpty(_endTurnSpriteKey))
            {
                StartCoroutine(LateBindIcon());
            }
        }

        /// <summary>
        /// Re-applies localized strings to the End Turn button.
        /// </summary>
        public void RefreshLocalization()
        {
            if (!_registered || _endTurnInfo == null)
                return;

            var name = GetLocalized(_uiStringTable, _endTurnNameKey, string.Empty);
            var desc = GetLocalized(_uiStringTable, _endTurnDescKey, string.Empty);
            _endTurnInfo.UpdateName(name);
            _endTurnInfo.UpdateDescription(desc);
        }

        private void OnEndTurnPressed()
        {
            Debug.Log("OnEndTurnPressed");
            TryPlaySfx();
            _onEndTurnRequested?.Invoke();
        }

        private void TryPlaySfx()
        {
            float vol = Mathf.Clamp01(_sfxVolume);
            if (vol <= 0f || _audio == null)
                return;

            // Prefer cached clip if warm-up succeeded.
            if (_cachedEndTurnClip != null)
            {
                _audio.PlayOneShot(_cachedEndTurnClip, vol);
                return;
            }

            // Try to fetch from provider now (also triggers auto-load if configured there).
            if (!string.IsNullOrEmpty(_endTurnSfxKey) && _assets != null)
            {
                if (_assets.TryGetAudioClip(_endTurnSfxKey, out var clip) && clip != null)
                {
                    _cachedEndTurnClip = clip;
                    _audio.PlayOneShot(_cachedEndTurnClip, vol);
                    return;
                }
                // If not ready yet, continue warming in background.
                StartCoroutine(WarmupEndTurnSfx());
            }

            // Graceful fallback so the click still gives immediate feedback.
            if (_endTurnSfxFallback != null)
            {
                _audio.PlayOneShot(_endTurnSfxFallback, vol);
            }
        }

        private Sprite ResolveSprite()
        {
            if (!string.IsNullOrEmpty(_endTurnSpriteKey) && _assets != null &&
                _assets.TryGetSprite(_endTurnSpriteKey, out var sprite) && sprite != null)
            {
                return sprite;
            }
            return _endTurnSpriteFallback;
        }

        private System.Collections.IEnumerator LateBindIcon()
        {
            // Small grace period to allow Addressables preloads to finish when entering scene directly.
            const float timeout = 2.0f;
            float t = 0f;
            while (t < timeout)
            {
                if (_assets != null && !string.IsNullOrEmpty(_endTurnSpriteKey) && _endTurnInfo != null)
                {
                    if (_assets.TryGetSprite(_endTurnSpriteKey, out var sprite) && sprite != null)
                    {
                        _endTurnInfo.UpdateIcon(sprite);
                        _menu.UpdatePositioning();
                        yield break;
                    }
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        private System.Collections.IEnumerator WarmupEndTurnSfx()
        {
            if (_cachedEndTurnClip != null) yield break;
            if (_assets == null || string.IsNullOrEmpty(_endTurnSfxKey)) yield break;

            float t = 0f;
            while (t < _sfxWarmupTimeout && _cachedEndTurnClip == null)
            {
                if (_assets.TryGetAudioClip(_endTurnSfxKey, out var clip) && clip != null)
                {
                    _cachedEndTurnClip = clip;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        private static string GetLocalized(string table, string key, string fallback)
        {
#if UNITY_LOCALIZATION
            if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(key))
            {
                try
                {
                    var s = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
                    if (!string.IsNullOrEmpty(s))
                        return s;
                }
                catch
                {
                    // Non-fatal: use fallback.
                }
            }
#endif
            return fallback ?? string.Empty;
        }
    }
}
