using System;

using System.Collections.Generic;

using UnityEngine;

using SevenCrowns.Map;

using SevenCrowns.Map.Resources;



namespace SevenCrowns.Map

{

    /// <summary>

    /// Scene authoring component for a resource node (e.g., gold pile) placed on the world map.

    /// Handles visual setup and registration into the <see cref="ResourceNodeService"/>.

    /// </summary>

    [DisallowMultipleComponent]

    [RequireComponent(typeof(SpriteRenderer))]

    public sealed class ResourceNodeAuthoring : MonoBehaviour

    {

        private static readonly Dictionary<string, ResourceNodeAuthoring> s_ByNodeId = new(StringComparer.Ordinal);



        private enum VariantSelectionMode

        {

            Specific,

            Random

        }



        [Header("Definition")]

        [SerializeField] private ResourceDefinition _resource;

        [SerializeField, Min(0)] private int _baseYield = 500;



        [Header("Identity")]

        [Tooltip("Stable identifier for this node. Auto-generated when empty.")]

        [SerializeField] private string _nodeId;



        [Header("Variant")]

        [SerializeField] private VariantSelectionMode _variantSelection = VariantSelectionMode.Random;

        [Tooltip("Variant identifier from the ResourceDefinition. Used when VariantSelection is Specific.")]

        [SerializeField] private string _variantId;

        [Tooltip("Optional deterministic seed used when VariantSelection is Random. When zero the node id hash is used.")]

        [SerializeField] private int _randomSeed;



        [Header("Placement")]

        [SerializeField] private Grid _grid;

        [SerializeField] private TilemapTileDataProvider _tileDataProvider;

        [SerializeField] private bool _snapToGrid = true;

        [SerializeField] private Vector3 _manualOffset;



        [Header("Debug")]

        [SerializeField] private bool _logMissingService;



        private SpriteRenderer _spriteRenderer;

        private ResourceNodeService _service;

        private ResourceVisualVariant _resolvedVariant;

        private GridCoord? _gridCoord;

        private Vector3 _authoredWorldPosition;

        private bool _hasCachedPosition;

        private bool _isRegistered;

        private bool _isCollected;



        private void Awake()

        {

            _spriteRenderer = GetComponent<SpriteRenderer>();

            CacheAuthoredPosition();

            EnsureNodeId();

            ResolveService();

        }



        private void OnEnable()

        {

            CacheAuthoredPosition();

            ResolveService();

            ResolveVariant();

            ApplyVisual();

            RegisterSelf();

            RegisterNode();

        }



        private void OnDisable()

        {

            UnregisterSelf();

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

            if (_spriteRenderer == null)

            {

                _spriteRenderer = GetComponent<SpriteRenderer>();

            }



            CacheAuthoredPosition();

            EnsureNodeId();

            if (!Application.isPlaying)

            {

                ResolveVariant();

                ApplyVisual();

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

            if (string.IsNullOrEmpty(_nodeId))

                return;



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

            _isCollected = false;

        }



        private void UnregisterSelf()

        {

            if (string.IsNullOrEmpty(_nodeId))

                return;



            if (s_ByNodeId.TryGetValue(_nodeId, out var existing) && existing == this)

            {

                s_ByNodeId.Remove(_nodeId);

            }

        }



        public static bool TryGetNode(string nodeId, out ResourceNodeAuthoring node)

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



            var candidates = FindObjectsOfType<ResourceNodeAuthoring>(true);

            for (int i = 0; i < candidates.Length; i++)

            {

                var candidate = candidates[i];

                if (candidate == null)

                    continue;



                var candidateId = candidate._nodeId;

                if (string.IsNullOrEmpty(candidateId))

                    continue;



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



        public void Collect()

        {

            if (_isCollected)

                return;



            _isCollected = true;



            if (_service != null && _isRegistered && !string.IsNullOrEmpty(_nodeId))

            {

                _service.Unregister(_nodeId);

                _isRegistered = false;

            }



            UnregisterSelf();



            if (Application.isPlaying)

            {

                Destroy(gameObject);

            }

            else

            {

                DestroyImmediate(gameObject);

            }

        }



        private void ResolveService()

        {

            if (_service != null) return;



            var behaviours = FindObjectsOfType<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)

            {

                if (behaviours[i] is ResourceNodeService service)

                {

                    _service = service;

                    break;

                }

            }



            if (_service == null && _logMissingService)

            {

                Debug.LogWarning($"[ResourceNodeAuthoring] No {nameof(ResourceNodeService)} found in scene.", this);

            }

        }



        private void ResolveVariant()

        {

            _resolvedVariant = default;

            if (_resource == null)

            {

                return;

            }



            switch (_variantSelection)

            {

                case VariantSelectionMode.Specific:

                    if (!string.IsNullOrWhiteSpace(_variantId) && _resource.TryGetVariant(_variantId, out var specific))

                    {

                        _resolvedVariant = specific;

                    }

                    break;

                case VariantSelectionMode.Random:

                    System.Random rng = null;

                    if (_randomSeed != 0)

                    {

                        rng = new System.Random(_randomSeed);

                    }

                    else if (!string.IsNullOrEmpty(_nodeId))

                    {

                        rng = new System.Random(StringComparer.Ordinal.GetHashCode(_nodeId));

                    }



                    if (_resource.TryGetRandomVariant(out var randomVariant, rng))

                    {

                        _resolvedVariant = randomVariant;

                        if (string.IsNullOrEmpty(_variantId))

                        {

                            _variantId = randomVariant.VariantId;

                        }

                    }

                    break;

            }

        }



        private void ApplyVisual()

        {

            if (_spriteRenderer == null)

            {

                return;

            }



            if (!_resolvedVariant.IsValid)

            {

                _spriteRenderer.enabled = false;

                _gridCoord = null;

                if (_resource != null)

                {

                    Debug.LogWarning($"[ResourceNodeAuthoring] Unable to resolve variant '{_variantId}' for resource '{_resource.name}'.", this);

                }

                return;

            }



            _spriteRenderer.enabled = true;

            _spriteRenderer.sprite = _resolvedVariant.Sprite;



            Vector3 basePosition = _authoredWorldPosition;

            _gridCoord = null;



            if (_snapToGrid && _grid != null)

            {

                // Snap visual to nearest cell center at authored position.
                var cell = _grid.WorldToCell(basePosition);

                var snapped = _grid.GetCellCenterWorld(cell);

                basePosition = new Vector3(snapped.x, snapped.y, basePosition.z);

                // If a provider is available, map to its local coord only when inside its baked bounds.
                if (_tileDataProvider != null)
                {
#if UNITY_EDITOR
                    var providerGrid = _tileDataProvider.GroundGrid;
                    if (providerGrid != null && _grid != null && providerGrid != _grid)
                    {
                        Debug.LogWarning($"[ResourceNodeAuthoring] Node '{name}' Grid reference differs from provider's ground Grid. Assign the same Grid as the Tilemap to avoid mapping errors.", this);
                    }
#endif

                    bool inBounds;

                    var local = _tileDataProvider.WorldToCoordUnclamped(_grid, basePosition, out inBounds);

                                        if (inBounds)
                    {
                        _gridCoord = local;
                    }
                    else
                    {
                        _gridCoord = null;
#if UNITY_EDITOR
                        Debug.LogWarning($"[ResourceNodeAuthoring] Node '{name}' is outside Tilemap provider bounds and will not be discoverable for hover/collect. Expand Tilemap or move the node inside.", this);
#endif
                    }

                }

                else

                {

                    _gridCoord = null;

                }

            }



            transform.position = basePosition + _manualOffset + _resolvedVariant.LocalOffset;

        }



        private void RegisterNode()

        {

            if (_service == null || _resource == null || string.IsNullOrEmpty(_nodeId))

            {

                return;

            }



            var descriptor = new ResourceNodeDescriptor(

                _nodeId,

                _resource,

                _resolvedVariant,

                transform.position,

                _gridCoord,

                _baseYield);



            _service.RegisterOrUpdate(descriptor);

            _isRegistered = true;

        }

    }

}



// Editor diagnostic inserted: ensure Grid matches provider's GroundGrid


