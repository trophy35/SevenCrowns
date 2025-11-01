using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Lightweight hover tint for uGUI buttons and images.
    /// - Uses Graphic.CrossFadeColor for low-allocation color transitions.
    /// - Honors Button.interactable when present (no hover when disabled).
    /// - Works best with the Button Transition set to None to avoid double-tinting.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class ButtonHoverColorTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Target")]
        [SerializeField] private Graphic _target; // Defaults to self

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("Tween")] 
        [SerializeField, Min(0f)] private float _fadeDuration = 0f; // 0 = instant

        private Button _button;
        private bool _hovered;

        public Color NormalColor { get => _normalColor; set => _normalColor = value; }
        public Color HoverColor { get => _hoverColor; set => _hoverColor = value; }
        public float FadeDuration { get => _fadeDuration; set => _fadeDuration = Mathf.Max(0f, value); }

        private void Awake()
        {
            EnsureTarget();
            TryGetComponent(out _button);
        }

        private void OnEnable()
        {
            EnsureTarget();
            ApplyColor(_normalColor, immediate: true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable())
                return;
            _hovered = true;
            ApplyColor(_hoverColor, immediate: _fadeDuration <= 0f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyColor(_normalColor, immediate: _fadeDuration <= 0f);
        }

        private bool IsInteractable()
        {
            return _button == null || (_button.isActiveAndEnabled && _button.interactable);
        }

        private void ApplyColor(Color c, bool immediate)
        {
            if (_target == null)
            {
                EnsureTarget();
                if (_target == null) return;
            }
            if (immediate)
            {
                _target.color = c;
            }
            else
            {
                _target.CrossFadeColor(c, _fadeDuration, true, true);
            }
        }

        private void EnsureTarget()
        {
            if (_target != null) return;
            if (!TryGetComponent(out _target))
            {
                _target = GetComponentInChildren<Graphic>();
            }
        }
    }
}
