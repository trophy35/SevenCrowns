using UnityEngine;

namespace SevenCrowns.Boot
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class BlinkCanvasGroup : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 2f;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 1f;

        private CanvasGroup _group;
        private float _phase;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _phase = 0f;
        }

        private void OnEnable()
        {
            _phase = 0f;
        }

        private void Update()
        {
            _phase += Time.deltaTime * _speed;
            float t = 0.5f + 0.5f * Mathf.Sin(_phase);
            _group.alpha = Mathf.Lerp(_minAlpha, _maxAlpha, t);
        }
    }
}
