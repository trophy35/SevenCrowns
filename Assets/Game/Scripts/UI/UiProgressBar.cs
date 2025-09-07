using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Reusable progress bar with immediate and smooth updates.
    /// - Assign a Filled Horizontal Image to <see cref="_fill"/>.
    /// - Optionally assign a TMP label to display numeric percent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiProgressBar : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private Image _fill;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField, Min(0.1f), Tooltip("Lerp speed for SetSmooth (units per second)")]
        private float _smoothSpeed = 3f;
        [SerializeField, Tooltip("If true, label shows NN% automatically")] private bool _useNumericLabel = true;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, the bar follows Debug Value, overriding external updates.")]
        private bool _debugOverride = false;
        [SerializeField, Range(0f, 1f), Tooltip("Manual value (0..1) when Debug Override is enabled.")]
        private float _debugValue = 0f;
        [SerializeField, Tooltip("When Debug Override is enabled, animate towards Debug Value instead of snapping.")]
        private bool _debugUseSmooth = false;

        private Coroutine _smoothRoutine;
        private float _target = 0f;

        /// <summary>Instantly sets the bar to the provided 0..1 value and stops smoothing.</summary>
        /// <param name="value01">The fill amount, clamped between 0 and 1.</param>
        public void SetImmediate(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            _target = value01;
            if (_smoothRoutine != null)
            {
                StopCoroutine(_smoothRoutine);
                _smoothRoutine = null;
            }
            Apply(value01);
        }

        /// <summary>
        /// Smoothly animates the bar towards the provided 0..1 value. Subsequent calls retarget the animation.
        /// </summary>
        /// <param name="target01">The target fill amount, clamped between 0 and 1.</param>
        public void SetSmooth(float target01)
        {
            _target = Mathf.Clamp01(target01);
            if (_smoothRoutine == null)
            {
                _smoothRoutine = StartCoroutine(SmoothToTarget());
            }
        }

        /// <summary>Explicitly update the label text (used when not showing numeric percent).</summary>
        /// <param name="text">The text to display on the label.</param>
        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <summary>Enable/disable automatic numeric percent on the label.</summary>
        /// <param name="on">Whether to use numeric labeling.</param>
        public void UseNumericLabel(bool on)
        {
            _useNumericLabel = on;
            // Refresh once to reflect mode switch
            if (_useNumericLabel && _fill != null)
                UpdateNumeric(_fill.fillAmount);
        }

        private void Update()
        {
            if (!_debugOverride) return;
            float v = Mathf.Clamp01(_debugValue);
            if (_debugUseSmooth && Application.isPlaying)
                SetSmooth(v);
            else
                SetImmediate(v);
        }

        /// <summary>
        /// Coroutine that smoothly animates the progress bar to the target value.
        /// </summary>
        private IEnumerator SmoothToTarget()
        {
            // Ensure we have a starting point
            float current = _fill != null ? _fill.fillAmount : 0f;
            while (true)
            {
                if (Mathf.Abs(current - _target) <= 0.001f)
                {
                    current = _target;
                    Apply(current);
                    _smoothRoutine = null;
                    yield break;
                }

                current = Mathf.MoveTowards(current, _target, _smoothSpeed * Time.unscaledDeltaTime);
                Apply(current);
                yield return null;
            }
        }

        /// <summary>
        /// Applies the given fill amount to the Image and updates the numeric label if enabled.
        /// </summary>
        /// <param name="value01">The fill amount, between 0 and 1.</param>
        private void Apply(float value01)
        {
            if (_fill != null)
                _fill.fillAmount = value01;
            if (_useNumericLabel)
                UpdateNumeric(value01);
        }

        /// <summary>
        /// Updates the numeric label with the percentage representation of the fill amount.
        /// </summary>
        /// <param name="value01">The fill amount, between 0 and 1.</param>
        private void UpdateNumeric(float value01)
        {
            if (_label == null) return;
            int pct = Mathf.RoundToInt(value01 * 100f);
            _label.text = pct + "%";
        }

        /// <summary>
        /// Called when the script is loaded or a value is changed in the Inspector.
        /// Attempts to automatically wire up the _fill and _label if they are not already assigned.
        /// </summary>
        private void Reset()
        {
            // Try auto-wire on add
            if (_fill == null)
                _fill = GetComponentInChildren<Image>();
            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called in the editor when the script is loaded or a value changes in the Inspector.
        /// Ensures that the _fill Image is of type Filled and has the correct settings.
        /// </summary>
        private void OnValidate()
        {
            if (_fill == null)
                _fill = GetComponentInChildren<Image>();

            if (_fill != null && _fill.type != Image.Type.Filled)
            {
                // Keep silent in player; warn in editor to set up correctly
                _fill.type = Image.Type.Filled;
                _fill.fillMethod = Image.FillMethod.Horizontal;
                _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _fill.fillAmount = Mathf.Clamp01(_fill.fillAmount);
            }

            if (_debugOverride)
            {
                // Reflect debug value in editor immediately
                Apply(Mathf.Clamp01(_debugValue));
            }
        }

        [ContextMenu("Test 0%")]
        private void CM_TestZero() => SetImmediate(0f);

        [ContextMenu("Test 100%")]
        private void CM_TestFull() => SetImmediate(1f);
#endif
    }
}
