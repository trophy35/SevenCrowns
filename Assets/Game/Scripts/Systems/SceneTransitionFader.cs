using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.SceneFlow
{
    /// <summary>
    /// Fullscreen UI overlay that fades via CanvasGroup alpha.
    /// Expected on a prefab with Canvas + CanvasGroup + Image (stretched).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneTransitionFader : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _image;

        private void Awake()
        {
            if (_canvas == null) _canvas = GetComponentInChildren<Canvas>(true);
            if (_group == null) _group = GetComponentInChildren<CanvasGroup>(true);
            if (_image == null) _image = GetComponentInChildren<Image>(true);
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
        }

        public void SetSortOrder(int order)
        {
            if (_canvas != null) _canvas.sortingOrder = order;
        }

        public IEnumerator FadeOut(float seconds, AnimationCurve curve, Color color, bool blockRaycasts)
        {
            if (_image != null) _image.color = color;
            if (_group == null || seconds <= 0f)
            {
                if (_group != null)
                {
                    _group.alpha = 1f;
                    _group.blocksRaycasts = blockRaycasts;
                    _group.interactable = blockRaycasts;
                }
                yield break;
            }

            _group.blocksRaycasts = blockRaycasts;
            _group.interactable = blockRaycasts;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float x = Mathf.Clamp01(t / seconds);
                float a = curve != null ? curve.Evaluate(x) : x;
                _group.alpha = a;
                yield return null;
            }
            _group.alpha = 1f;
        }

        public IEnumerator FadeIn(float seconds, AnimationCurve curve, Color color, bool blockRaycasts)
        {
            if (_image != null) _image.color = color;
            if (_group == null || seconds <= 0f)
            {
                if (_group != null)
                {
                    _group.alpha = 0f;
                    _group.blocksRaycasts = false;
                    _group.interactable = false;
                }
                yield break;
            }

            // Keep raycasts blocked during fade-in if requested
            _group.blocksRaycasts = blockRaycasts;
            _group.interactable = blockRaycasts;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float x = Mathf.Clamp01(t / seconds);
                float a = 1f - (curve != null ? curve.Evaluate(x) : x);
                _group.alpha = a;
                yield return null;
            }
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }
    }
}

