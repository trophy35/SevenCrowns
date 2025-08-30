using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class UiProgressBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _barRect;   // Container: ProgressBar
        [SerializeField] private RectTransform _fillRect;  // Child: Fill (Image)

        [Header("Progress")]
        [Range(0f, 1f)]
        [SerializeField] private float _value = 0f;        // 0..1
        [SerializeField] private bool _smooth = false;
        [SerializeField] private float _smoothSpeed = 8f;  // units per second

        [Header("Inner Padding (pixels)")]
        [SerializeField] private float _left = 2f;
        [SerializeField] private float _right = 2f;
        [SerializeField] private float _top = 2f;
        [SerializeField] private float _bottom = 2f;

        [Header("Height Handling")]
        [SerializeField] private bool _autoFitHeight = true;

        private float _displayed; // actually rendered value (for smoothing)

        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                if (!_smooth)
                {
                    _displayed = _value;
                    UpdateVisual();
                }
            }
        }

        private void Reset()
        {
            if (_barRect == null) _barRect = GetComponent<RectTransform>();
            if (_fillRect == null && transform.childCount > 0)
            {
                _fillRect = transform.GetChild(0) as RectTransform;
            }

            EnsureFillAnchors();
            _displayed = _value;
            UpdateVisual();
        }

        private void OnValidate()
        {
            if (_barRect == null) _barRect = GetComponent<RectTransform>();
            EnsureFillAnchors();
            if (!Application.isPlaying)
            {
                _displayed = _value;
                UpdateVisual();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (_smooth)
            {
                _displayed = Mathf.MoveTowards(_displayed, _value, _smoothSpeed * Time.deltaTime);
                UpdateVisual();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateVisual();
        }

        private void EnsureFillAnchors()
        {
            if (_fillRect == null) return;

            // Left-anchored strip we resize manually (compensates inner padding exactly)
            if (_fillRect.anchorMin != new Vector2(0f, 0.5f)) _fillRect.anchorMin = new Vector2(0f, 0.5f);
            if (_fillRect.anchorMax != new Vector2(0f, 0.5f)) _fillRect.anchorMax = new Vector2(0f, 0.5f);
            if (_fillRect.pivot != new Vector2(0f, 0.5f)) _fillRect.pivot = new Vector2(0f, 0.5f);
        }

        private void UpdateVisual()
        {
            if (_barRect == null || _fillRect == null) return;

            float barWidth = _barRect.rect.width;
            float barHeight = _barRect.rect.height;

            float innerW = Mathf.Max(0f, barWidth - _left - _right);
            float innerH = Mathf.Max(0f, barHeight - _top - _bottom);

            float fillW = innerW * Mathf.Clamp01(_displayed);

            Vector2 size = _fillRect.sizeDelta;
            Vector2 pos = _fillRect.anchoredPosition;

            pos.x = _left;
            pos.y = 0f;

            if (_autoFitHeight)
            {
                size.y = innerH;
            }

            size.x = fillW;

            _fillRect.anchoredPosition = pos;
            _fillRect.sizeDelta = size;
        }

        public void SetImmediate(float value)
        {
            Value = Mathf.Clamp01(value);
            _displayed = _value;
            UpdateVisual();
        }

        public void SetSmooth(float value)
        {
            Value = Mathf.Clamp01(value);
            // Rendering happens in Update via smoothing
        }
    }
}