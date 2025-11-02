using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI.Cities;

namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// Populates the City "Building" tab list with available buildings for the current faction.
    /// Discovers providers at runtime to keep UI decoupled from Core implementations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityBuildingsListController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Transform _content; // ScrollRect content
        [SerializeField] private BuildingListItemView _itemPrefab;
        [SerializeField, Tooltip("Optional ScrollRect to adjust sensitivity.")]
        private ScrollRect _scrollRect;
        [SerializeField, Min(0f), Tooltip("Mouse wheel scroll sensitivity for the list.")]
        private float _scrollSensitivity = 40f;
        [SerializeField, Tooltip("Enable verbose debug logs for troubleshooting population and provider discovery.")]
        private bool _debugLogs = false;

        [Header("Providers (Optional)")]
        [SerializeField] private MonoBehaviour _catalogProviderBehaviour; // ICityBuildingCatalogProvider
        [SerializeField] private MonoBehaviour _stateProviderBehaviour;   // ICityBuildingStateProvider
        [SerializeField] private MonoBehaviour _researchProviderBehaviour; // IResearchStateProvider
        [SerializeField] private MonoBehaviour _factionProviderBehaviour; // ICityFactionIdProvider
        [SerializeField] private MonoBehaviour _assetProviderBehaviour;   // IUiAssetProvider

        private ICityBuildingCatalogProvider _catalog;
        private ICityBuildingStateProvider _state;
        private IResearchStateProvider _research;
        private ICityFactionIdProvider _faction;
        private SevenCrowns.UI.IUiAssetProvider _assets;

        private readonly List<GameObject> _spawned = new List<GameObject>(16);
        private Coroutine _lateRetryRoutine;
        [SerializeField, Min(0f), Tooltip("Seconds to keep retrying population when catalog is auto-loading.")]
        private float _lateRetryTimeout = 2.0f;

        private void Awake()
        {
            ResolveProviders();
            if (_scrollRect == null)
                _scrollRect = GetComponentInParent<ScrollRect>(true);
            if (_scrollRect != null)
                _scrollRect.scrollSensitivity = Mathf.Max(0f, _scrollSensitivity);
            if (_debugLogs)
            {
                Debug.Log($"[CityBuildingsList] Awake: content={_content!=null} itemPrefab={_itemPrefab!=null} scrollRect={_scrollRect!=null} sensitivity={(_scrollRect!=null?_scrollRect.scrollSensitivity:_scrollSensitivity)}", this);
            }
        }

        private void OnEnable()
        {
            if (_debugLogs) Debug.Log("[CityBuildingsList] OnEnable -> Populate()", this);
            Populate();
        }

        private void OnDisable()
        {
            Clear();
            if (_lateRetryRoutine != null)
            {
                StopCoroutine(_lateRetryRoutine);
                _lateRetryRoutine = null;
            }
        }

        private void ResolveProviders()
        {
            if (_catalogProviderBehaviour != null && _catalogProviderBehaviour is ICityBuildingCatalogProvider c)
                _catalog = c;
            if (_stateProviderBehaviour != null && _stateProviderBehaviour is ICityBuildingStateProvider s)
                _state = s;
            if (_researchProviderBehaviour != null && _researchProviderBehaviour is IResearchStateProvider r)
                _research = r;
            if (_factionProviderBehaviour != null && _factionProviderBehaviour is ICityFactionIdProvider f)
                _faction = f;
            if (_assetProviderBehaviour != null && _assetProviderBehaviour is SevenCrowns.UI.IUiAssetProvider a)
                _assets = a;

            if (_catalog == null || _state == null || _research == null || _faction == null || _assets == null)
            {
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    var mb = behaviours[i];
                    if (_catalog == null && mb is ICityBuildingCatalogProvider cp) _catalog = cp;
                    if (_state == null && mb is ICityBuildingStateProvider sp) _state = sp;
                    if (_research == null && mb is IResearchStateProvider rp) _research = rp;
                    if (_faction == null && mb is ICityFactionIdProvider fp) _faction = fp;
                    if (_assets == null && mb is SevenCrowns.UI.IUiAssetProvider ap) _assets = ap;
                }
            }
            if (_debugLogs)
            {
                Debug.Log($"[CityBuildingsList] ResolveProviders: catalog={_catalog!=null} state={_state!=null} research={_research!=null} faction={_faction!=null} assets={_assets!=null}", this);
            }
        }

        public void Populate()
        {
            if (_content == null || _itemPrefab == null)
            {
                Debug.LogWarning($"[CityBuildingsList] Missing assignments. content={_content!=null} itemPrefab={_itemPrefab!=null}", this);
                return;
            }
            ResolveProviders();
            Clear();

            if (_faction == null || !_faction.TryGetFactionId(out var factionId) || string.IsNullOrEmpty(factionId))
            {
                Debug.LogWarning("[CityBuildingsList] No faction id available from ICityFactionIdProvider.", this);
                return;
            }
            if (_debugLogs) Debug.Log($"[CityBuildingsList] Faction id='{factionId}'", this);

            if (_catalog == null)
            {
                Debug.LogWarning("[CityBuildingsList] No ICityBuildingCatalogProvider found.", this);
                return;
            }
            if (!_catalog.TryGetBuildingEntries(factionId, out var entries) || entries == null)
            {
                Debug.LogWarning($"[CityBuildingsList] Catalog returned no entries for faction='{factionId}'.", this);
                // Late retry population to allow async Addressables auto-load in the catalog service.
                if (_lateRetryRoutine == null && _lateRetryTimeout > 0f)
                {
                    if (_debugLogs) Debug.Log($"[CityBuildingsList] Scheduling late retry for { _lateRetryTimeout }s.", this);
                    _lateRetryRoutine = StartCoroutine(LateRetryPopulate());
                }
                return;
            }
            if (_debugLogs) Debug.Log($"[CityBuildingsList] Entries count={entries.Count}", this);

            for (int i = 0; i < entries.Count; i++)
            {
                var data = entries[i];
                var item = Instantiate(_itemPrefab, _content);
                item.Bind(data, _assets, _state, _research);
                _spawned.Add(item.gameObject);
                if (_debugLogs)
                    Debug.Log($"[CityBuildingsList] Spawned item {i}: buildingId='{data?.buildingId}'", this);
            }
            if (_debugLogs) Debug.Log($"[CityBuildingsList] Populate complete. Spawned={_spawned.Count}", this);
        }

        private System.Collections.IEnumerator LateRetryPopulate()
        {
            float t = 0f;
            while (t < _lateRetryTimeout)
            {
                if (_faction != null && _faction.TryGetFactionId(out var factionId) && !string.IsNullOrEmpty(factionId))
                {
                    if (_catalog != null && _catalog.TryGetBuildingEntries(factionId, out var entries) && entries != null && entries.Count > 0)
                    {
                        if (_debugLogs) Debug.Log("[CityBuildingsList] Late retry success; repopulating.", this);
                        Populate();
                        _lateRetryRoutine = null;
                        yield break;
                    }
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (_debugLogs) Debug.LogWarning("[CityBuildingsList] Late retry timed out with no entries.", this);
            _lateRetryRoutine = null;
        }

        private void Clear()
        {
            if (_content == null) return;
            for (int i = 0; i < _spawned.Count; i++)
            {
                var go = _spawned[i];
                if (go != null)
                {
                    if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            // As a safety net, also clear any other children not in the list (e.g., if instantiated externally)
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
            }
        }
    }
}
