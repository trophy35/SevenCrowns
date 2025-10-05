using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Binds a localized label and click handler for a popup option button.
    /// </summary>
    public sealed class PopupButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;

        private LocalizedString _localized;
        private LocalizedString.ChangeHandler _handler;

        public void Bind(PopupOptionDefinition option, Action onClicked)
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_label == null) _label = GetComponentInChildren<TextMeshProUGUI>();

            Unbind();

            _localized = option.Label;
            if (_localized != null)
            {
                _handler = value =>
                {
                    if (_label != null)
                    {
                        _label.text = value;
                    }
                };
                _localized.StringChanged += _handler;
                _localized.RefreshString();
            }

            if (_button != null && onClicked != null)
            {
                _button.onClick.AddListener(() => onClicked());
            }
        }

        public void Unbind()
        {
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
            }

            if (_localized != null && _handler != null)
            {
                _localized.StringChanged -= _handler;
            }

            _localized = null;
            _handler = null;

            if (_label != null)
            {
                _label.text = string.Empty;
            }
        }
    }
}
