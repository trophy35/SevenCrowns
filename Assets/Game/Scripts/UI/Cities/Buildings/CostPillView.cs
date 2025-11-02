using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// Displays a single resource cost as an icon + amount.
    /// Uses IUiAssetProvider to resolve the resource icon via a key format.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CostPillView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _amountText;

        [Header("Assets")]
        [SerializeField, Tooltip("Optional explicit asset provider behaviour. When null, discovered in scene.")]
        private MonoBehaviour _assetProviderBehaviour; // IUiAssetProvider
        [SerializeField, Tooltip("Key format for resource icons. Use {0} for resource id.")]
        private string _resourceIconKeyFormat = "UI/Resources/{0}";

        [Header("Wallet & State Visuals")]
        [SerializeField, Tooltip("Optional explicit wallet provider. When null, discovered in scene.")]
        private MonoBehaviour _walletBehaviour; // IResourceWallet
        [SerializeField, Tooltip("State image swapped to indicate whether the player can afford this cost.")]
        private Image _stateImage; // Optional
        [SerializeField, Tooltip("Sprite used when the player has enough of this resource.")]
        private Sprite _enoughSprite; // Optional
        [SerializeField, Tooltip("Sprite used when the player does not have enough of this resource.")]
        private Sprite _notEnoughSprite; // Optional
        [Header("Visual Tint (Optional)")]
        [SerializeField, Tooltip("Optional Image to tint for glow/highlight. If assigned, its color will be set based on affordability.")]
        private Image _tintTarget; // Optional external image (e.g., glow) to color
        [SerializeField, Tooltip("Tint color when the player has enough resources.")]
        private Color _enoughColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField, Tooltip("Tint color when the player does not have enough resources.")]
        private Color _notEnoughColor = new Color(1f, 0.25f, 0.25f, 1f);

        private SevenCrowns.UI.IUiAssetProvider _assets;
        private SevenCrowns.Map.Resources.IResourceWallet _wallet;
        private readonly System.Globalization.CultureInfo _culture = System.Globalization.CultureInfo.InvariantCulture;
        [SerializeField, Tooltip("Enable verbose debug logs for resource icon resolution.")]
        private bool _debugLogs = false;
        [SerializeField, Min(0f), Tooltip("Seconds to retry resolving the resource icon after auto-load.")]
        private float _lateBindTimeout = 2.0f;
        private string _lastKey;
        private Coroutine _lateBindRoutine;
        private string _boundResourceId;
        private int _requiredAmount;
        [SerializeField, Min(0f), Tooltip("Seconds to wait for a wallet service to appear when none is available on enable.")]
        private float _waitForWalletTimeout = 2.0f;
        private Coroutine _waitWalletRoutine;

        private void Awake()
        {
            if (_assetProviderBehaviour != null && _assetProviderBehaviour is SevenCrowns.UI.IUiAssetProvider p)
                _assets = p;
            if (_icon == null)
                _icon = GetComponentInChildren<Image>(true);
            if (_amountText == null)
                _amountText = GetComponentInChildren<TextMeshProUGUI>(true);

            // Resolve wallet eagerly if provided explicitly
            if (_walletBehaviour != null && _walletBehaviour is SevenCrowns.Map.Resources.IResourceWallet w)
                _wallet = w;
            if (_debugLogs)
            {
                Debug.Log($"[CostPill] Awake: icon={_icon!=null} amountText={_amountText!=null} providerAssigned={_assets!=null} walletAssigned={_wallet!=null}", this);
            }
        }

        private void OnEnable()
        {
            ResolveWallet();
            if (_wallet != null)
            {
                _wallet.ResourceChanged += OnWalletChanged;
                if (_debugLogs) Debug.Log("[CostPill] OnEnable: wallet subscribed.", this);
            }
            else if (_waitWalletRoutine == null && _waitForWalletTimeout > 0f)
            {
                if (_debugLogs) Debug.Log($"[CostPill] OnEnable: wallet not found; waiting up to {_waitForWalletTimeout:0.00}s.", this);
                _waitWalletRoutine = StartCoroutine(WaitForWalletThenRefresh());
            }
            // Refresh state if we have a bound resource
            if (!string.IsNullOrEmpty(_boundResourceId))
                RefreshAffordability();
        }

        private void OnDisable()
        {
            if (_wallet != null)
                _wallet.ResourceChanged -= OnWalletChanged;
            if (_waitWalletRoutine != null)
            {
                StopCoroutine(_waitWalletRoutine);
                _waitWalletRoutine = null;
            }
            if (_debugLogs) Debug.Log("[CostPill] OnDisable: unsubscribed and cleared wait.", this);
        }

        public void Bind(string resourceId, int amount)
        {
            if (_amountText != null)
                _amountText.text = amount.ToString(_culture);

            if (_icon == null)
                return;

            if (_assets == null)
            {
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && _assets == null; i++)
                {
                    if (behaviours[i] is SevenCrowns.UI.IUiAssetProvider a)
                        _assets = a;
                }
            }

            _boundResourceId = string.IsNullOrEmpty(resourceId) ? string.Empty : resourceId.Trim();
            _requiredAmount = Mathf.Max(0, amount);
            if (_debugLogs)
            {
                Debug.Log($"[CostPill] Bind: resource='{_boundResourceId}' len={_boundResourceId?.Length??0} amount={_requiredAmount} assetProvider={_assets!=null}", this);
            }

            var key = string.Format(string.IsNullOrEmpty(_resourceIconKeyFormat) ? "{0}" : _resourceIconKeyFormat, resourceId);
            _lastKey = key;
            if (_assets != null && _assets.TryGetSprite(key, out var sprite) && sprite != null)
            {
                _icon.sprite = sprite;
                _icon.enabled = true;
                if (_debugLogs) Debug.Log($"[CostPill] Icon resolved: resource='{resourceId}' key='{key}' amount={amount}", this);
            }
            else
            {
                // No icon available – disable image to avoid empty placeholder.
                _icon.enabled = false;
                if (_debugLogs) Debug.LogWarning($"[CostPill] Icon NOT resolved: resource='{resourceId}' key='{key}' amount={amount}", this);
                if (Application.isPlaying && _lateBindTimeout > 0f && !string.IsNullOrEmpty(_lastKey))
                {
                    if (_lateBindRoutine != null) StopCoroutine(_lateBindRoutine);
                    _lateBindRoutine = StartCoroutine(LateBind());
                }
            }

            RefreshAffordability();
        }

        private System.Collections.IEnumerator LateBind()
        {
            if (_debugLogs) Debug.Log("[CostPill] WaitForWallet: start", this);
            float t = 0f;
            while (t < _lateBindTimeout)
            {
                if (_assets == null)
                {
                    var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                    for (int i = 0; i < behaviours.Length && _assets == null; i++)
                    {
                        if (behaviours[i] is SevenCrowns.UI.IUiAssetProvider a) _assets = a;
                    }
                }
                if (_assets != null && !string.IsNullOrEmpty(_lastKey) && _assets.TryGetSprite(_lastKey, out var sprite) && sprite != null)
                {
                    if (_icon != null)
                    {
                        _icon.sprite = sprite;
                        _icon.enabled = true;
                    }
                    if (_debugLogs) Debug.Log($"[CostPill] LateBind icon success for key='{_lastKey}' at t={t:0.00}s", this);
                    _lateBindRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_debugLogs) Debug.LogWarning($"[CostPill] LateBind icon timeout for key='{_lastKey}' after {t:0.00}s", this);
            _lateBindRoutine = null;
        }

        private void ResolveWallet()
        {
            if (_wallet != null) return;
            // Prefer an explicit City wallet provider when available
            var providers = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < providers.Length && _wallet == null; i++)
            {
                if (providers[i] is SevenCrowns.UI.Cities.ICityWalletProvider p && p.TryGetWallet(out var cw) && cw != null)
                {
                    _wallet = cw;
                    if (_debugLogs)
                    {
                        var comp = _wallet as Component;
                        string n = comp != null ? comp.gameObject.name : "(unknown)";
                        int amt = !string.IsNullOrEmpty(_boundResourceId) ? _wallet.GetAmount(_boundResourceId) : -1;
                        Debug.Log($"[CostPill] ResolveWallet: bound via ICityWalletProvider go='{n}' amount({_boundResourceId})={amt}", this);
                        // Also list all wallets in scene for diagnostics when duplicates exist
                        var behavioursAll = FindObjectsOfType<MonoBehaviour>(true);
                        int count = 0;
                        for (int j = 0; j < behavioursAll.Length; j++)
                        {
                            if (behavioursAll[j] is SevenCrowns.Map.Resources.IResourceWallet cand)
                            {
                                count++;
                                var comp2 = cand as Component;
                                string gn = comp2 != null ? comp2.gameObject.name : "(unknown)";
                                int val = !string.IsNullOrEmpty(_boundResourceId) ? cand.GetAmount(_boundResourceId) : -1;
                                Debug.Log($"[CostPill] ResolveWallet: wallet[{count}] go='{gn}' amount({_boundResourceId})={val}", this);
                            }
                        }
                    }
                }
            }
            if (_wallet != null) return;
            if (_walletBehaviour != null && _walletBehaviour is SevenCrowns.Map.Resources.IResourceWallet wb)
            {
                _wallet = wb;
                if (_debugLogs) Debug.Log("[CostPill] ResolveWallet: bound via explicit behaviour.", this);
                return;
            }

            var behaviours = providers.Length > 0 ? providers : FindObjectsOfType<MonoBehaviour>(true);
            int found = 0;
            SevenCrowns.Map.Resources.IResourceWallet first = null;
            if (_debugLogs)
            {
                Debug.Log("[CostPill] ResolveWallet: scanning scene for IResourceWallet...", this);
            }
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is SevenCrowns.Map.Resources.IResourceWallet candidate)
                {
                    found++;
                    if (first == null) first = candidate;
                    if (_debugLogs)
                    {
                        var go = behaviours[i] != null ? behaviours[i].gameObject : null;
                        string n = go != null ? go.name : "(null)";
                        int amt = !string.IsNullOrEmpty(_boundResourceId) ? candidate.GetAmount(_boundResourceId) : -1;
                        Debug.Log($"[CostPill] ResolveWallet: candidate[{found}] go='{n}' amount({_boundResourceId})={amt}", this);
                    }
                }
            }
            _wallet = first;
            if (_debugLogs)
            {
                if (_wallet != null)
                {
                    var comp = _wallet as Component;
                    string chosen = comp != null ? comp.gameObject.name : "(unknown)";
                    Debug.Log($"[CostPill] ResolveWallet: selected wallet='{chosen}' out of {found} candidates.", this);
                }
                else
                {
                    Debug.LogWarning("[CostPill] ResolveWallet: no IResourceWallet found in scene.", this);
                }
            }
        }

        private void OnWalletChanged(SevenCrowns.Map.Resources.ResourceChange change)
        {
            if (string.IsNullOrEmpty(_boundResourceId)) return;
            if (!string.Equals(change.ResourceId, _boundResourceId, System.StringComparison.Ordinal)) return;
            if (_debugLogs) Debug.Log($"[CostPill] WalletChanged: resource='{change.ResourceId}' new={change.NewAmount} required={_requiredAmount}", this);
            RefreshAffordability();
        }

        private System.Collections.IEnumerator WaitForWalletThenRefresh()
        {
            float t = 0f;
            while (t < _waitForWalletTimeout && _wallet == null)
            {
                ResolveWallet();
                if (_wallet != null)
                {
                    _wallet.ResourceChanged += OnWalletChanged;
                    RefreshAffordability();
                    if (_debugLogs) Debug.Log("[CostPill] WaitForWallet: wallet bound and refreshed.", this);
                    _waitWalletRoutine = null;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_debugLogs) Debug.LogWarning("[CostPill] WaitForWallet: timeout without wallet.", this);
            _waitWalletRoutine = null;
        }

        private void RefreshAffordability()
        {
            if (_stateImage == null) return;
            if (string.IsNullOrEmpty(_boundResourceId)) return;
            ResolveWallet();
            if (_wallet == null) return;

            int have = _wallet.GetAmount(_boundResourceId);
            bool enough = have >= _requiredAmount;

            if (_enoughSprite == null || _notEnoughSprite == null)
            {
                // Dedicated sprites are required; do not fall back to tinting.
                if (_debugLogs) Debug.LogWarning("[CostPill] State sprites not assigned; disabling state image.", this);
                _stateImage.enabled = false;
                return;
            }

            _stateImage.sprite = enough ? _enoughSprite : _notEnoughSprite;
            _stateImage.enabled = true;
            // Apply optional tint on a separate visual (e.g., glow) if provided
            if (_tintTarget != null)
            {
                _tintTarget.color = enough ? _enoughColor : _notEnoughColor;
            }
            if (_debugLogs)
            {
                var comp = _wallet as Component;
                string wname = comp != null ? comp.gameObject.name : "(unknown)";
                Debug.Log($"[CostPill] RefreshAffordability: wallet='{wname}' resource='{_boundResourceId}' have={have} required={_requiredAmount} enough={enough} sprite='{_stateImage.sprite?.name}' tintTarget={_tintTarget!=null} tintColor='{(enough?_enoughColor:_notEnoughColor)}'", this);
                // Diagnose potential duplicate wallets or resets occurring after transfer apply
                var behavioursAll = FindObjectsOfType<MonoBehaviour>(true);
                int count = 0;
                for (int j = 0; j < behavioursAll.Length; j++)
                {
                    if (behavioursAll[j] is SevenCrowns.Map.Resources.IResourceWallet cand)
                    {
                        count++;
                        var comp2 = cand as Component;
                        string gn = comp2 != null ? comp2.gameObject.name : "(unknown)";
                        int val = cand.GetAmount(_boundResourceId);
                        Debug.Log($"[CostPill] AffordabilityDiag: wallet[{count}] go='{gn}' amount({_boundResourceId})={val}", this);
                    }
                }
            }
        }
    }
}
