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
        [Tooltip("Key for the Move button label in the string table.")]
        [SerializeField] private string _moveNameKey = "Move";
        [Tooltip("Key for the Move button description in the string table.")]
        [SerializeField] private string _moveDescKey = "MoveDesc";

        [Header("Assets (Addressables Keys)")]
        [Tooltip("Addressables key for the End Turn icon sprite (preload recommended).")]
        [SerializeField] private string _endTurnSpriteKey = "UI/Icons/EndTurn";
        [Tooltip("Addressables key for the End Turn SFX (preload recommended).")]
        [SerializeField] private string _endTurnSfxKey = "SFX/UI/EndTurn";
        [Tooltip("Addressables key for the Move icon sprite (preload recommended).")]
        [SerializeField] private string _moveSpriteKey = "UI/Icons/Move";
        [Tooltip("Addressables key for the Move SFX (preload recommended).")]
        [SerializeField] private string _moveSfxKey = "SFX/UI/Move";

        [Header("Fallback Assets (Optional)")]
        [Tooltip("Fallback icon sprite if Addressables not preloaded.")]
        [SerializeField] private Sprite _endTurnSpriteFallback;
        [Tooltip("Fallback SFX if Addressables not preloaded.")]
        [SerializeField] private AudioClip _endTurnSfxFallback;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [Tooltip("Fallback icon sprite for Move if Addressables not preloaded.")]
        [SerializeField] private Sprite _moveSpriteFallback;
        [Tooltip("Fallback SFX for Move if Addressables not preloaded.")]
        [SerializeField] private AudioClip _moveSfxFallback;

        [Header("Events")]
        [Tooltip("Invoked when the End Turn button is pressed.")]
        [SerializeField] private UnityEvent _onEndTurnRequested;
        [Tooltip("Invoked with true/false when the Move mode button toggles.")]
        [SerializeField] private UnityEvent<bool> _onMoveModeChanged;

        private UltimateRadialButtonInfo _endTurnInfo;
        private UltimateRadialButtonInfo _moveInfo;
        private bool _registered;
        private AudioSource _audio;
        private IUiAssetProvider _assets;
        private AudioClip _cachedEndTurnClip;
        private AudioClip _cachedMoveClip;
        [SerializeField, Min(0f)] private float _sfxWarmupTimeout = 2.0f;
        private bool _moveActive;

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
            EnsureMoveButton();
            _menu.Enable();
            // Proactively warm up SFX so first click can play immediately even if Addressables were not preloaded.
            StartCoroutine(WarmupSfx(_endTurnSfxKey, clip => _cachedEndTurnClip = clip));
            StartCoroutine(WarmupSfx(_moveSfxKey, clip => _cachedMoveClip = clip));
        }

        /// <summary>
        /// Creates and registers the End Turn radial button if not already registered.
        /// </summary>
        public void EnsureEndTurnButton()
        {
            if (_registered || _menu == null)
            {
                // Registration covers both buttons; if one is present, skip duplicate work here.
                // Defer to EnsureMoveButton to register the second if needed.
                // Proceed to create End Turn if nothing is registered yet.
            }

            var name = GetLocalized(_uiStringTable, _endTurnNameKey, string.Empty);
            var desc = GetLocalized(_uiStringTable, _endTurnDescKey, string.Empty);

            var icon = ResolveSprite(_endTurnSpriteKey, _endTurnSpriteFallback);
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
                StartCoroutine(LateBindIcon(_endTurnSpriteKey, _endTurnInfo));
            }
        }

        /// <summary>
        /// Creates and registers the Move radial button.
        /// </summary>
        public void EnsureMoveButton()
        {
            if (_menu == null) return;

            var name = GetLocalized(_uiStringTable, _moveNameKey, string.Empty);
            var desc = GetLocalized(_uiStringTable, _moveDescKey, string.Empty);
            var icon = ResolveSprite(_moveSpriteKey, _moveSpriteFallback);

            _moveInfo = new UltimateRadialButtonInfo
            {
                name = name,
                description = desc,
                icon = icon,
                key = "move",
                id = 1
            };
            _menu.RegisterButton(OnMovePressed, _moveInfo, -1);

            if (icon == null && !string.IsNullOrEmpty(_moveSpriteKey))
            {
                StartCoroutine(LateBindIcon(_moveSpriteKey, _moveInfo));
            }
        }

        /// <summary>
        /// Re-applies localized strings to the End Turn button.
        /// </summary>
        public void RefreshLocalization()
        {
            if (_endTurnInfo != null)
            {
                var name = GetLocalized(_uiStringTable, _endTurnNameKey, string.Empty);
                var desc = GetLocalized(_uiStringTable, _endTurnDescKey, string.Empty);
                _endTurnInfo.UpdateName(name);
                _endTurnInfo.UpdateDescription(desc);
            }

            if (_moveInfo != null)
            {
                var mname = GetLocalized(_uiStringTable, _moveNameKey, string.Empty);
                var mdesc = GetLocalized(_uiStringTable, _moveDescKey, string.Empty);
                _moveInfo.UpdateName(mname);
                _moveInfo.UpdateDescription(mdesc);
            }
        }

        private void OnEndTurnPressed()
        {
            Debug.Log("OnEndTurnPressed");
            TryPlaySfx(_cachedEndTurnClip, _endTurnSfxKey, _endTurnSfxFallback, clip => _cachedEndTurnClip = clip);
            _onEndTurnRequested?.Invoke();
        }

        private void OnMovePressed()
        {
            _moveActive = !_moveActive;
            TryPlaySfx(_cachedMoveClip, _moveSfxKey, _moveSfxFallback, clip => _cachedMoveClip = clip);
            _onMoveModeChanged?.Invoke(_moveActive);
        }

        /// <summary>
        /// Allows external systems (e.g., in Game.Core) to subscribe to Move mode changes without creating assembly cycles.
        /// </summary>
        public void AddMoveModeChangedListener(UnityAction<bool> listener)
        {
            if (listener == null) return;
            if (_onMoveModeChanged == null) _onMoveModeChanged = new UnityEvent<bool>();
            _onMoveModeChanged.AddListener(listener);
        }

        /// <summary>
        /// Removes a previously added listener for Move mode changes.
        /// </summary>
        public void RemoveMoveModeChangedListener(UnityAction<bool> listener)
        {
            if (listener == null || _onMoveModeChanged == null) return;
            _onMoveModeChanged.RemoveListener(listener);
        }

        /// <summary>
        /// Programmatically sets the Move button active state without invoking the external event or playing SFX.
        /// Use this to keep UI state in sync with gameplay logic (e.g., when selection changes).
        /// </summary>
        public void SetMoveActive(bool active)
        {
            _moveActive = active;
        }

        private void TryPlaySfx(AudioClip cached, string sfxKey, AudioClip fallback, System.Action<AudioClip> setCached)
        {
            float vol = Mathf.Clamp01(_sfxVolume);
            if (vol <= 0f || _audio == null)
                return;

            // Prefer cached clip if warm-up succeeded.
            if (cached != null)
            {
                _audio.PlayOneShot(cached, vol);
                return;
            }

            // Try to fetch from provider now (also triggers auto-load if configured there).
            if (!string.IsNullOrEmpty(sfxKey) && _assets != null)
            {
                if (_assets.TryGetAudioClip(sfxKey, out var clip) && clip != null)
                {
                    setCached?.Invoke(clip);
                    _audio.PlayOneShot(clip, vol);
                    return;
                }
                // If not ready yet, continue warming in background.
                StartCoroutine(WarmupSfx(sfxKey, setCached));
            }

            // Graceful fallback so the click still gives immediate feedback.
            if (fallback != null)
            {
                _audio.PlayOneShot(fallback, vol);
            }
        }

        private Sprite ResolveSprite(string spriteKey, Sprite fallback)
        {
            if (!string.IsNullOrEmpty(spriteKey) && _assets != null &&
                _assets.TryGetSprite(spriteKey, out var sprite) && sprite != null)
            {
                return sprite;
            }
            return fallback;
        }

        private System.Collections.IEnumerator LateBindIcon(string spriteKey, UltimateRadialButtonInfo info)
        {
            // Small grace period to allow Addressables preloads to finish when entering scene directly.
            const float timeout = 2.0f;
            float t = 0f;
            while (t < timeout)
            {
                if (_assets != null && !string.IsNullOrEmpty(spriteKey) && info != null)
                {
                    if (_assets.TryGetSprite(spriteKey, out var sprite) && sprite != null)
                    {
                        info.UpdateIcon(sprite);
                        _menu.UpdatePositioning();
                        yield break;
                    }
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        private System.Collections.IEnumerator WarmupSfx(string key, System.Action<AudioClip> setCached)
        {
            if (_assets == null || string.IsNullOrEmpty(key) || setCached == null) yield break;

            float t = 0f;
            AudioClip local = null;
            while (t < _sfxWarmupTimeout && local == null)
            {
                if (_assets.TryGetAudioClip(key, out var clip) && clip != null)
                {
                    local = clip;
                    setCached(local);
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
