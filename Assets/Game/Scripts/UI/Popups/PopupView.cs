using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Visual popup container reused by PopupService.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _messageLabel;
        [SerializeField] private Transform _buttonRoot;
        [SerializeField] private PopupButtonView _buttonPrefab;

        private readonly List<PopupButtonView> _activeButtons = new();
        private readonly List<PopupButtonView> _pooledButtons = new();

        private Action<PopupResult> _onSelection;
        private LocalizedString _titleSource;
        private LocalizedString _messageSource;
        private LocalizedString.ChangeHandler _titleHandler;
        private LocalizedString.ChangeHandler _messageHandler;

        public void Show(PopupRequest request, Action<PopupResult> onSelection)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _onSelection = onSelection;

            BindTitle(request.Title);
            BindMessage(request.Message);
            BuildButtons(request.Options);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);
        }

        public void Hide(Action onHidden)
        {
            UnbindTitle();
            UnbindMessage();
            ClearButtons();
            _onSelection = null;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            onHidden?.Invoke();
        }

        private void BindTitle(LocalizedString title)
        {
            UnbindTitle();

            _titleSource = title;

            if (_titleLabel == null) return;

            if (title == null)
            {
                _titleLabel.gameObject.SetActive(false);
                _titleLabel.text = string.Empty;
                return;
            }

            _titleLabel.gameObject.SetActive(true);

            _titleHandler = value =>
            {
                if (_titleLabel != null)
                {
                    _titleLabel.text = value;
                }
            };
            title.StringChanged += _titleHandler;
            title.RefreshString();
        }

        private void UnbindTitle()
        {
            if (_titleSource != null && _titleHandler != null)
            {
                _titleSource.StringChanged -= _titleHandler;
            }

            _titleSource = null;
            _titleHandler = null;

            if (_titleLabel != null)
            {
                _titleLabel.text = string.Empty;
                _titleLabel.gameObject.SetActive(true);
            }
        }

        private void BindMessage(LocalizedString message)
        {
            UnbindMessage();

            _messageSource = message;

            if (_messageLabel == null || message == null) return;

            _messageHandler = value =>
            {
                if (_messageLabel != null)
                {
                    _messageLabel.text = value;
                }
            };
            message.StringChanged += _messageHandler;
            message.RefreshString();
        }

        private void UnbindMessage()
        {
            if (_messageSource != null && _messageHandler != null)
            {
                _messageSource.StringChanged -= _messageHandler;
            }

            _messageSource = null;
            _messageHandler = null;

            if (_messageLabel != null)
            {
                _messageLabel.text = string.Empty;
            }
        }

        private void BuildButtons(IReadOnlyList<PopupOptionDefinition> options)
        {
            ClearButtons();

            if (_buttonPrefab == null || _buttonRoot == null || options == null) return;

            PopupButtonView defaultFocus = null;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var captured = option;
                var view = AcquireButton();
                view.transform.SetParent(_buttonRoot, false);
                view.Bind(option, () =>
                {
                    var handler = _onSelection;
                    handler?.Invoke(new PopupResult(captured.Id));
                });

                _activeButtons.Add(view);
                if (option.IsDefaultFocus)
                {
                    defaultFocus = view;
                }
            }

            if (defaultFocus != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(defaultFocus.gameObject);
            }
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _activeButtons.Count; i++)
            {
                var view = _activeButtons[i];
                view.Unbind();
                view.gameObject.SetActive(false);
                _pooledButtons.Add(view);
            }

            _activeButtons.Clear();
        }

        private PopupButtonView AcquireButton()
        {
            if (_pooledButtons.Count > 0)
            {
                var view = _pooledButtons[0];
                _pooledButtons.RemoveAt(0);
                view.gameObject.SetActive(true);
                return view;
            }

            return Instantiate(_buttonPrefab, _buttonRoot);
        }
    }
}


