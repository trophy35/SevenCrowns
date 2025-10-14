using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SevenCrowns.Map.Mines
{
    /// <summary>
    /// Scene authoring component for a mine entry placed on the world map.
    /// Registers into MineNodeService and can switch a flag tile when claimed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MineAuthoring : MonoBehaviour
    {
        private static readonly Dictionary<string, MineAuthoring> s_ByNodeId = new(StringComparer.Ordinal);

        [Header("Identity")]
        [Tooltip("Stable identifier for this mine. Auto-generated when empty.")]
        [SerializeField] private string _nodeId;

        [Header("Placement")]
        [SerializeField] private Grid _grid;
        [SerializeField] private TilemapTileDataProvider _tileDataProvider;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private Vector3 _manualOffset;

        [Header("Flag Visuals")] 
        [Tooltip("Optional parent transform for the spawned flag GameObject. When null, parent is this transform.")]
        [SerializeField] private Transform _flagParent;
        [Tooltip("Prefab for the flag visual (SpriteRenderer and optional Animator). When null, a default SpriteRenderer is created.")]
        [SerializeField] private GameObject _flagPrefab;
        [Tooltip("Local offset in world units from the entry cell center to place the flag.")]
        [SerializeField] private Vector3 _flagLocalOffset = new Vector3(0.5f, 0.5f, 0f);
        [Tooltip("Default flag color when no owner color is supplied.")]
        [SerializeField] private Color _defaultFlagColor = Color.white;

        [Header("Debug")]
        [SerializeField] private bool _logMissingService;
        [SerializeField] private bool _logRegistration;

        // SFX are handled by a UI listener (Game.UI) to avoid assembly cycles.

        private MineNodeService _service;
        private GridCoord? _entryCoord;
        private Vector3 _authoredWorldPosition;
        private bool _hasCachedPosition;
        private bool _isRegistered;
        private bool _isOwned;
        private string _ownerId;
        private GameObject _flagInstance;
        private SpriteRenderer _flagSprite;
        private Coroutine _registerRoutine;
        // SFX handled externally

        private void Awake()
        {
            CacheAuthoredPosition();
            EnsureNodeId();
            ResolveService();
        }

        private void OnEnable()
        {
            CacheAuthoredPosition();
            ResolveService();
            ResolveProviderIfMissing();
            RegisterSelf();
            // Defer entry resolution/registration until provider baked (bounds ready)
            if (_registerRoutine != null) StopCoroutine(_registerRoutine);
            _registerRoutine = StartCoroutine(RegisterWhenReady());
        }

        private void OnDisable()
        {
            UnregisterSelf();
            if (_registerRoutine != null)
            {
                StopCoroutine(_registerRoutine);
                _registerRoutine = null;
            }
            if (_service != null && _isRegistered && !string.IsNullOrEmpty(_nodeId))
            {
                _service.Unregister(_nodeId);
                _isRegistered = false;
            }
            
        }

        private void OnDestroy()
        {
            UnregisterSelf();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheAuthoredPosition();
            EnsureNodeId();
            if (!Application.isPlaying)
            {
                ResolveEntryCoord();
            }
        }
#endif

        private void CacheAuthoredPosition()
        {
            if (!_hasCachedPosition)
            {
                _authoredWorldPosition = transform.position;
                _hasCachedPosition = true;
            }
        }

        private void EnsureNodeId()
        {
            if (string.IsNullOrWhiteSpace(_nodeId))
            {
                _nodeId = Guid.NewGuid().ToString("N");
            }
            else
            {
                _nodeId = _nodeId.Trim();
            }
        }

        private void RegisterSelf()
        {
            if (string.IsNullOrEmpty(_nodeId)) return;

            string staleKey = null;
            foreach (var kvp in s_ByNodeId)
            {
                if (kvp.Value == this)
                {
                    staleKey = kvp.Key;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(staleKey))
            {
                s_ByNodeId.Remove(staleKey);
            }
            s_ByNodeId[_nodeId] = this;
        }

        private void UnregisterSelf()
        {
            if (string.IsNullOrEmpty(_nodeId)) return;
            if (s_ByNodeId.TryGetValue(_nodeId, out var existing) && existing == this)
            {
                s_ByNodeId.Remove(_nodeId);
            }
        }

        public static bool TryGetNode(string nodeId, out MineAuthoring node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            nodeId = nodeId.Trim();
            if (s_ByNodeId.TryGetValue(nodeId, out node))
            {
                return true;
            }

            var candidates = FindObjectsOfType<MineAuthoring>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null) continue;
                var candidateId = candidate._nodeId;
                if (string.IsNullOrEmpty(candidateId)) continue;
                if (string.Equals(candidateId, nodeId, StringComparison.Ordinal))
                {
                    candidate.RegisterSelf();
                    node = candidate;
                    return true;
                }
            }
            node = null;
            return false;
        }

        private void ResolveService()
        {
            if (_service != null) return;
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is MineNodeService service)
                {
                    _service = service;
                    break;
                }
            }
            if (_service == null && _logMissingService)
            {
                Debug.LogWarning($"[MineAuthoring] No {nameof(MineNodeService)} found in scene.", this);
            }
        }

        private void ResolveEntryCoord()
        {
            // Always use current transform position at runtime (cached authored pos may be stale in Player)
            Vector3 basePosition = transform.position;
            _entryCoord = null;

            if (_snapToGrid && _grid != null)
            {
                // Prefer provider's GroundGrid for mapping to ensure coord space alignment
                var mappingGrid = _grid;
                var providerGrid = _tileDataProvider != null ? _tileDataProvider.GroundGrid : null;
                if (providerGrid != null && providerGrid != _grid)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[MineAuthoring] Grid reference differs from provider's GroundGrid. Using provider grid for coord mapping.", this);
#endif
                    mappingGrid = providerGrid;
                }

                var cell = mappingGrid.WorldToCell(basePosition);
                var snapped = mappingGrid.GetCellCenterWorld(cell);
                basePosition = new Vector3(snapped.x, snapped.y, basePosition.z);

                if (_tileDataProvider != null)
                {
                    bool inBounds;
                    var localUnclamped = _tileDataProvider.WorldToCoordUnclamped(mappingGrid, basePosition, out inBounds);
                    if (inBounds)
                    {
                        _entryCoord = localUnclamped;
                    }
                    else
                    {
                        // Fallback to clamped coord so hover/lookup uses the same mapping as ClickToMoveController
                        var localClamped = _tileDataProvider.WorldToCoord(mappingGrid, basePosition);
                        _entryCoord = localClamped;
#if UNITY_EDITOR
                        Debug.LogWarning($"[MineAuthoring] Mine '{name}' position is outside provider bounds; clamping entry to {localClamped}. Consider increasing bounds padding or moving the mine.", this);
#endif
                    }
                }
                else
                {
                    Debug.LogWarning($"[MineAuthoring] No TilemapTileDataProvider assigned; cannot compute entry coord.", this);
                }
            }

            // Keep authored position; do not force reposition in runtime (flag uses local offset separately)
            transform.position = basePosition + _manualOffset;
        }

        private void ResolveProviderIfMissing()
        {
            if (_tileDataProvider != null) return;
            // Try to reuse the provider used by ClickToMoveController to ensure coord spaces match
            var ctm = FindObjectOfType<ClickToMoveController>(true);
            if (ctm != null)
            {
                var f = typeof(ClickToMoveController).GetField("_provider", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var prov = (TilemapTileDataProvider)f?.GetValue(ctm);
                if (prov != null)
                {
                    _tileDataProvider = prov;
                    if (_logMissingService)
                        Debug.Log("[MineAuthoring] Auto-bound TilemapTileDataProvider from ClickToMoveController.", this);
                }
            }
            if (_tileDataProvider == null)
            {
                var prov = FindObjectOfType<TilemapTileDataProvider>(true);
                if (prov != null)
                {
                    _tileDataProvider = prov;
                    if (_logMissingService)
                        Debug.Log("[MineAuthoring] Auto-bound first TilemapTileDataProvider in scene.", this);
                }
            }
        }

        private void RegisterNode()
        {
            if (_service == null || string.IsNullOrEmpty(_nodeId)) return;
            var descriptor = new MineNodeDescriptor(_nodeId, transform.position, _entryCoord, _isOwned, _ownerId);
            _service.RegisterOrUpdate(descriptor);
            _isRegistered = true;
            if (_logRegistration)
            {
                Debug.Log($"[MineAuthoring] Registered mine id='{_nodeId}' entry={(_entryCoord.HasValue ? _entryCoord.Value.ToString() : "<null>")} provider='{(_tileDataProvider!=null?_tileDataProvider.name:"<null>")}' grid='{(_grid!=null?_grid.name:"<null>")}'.", this);
            }
        }

        private System.Collections.IEnumerator RegisterWhenReady()
        {
            // Wait up to ~2 seconds for provider bake
            const int maxFrames = 120;
            int frames = 0;
            while (frames++ < maxFrames)
            {
                if (_tileDataProvider == null)
                {
                    ResolveProviderIfMissing();
                }
                if (_tileDataProvider != null && _tileDataProvider.Bounds.Width > 0 && _tileDataProvider.Bounds.Height > 0)
                {
                    var gridToUse = _tileDataProvider.GroundGrid ?? _grid;
                    if (gridToUse != null)
                    {
                        ResolveEntryCoord();
                        RegisterNode();
                        yield break;
                    }
                }
                yield return null;
            }
            if (_logMissingService)
            {
                Debug.LogWarning("[MineAuthoring] Timed out waiting for TilemapTileDataProvider bake; mine entry may be invalid.", this);
            }
        }

        /// <summary>
        /// Claims this mine for the current player and spawns/updates a flag sprite at the configured offset.
        /// </summary>
        public void Claim(string ownerId = "")
        {
            _isOwned = true;
            _ownerId = ownerId ?? string.Empty;
            SpawnOrUpdateFlag(null);
            Claimed?.Invoke(this);
            if (_service != null && !string.IsNullOrEmpty(_nodeId))
            {
                var descriptor = new MineNodeDescriptor(_nodeId, transform.position, _entryCoord, _isOwned, _ownerId);
                _service.RegisterOrUpdate(descriptor);
            }
        }

        /// <summary>
        /// Overload with explicit owner color (e.g., from a player color service).
        /// </summary>
        public void Claim(string ownerId, Color ownerColor)
        {
            _isOwned = true;
            _ownerId = ownerId ?? string.Empty;
            SpawnOrUpdateFlag(ownerColor);
            Claimed?.Invoke(this);
            if (_service != null && !string.IsNullOrEmpty(_nodeId))
            {
                var descriptor = new MineNodeDescriptor(_nodeId, transform.position, _entryCoord, _isOwned, _ownerId);
                _service.RegisterOrUpdate(descriptor);
            }
        }

        private void SpawnOrUpdateFlag(Color? overrideColor)
        {
            if (!_entryCoord.HasValue)
            {
                Debug.LogWarning("[MineAuthoring] Entry coord not resolved; cannot place flag sprite.", this);
                return;
            }

            if (_flagInstance == null)
            {
                var parent = _flagParent != null ? _flagParent : transform;
                if (_flagPrefab != null)
                {
                    _flagInstance = Instantiate(_flagPrefab, parent);
                    _flagInstance.name = string.IsNullOrEmpty(_nodeId) ? "MineFlag" : $"MineFlag_{_nodeId}";
                }
                else
                {
                    _flagInstance = new GameObject(string.IsNullOrEmpty(_nodeId) ? "MineFlag" : $"MineFlag_{_nodeId}");
                    _flagInstance.transform.SetParent(parent, false);
                    _flagSprite = _flagInstance.AddComponent<SpriteRenderer>();
                }
            }

            if (_flagSprite == null)
            {
                _flagSprite = _flagInstance.GetComponent<SpriteRenderer>();
            }

            var targetPos = GetFlagWorldPosition(_entryCoord.Value);
            _flagInstance.transform.position = targetPos;

            var color = overrideColor.HasValue ? overrideColor.Value : _defaultFlagColor;
            if (_flagSprite != null)
            {
                _flagSprite.color = color;
                _flagSprite.sortingOrder += 1;
            }
        }

        private Vector3 GetFlagWorldPosition(GridCoord entry)
        {
            if (_tileDataProvider != null && _grid != null)
            {
                var center = _tileDataProvider.CoordToWorld(_grid, entry);
                return center + _flagLocalOffset;
            }
            if (_grid != null)
            {
                var cell = new Vector3Int(entry.X, entry.Y, 0);
                var center = _grid.GetCellCenterWorld(cell);
                return center + _flagLocalOffset;
            }
            return transform.position + _flagLocalOffset;
        }

        public event Action<MineAuthoring> Claimed;
    }
}
