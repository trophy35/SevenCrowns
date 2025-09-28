using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Handles map camera edge-pan, keyboard pan, and scroll-wheel zoom while keeping the view clamped to the tilemap bounds.
    /// QA checklist:
    /// - Validate panning (edge + keyboard) at 1080p and 1440p to ensure consistent speed.
    /// - Ensure zoom stays within min/max and the camera never leaves tilemap bounds.
    /// - Verify UI hover blocks edge-pan/zoom when configured.
    /// - Confirm ClickToMove interactions still line up after extreme pan/zoom.
    /// - Profile Update loop to confirm no per-frame allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapCameraController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Grid _grid;
        [SerializeField] private TilemapTileDataProvider _provider;

        [Header("Pan Settings")]
        [SerializeField] private PanSettings _pan = PanSettings.CreateDefault();

        [Header("Zoom Settings")]
        [SerializeField] private ZoomSettings _zoom = ZoomSettings.CreateDefault();

        [Header("Clamp Settings")]
        [SerializeField] private ClampSettings _clamp = ClampSettings.CreateDefault();

        [Header("Runtime State")]
        [SerializeField] private bool _inputEnabled = true;

        private Rect _worldRect;
        private bool _hasWorldRect;
        private Vector3 _targetPosition;
        private Vector3 _positionVelocity;
        private float _targetZoom;
        private float _zoomVelocity;

        /// <summary>Enable or disable player-driven camera input.</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _positionVelocity = Vector3.zero;
                _zoomVelocity = 0f;
            }
        }

        /// <summary>Recomputes the clamp rectangle using the current provider.</summary>
        public void RefreshBounds()
        {
            CacheWorldRect();
            ApplyImmediateClamp();
        }

        /// <summary>Allows swapping the provider at runtime (e.g., when loading a new map).</summary>
        public void ApplyBounds(TilemapTileDataProvider provider)
        {
            if (provider == null) return;
            _provider = provider;
            CacheWorldRect();
            ApplyImmediateClamp();
        }

        private void Reset()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_grid == null) _grid = GetComponentInParent<Grid>();
            if (_provider == null) _provider = GetComponentInParent<TilemapTileDataProvider>();
        }

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("MapCameraController requires a Camera reference.");
                enabled = false;
                return;
            }

            if (_grid == null)
            {
                Debug.LogError("MapCameraController requires a Grid reference.");
                enabled = false;
                return;
            }

            if (_provider == null)
            {
                _provider = GetComponentInParent<TilemapTileDataProvider>();
            }

            _targetPosition = _camera.transform.position;
            _targetZoom = Mathf.Max(0.01f, _camera.orthographicSize);
            CacheWorldRect();
            ApplyImmediateClamp();
        }

        private void OnEnable()
        {
            if (_camera != null)
            {
                _targetPosition = _camera.transform.position;
                _targetZoom = Mathf.Max(0.01f, _camera.orthographicSize);
            }
            CacheWorldRect();
            ApplyImmediateClamp();
        }

        private void OnValidate()
        {
            _pan.edgePanThreshold = Mathf.Max(0f, _pan.edgePanThreshold);
            _pan.edgePanSpeed = Mathf.Max(0f, _pan.edgePanSpeed);
            _pan.keyboardPanSpeed = Mathf.Max(0f, _pan.keyboardPanSpeed);
            _pan.smoothingTime = Mathf.Max(0f, _pan.smoothingTime);

            _zoom.minSize = Mathf.Max(0.01f, _zoom.minSize);
            _zoom.maxSize = Mathf.Max(_zoom.minSize, _zoom.maxSize);
            _zoom.scrollSensitivity = Mathf.Max(0f, _zoom.scrollSensitivity);
            _zoom.smoothTime = Mathf.Max(0f, _zoom.smoothTime);

            _clamp.padding.x = Mathf.Max(0f, _clamp.padding.x);
            _clamp.padding.y = Mathf.Max(0f, _clamp.padding.y);
        }

        private void Update()
        {
            if (_camera == null || _grid == null) return;
            if (!_hasWorldRect)
            {
                CacheWorldRect();
                if (!_hasWorldRect) return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            if (_inputEnabled)
            {
                ApplyPanInput(deltaTime);
                if (_zoom.enabled)
                {
                    ApplyZoomInput();
                }
            }

            float currentSize = UpdateZoom(deltaTime);
            ClampTargetPosition(currentSize);
            UpdateCameraPosition(deltaTime);
        }

        private void ApplyPanInput(float deltaTime)
        {
            Vector2 velocity = Vector2.zero;

            if (_pan.keyboardPanEnabled)
            {
                var keyboardDir = GetKeyboardDirection();
                if (keyboardDir.sqrMagnitude > 0f)
                {
                    velocity += keyboardDir * _pan.keyboardPanSpeed;
                }
            }

            if (_pan.edgePanEnabled)
            {
                bool pointerBlocked = _pan.edgePanBlockedByUI && UiPointerUtility.IsPointerOverUI(Input.mousePosition);
                if (!pointerBlocked)
                {
                    var edgeDir = GetEdgeDirection();
                    if (edgeDir.sqrMagnitude > 0f)
                    {
                        velocity += edgeDir * _pan.edgePanSpeed;
                    }
                }
            }

            if (velocity.sqrMagnitude <= 0f) return;

            MoveTarget(velocity, deltaTime);
        }

        private void ApplyZoomInput()
        {
            if (_zoom.blockWhenPointerOverUI && UiPointerUtility.IsPointerOverUI(Input.mousePosition)) return;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            if (_zoom.invertScroll) scroll = -scroll;

            _targetZoom -= scroll * _zoom.scrollSensitivity;
            _targetZoom = Mathf.Clamp(_targetZoom, _zoom.minSize, _zoom.maxSize);
        }

        private float UpdateZoom(float deltaTime)
        {
            if (!_zoom.enabled) return _camera.orthographicSize;

            float current = _camera.orthographicSize;
            if (Mathf.Approximately(current, _targetZoom))
            {
                _zoomVelocity = 0f;
                return current;
            }

            if (_zoom.smoothTime > 0f)
            {
                current = Mathf.SmoothDamp(current, _targetZoom, ref _zoomVelocity, _zoom.smoothTime, Mathf.Infinity, deltaTime);
            }
            else
            {
                current = _targetZoom;
                _zoomVelocity = 0f;
            }

            _camera.orthographicSize = current;
            return current;
        }

        private void ClampTargetPosition(float orthographicSize)
        {
            if (!_hasWorldRect) return;

            Vector2 halfExtents = GetHalfExtents(orthographicSize);
            _targetPosition = CameraClampUtility.ClampOrthographic(_worldRect, halfExtents, _targetPosition);
        }

        private void UpdateCameraPosition(float deltaTime)
        {
            Vector3 desired = _targetPosition;
            if (_pan.smoothingTime > 0f)
            {
                var current = _camera.transform.position;
                desired = Vector3.SmoothDamp(current, _targetPosition, ref _positionVelocity, _pan.smoothingTime, Mathf.Infinity, deltaTime);
            }
            else
            {
                _positionVelocity = Vector3.zero;
            }

            _camera.transform.position = desired;
        }

        private void MoveTarget(Vector2 velocityPerSecond, float deltaTime)
        {
            Vector3 right = GetPlanarRight();
            Vector3 up = GetPlanarUp();

            _targetPosition += right * (velocityPerSecond.x * deltaTime);
            _targetPosition += up * (velocityPerSecond.y * deltaTime);
        }

        private Vector2 GetKeyboardDirection()
        {
            float horizontal = 0f;
            float vertical = 0f;
            if (!string.IsNullOrEmpty(_pan.horizontalAxis)) horizontal = Input.GetAxisRaw(_pan.horizontalAxis);
            if (!string.IsNullOrEmpty(_pan.verticalAxis)) vertical = Input.GetAxisRaw(_pan.verticalAxis);
            var dir = new Vector2(horizontal, vertical);
            if (_pan.normalizeDiagonal && dir.sqrMagnitude > 1f)
            {
                dir.Normalize();
            }
            return dir;
        }

        private Vector2 GetEdgeDirection()
        {
            if (_pan.edgePanRequiresPointerInWindow)
            {
                var mouse = Input.mousePosition;
                if (mouse.x < 0f || mouse.x > Screen.width || mouse.y < 0f || mouse.y > Screen.height)
                {
                    return Vector2.zero;
                }
            }

            Vector2 dir = Vector2.zero;
            float threshold = _pan.edgePanThreshold;
            if (threshold > 0f)
            {
                var mouse = Input.mousePosition;
                if (mouse.x <= threshold) dir.x -= 1f;
                else if (mouse.x >= Screen.width - threshold) dir.x += 1f;
                if (mouse.y <= threshold) dir.y -= 1f;
                else if (mouse.y >= Screen.height - threshold) dir.y += 1f;
            }

            if (_pan.normalizeDiagonal && dir.sqrMagnitude > 1f)
            {
                dir.Normalize();
            }
            return dir;
        }

        private Vector3 GetPlanarRight()
        {
            var right = _camera.transform.right;
            right.z = 0f;
            float mag = right.sqrMagnitude;
            if (mag < 1e-4f)
            {
                return Vector3.right;
            }
            return right / Mathf.Sqrt(mag);
        }

        private Vector3 GetPlanarUp()
        {
            var up = _camera.transform.up;
            up.z = 0f;
            float mag = up.sqrMagnitude;
            if (mag < 1e-4f)
            {
                return Vector3.up;
            }
            return up / Mathf.Sqrt(mag);
        }

        private Vector2 GetHalfExtents(float orthographicSize)
        {
            float halfHeight = orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            return new Vector2(halfWidth, halfHeight);
        }

        private void CacheWorldRect()
        {
            if (_provider == null)
            {
                _hasWorldRect = false;
                return;
            }

            var bounds = _provider.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                _hasWorldRect = false;
                return;
            }

            var minCell = _provider.CoordToCell(new GridCoord(0, 0));
            var maxCell = _provider.CoordToCell(new GridCoord(bounds.Width - 1, bounds.Height - 1));

            var minWorld = _grid.CellToWorld(minCell);
            var maxExclusiveCell = new Vector3Int(maxCell.x + 1, maxCell.y + 1, maxCell.z);
            var maxWorld = _grid.CellToWorld(maxExclusiveCell);

            float xMin = Mathf.Min(minWorld.x, maxWorld.x) + _clamp.padding.x;
            float xMax = Mathf.Max(minWorld.x, maxWorld.x) - _clamp.padding.x;
            float yMin = Mathf.Min(minWorld.y, maxWorld.y) + _clamp.padding.y;
            float yMax = Mathf.Max(minWorld.y, maxWorld.y) - _clamp.padding.y;

            if (xMax <= xMin || yMax <= yMin)
            {
                _hasWorldRect = false;
                if (_clamp.recenterOnInvalidBounds)
                {
                    float centerX = (xMin + xMax) * 0.5f;
                    float centerY = (yMin + yMax) * 0.5f;
                    _targetPosition = new Vector3(centerX, centerY, _targetPosition.z);
                }
                return;
            }

            _worldRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            _hasWorldRect = true;
        }

        private void ApplyImmediateClamp()
        {
            if (_camera == null || !_hasWorldRect) return;
            float size = Mathf.Max(0.01f, _camera.orthographicSize);
            Vector2 halfExtents = GetHalfExtents(size);
            _targetPosition = CameraClampUtility.ClampOrthographic(_worldRect, halfExtents, _targetPosition);
            _camera.transform.position = _targetPosition;
        }

        [System.Serializable]
        private struct PanSettings
        {
            public bool edgePanEnabled;
            public float edgePanThreshold;
            public float edgePanSpeed;
            public bool edgePanRequiresPointerInWindow;
            public bool edgePanBlockedByUI;
            public bool keyboardPanEnabled;
            public float keyboardPanSpeed;
            public string horizontalAxis;
            public string verticalAxis;
            public bool normalizeDiagonal;
            public float smoothingTime;

            public static PanSettings CreateDefault()
            {
                return new PanSettings
                {
                    edgePanEnabled = true,
                    edgePanThreshold = 32f,
                    edgePanSpeed = 12f,
                    edgePanRequiresPointerInWindow = true,
                    edgePanBlockedByUI = true,
                    keyboardPanEnabled = true,
                    keyboardPanSpeed = 15f,
                    horizontalAxis = "Horizontal",
                    verticalAxis = "Vertical",
                    normalizeDiagonal = true,
                    smoothingTime = 0.1f
                };
            }
        }

        [System.Serializable]
        private struct ZoomSettings
        {
            public bool enabled;
            public float minSize;
            public float maxSize;
            public float scrollSensitivity;
            public bool invertScroll;
            public float smoothTime;
            public bool blockWhenPointerOverUI;

            public static ZoomSettings CreateDefault()
            {
                return new ZoomSettings
                {
                    enabled = true,
                    minSize = 6f,
                    maxSize = 18f,
                    scrollSensitivity = 3f,
                    invertScroll = false,
                    smoothTime = 0.15f,
                    blockWhenPointerOverUI = true
                };
            }
        }

        [System.Serializable]
        private struct ClampSettings
        {
            public Vector2 padding;
            public bool recenterOnInvalidBounds;

            public static ClampSettings CreateDefault()
            {
                return new ClampSettings
                {
                    padding = Vector2.zero,
                    recenterOnInvalidBounds = true
                };
            }
        }
    }
}
