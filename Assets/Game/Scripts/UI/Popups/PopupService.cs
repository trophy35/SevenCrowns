using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Queues popup requests and manages a single PopupView instance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupService : MonoBehaviour, IPopupService
    {
        [Header("View")]
        [SerializeField] private PopupView _viewPrefab;
        [SerializeField] private Transform _viewParent;
        [SerializeField] private bool _instantiateOnAwake = true;

        private readonly Queue<PendingPopup> _queue = new();
        private PopupView _view;
        private PendingPopup _active;
        private bool _hasActive;

        public bool IsShowing => _hasActive;

        private void Awake()
        {
            if (_viewParent == null)
            {
                _viewParent = transform;
            }

            if (_instantiateOnAwake && _viewPrefab != null && _view == null)
            {
                _view = Instantiate(_viewPrefab, _viewParent);
                _view.gameObject.SetActive(false);
            }
        }

        public void RequestPopup(PopupRequest request, Action<PopupResult> onCompleted)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var pending = new PendingPopup(request, onCompleted);

            if (_hasActive)
            {
                _queue.Enqueue(pending);
                return;
            }

            Show(pending);
        }

        private void Show(PendingPopup pending)
        {
            if (!EnsureView())
            {
                pending.Callback?.Invoke(new PopupResult(PopupOptionIds.Cancel));
                return;
            }

            _active = pending;
            _hasActive = true;

            _view.Show(pending.Request, OnSelection);
        }

        private bool EnsureView()
        {
            if (_view != null) return true;

            if (_viewPrefab == null)
            {
                Debug.LogError("[PopupService] View prefab is not assigned.", this);
                return false;
            }

            _view = Instantiate(_viewPrefab, _viewParent != null ? _viewParent : transform);
            _view.gameObject.SetActive(false);
            return true;
        }

        private void OnSelection(PopupResult result)
        {
            var pending = _active;

            void Complete()
            {
                pending.Callback?.Invoke(result);
                _hasActive = false;

                if (_queue.Count > 0)
                {
                    Show(_queue.Dequeue());
                }
            }

            if (_view != null)
            {
                _view.Hide(Complete);
            }
            else
            {
                Complete();
            }
        }

        private readonly struct PendingPopup
        {
            public PendingPopup(PopupRequest request, Action<PopupResult> callback)
            {
                Request = request;
                Callback = callback;
            }

            public PopupRequest Request { get; }
            public Action<PopupResult> Callback { get; }
        }
    }
}
