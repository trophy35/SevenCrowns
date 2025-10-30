using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Abstraction for quitting the application to keep UI decoupled and testable.
    /// Defined here to guarantee availability within the Game.UI assembly.
    /// </summary>
    public interface IApplicationQuitter
    {
        /// <summary>
        /// Requests the application to quit.
        /// In Editor, implementations may stop play mode instead.
        /// </summary>
        void Quit();
    }

    /// <summary>
    /// Controls visibility of the Main Menu (Canvas root) and handles Cancel (ESC) behavior.
    /// - Shows the menu when ESC/Cancel is pressed.
    /// - Hides the menu when the Cancel button is clicked or ESC pressed again.
    /// - Exposes Show/Hide/Toggle for external wiring and unit testing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Main Menu Root")]
        [Tooltip("Root GameObject of the main menu (typically the Canvas or a top-level panel).")]
        [SerializeField] private GameObject _root;

        [Header("Buttons")]
        [Tooltip("UI Button that hides the menu (TextMeshPro Button uses standard Button component).")]
        [SerializeField] private Button _cancelButton;
        [Tooltip("UI Button that triggers a save request.")]
        [SerializeField] private Button _saveButton;
        [Tooltip("UI Button that triggers a load request.")]
        [SerializeField] private Button _loadButton;
        [Tooltip("UI Button that quits the application.")]
        [SerializeField] private Button _quitButton;

        [Header("Events")]
        [Tooltip("Invoked when the Save button is clicked. Wire a Core save service here.")]
        [SerializeField] private UnityEngine.Events.UnityEvent _onSaveRequested;
        [Tooltip("Invoked when the Load button is clicked. Wire a Core load service here.")]
        [SerializeField] private UnityEngine.Events.UnityEvent _onLoadRequested;

        [Header("Behavior")]
        [SerializeField] private bool _startHidden = true;
        [Tooltip("Listen for ESC/Cancel in Update to toggle visibility.")]
        [SerializeField] private bool _listenForCancel = true;

        [Header("Quit Wiring")]
        [Tooltip("Optional behaviour implementing IApplicationQuitter. If null, a default quitter using Application.Quit is used.")]
        [SerializeField] private MonoBehaviour _quitterBehaviour;

        private bool _isVisible;
        private bool _wired;
        private bool _quitWired;
        private IApplicationQuitter _quitter;

        [Header("Localization")]
        [Tooltip("String table name holding UI common strings (e.g., 'UI.Common').")]
        [SerializeField] private string _uiStringTable = "UI.Common";
        [Tooltip("Localization entry key for the Cancel button label.")]
        [SerializeField] private string _cancelEntry = "Popup.Cancel";
        [Tooltip("Localization entry key for the Save button label.")]
        [SerializeField] private string _saveEntry = "MainMenu.Save";
        [Tooltip("Localization entry key for the Load button label.")]
        [SerializeField] private string _loadEntry = "MainMenu.Load";
        [Tooltip("Localization entry key for the Quit button label.")]
        [SerializeField] private string _quitEntry = "MainMenu.Quit";

        [Header("Labels (optional overrides)")]
        [SerializeField] private TextMeshProUGUI _cancelLabel;
        [SerializeField] private TextMeshProUGUI _saveLabel;
        [SerializeField] private TextMeshProUGUI _loadLabel;
        [SerializeField] private TextMeshProUGUI _quitLabel;

        private LocalizedString _cancelLocalized;
        private LocalizedString _saveLocalized;
        private LocalizedString _loadLocalized;
        private LocalizedString _quitLocalized;

        private LocalizedString.ChangeHandler _cancelHandler;
        private LocalizedString.ChangeHandler _saveHandler;
        private LocalizedString.ChangeHandler _loadHandler;
        private LocalizedString.ChangeHandler _quitHandler;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject; // fallback: control self if root not assigned
            }

            SetVisible(!_startHidden);
            EnsureWired();
        }

        private void OnEnable()
        {
            EnsureWired();
            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(OnSaveClicked);
            }
            if (_loadButton != null)
            {
                _loadButton.onClick.AddListener(OnLoadClicked);
            }
            if (_quitButton != null && !_quitWired)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
                _quitWired = true;
            }

            // Bind localized labels
            BindLocalizedButton(_cancelButton, ref _cancelLabel, ref _cancelLocalized, ref _cancelHandler, _cancelEntry);
            BindLocalizedButton(_saveButton, ref _saveLabel, ref _saveLocalized, ref _saveHandler, _saveEntry);
            BindLocalizedButton(_loadButton, ref _loadLabel, ref _loadLocalized, ref _loadHandler, _loadEntry);
            BindLocalizedButton(_quitButton, ref _quitLabel, ref _quitLocalized, ref _quitHandler, _quitEntry);
        }

        private void OnDisable()
        {
            if (_wired && _cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(Hide);
                _wired = false;
            }
            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(OnSaveClicked);
            }
            if (_loadButton != null)
            {
                _loadButton.onClick.RemoveListener(OnLoadClicked);
            }
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
                _quitWired = false;
            }

            // Unbind localized labels
            UnbindLocalizedButton(ref _cancelLocalized, ref _cancelHandler, _cancelLabel);
            UnbindLocalizedButton(ref _saveLocalized, ref _saveHandler, _saveLabel);
            UnbindLocalizedButton(ref _loadLocalized, ref _loadHandler, _loadLabel);
            UnbindLocalizedButton(ref _quitLocalized, ref _quitHandler, _quitLabel);
        }

        private void Update()
        {
            if (!_listenForCancel)
                return;

#if ENABLE_INPUT_SYSTEM
            // New Input System: support generic Cancel (maps to ESC / B by default)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            bool pressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                           || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            if (pressed)
            {
                Toggle();
            }
#else
            // Legacy Input Manager fallback
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
            }
#endif
        }

        public void Show()
        {
            EnsureWired();
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void Toggle()
        {
            SetVisible(!_isVisible);
        }

        public void RequestSave()
        {
            OnSaveClicked();
        }

        public void RequestLoad()
        {
            OnLoadClicked();
        }

        public void RequestQuit()
        {
            OnQuitClicked();
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_root != null && _root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }

        private void EnsureWired()
        {
            if (!_wired && _cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Hide);
                _wired = true;
            }
            if (_quitButton != null && !_quitWired)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
                _quitWired = true;
            }

            if (_quitter == null)
            {
                if (_quitterBehaviour is IApplicationQuitter asQuitter)
                {
                    _quitter = asQuitter;
                }
                else
                {
                    _quitter = new DefaultApplicationQuitter();
                }
            }
        }

        private void OnSaveClicked()
        {
            _onSaveRequested?.Invoke();
            Hide();
        }

        private void OnLoadClicked()
        {
            _onLoadRequested?.Invoke();
            Hide();
        }

        private void OnQuitClicked()
        {
            Hide();
            _quitter?.Quit();
        }

        // Localization helpers
        private void BindLocalizedButton(Button button, ref TextMeshProUGUI labelField, ref LocalizedString localized, ref LocalizedString.ChangeHandler handler, string entry)
        {
            if (button == null)
                return;

            if (labelField == null)
            {
                labelField = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            var lbl = labelField;

            if (localized == null)
            {
                localized = new LocalizedString
                {
                    TableReference = string.IsNullOrEmpty(_uiStringTable) ? "UI.Common" : _uiStringTable,
                    TableEntryReference = entry
                };
            }

            handler = value =>
            {
                if (lbl != null)
                {
                    lbl.text = value ?? string.Empty;
                }
            };
            localized.StringChanged += handler;
            localized.RefreshString();
        }

        private void UnbindLocalizedButton(ref LocalizedString localized, ref LocalizedString.ChangeHandler handler, TextMeshProUGUI label)
        {
            if (localized != null && handler != null)
            {
                localized.StringChanged -= handler;
            }
            if (label != null)
            {
                label.text = string.Empty;
            }
            localized = null;
            handler = null;
        }

        // Default quitter implementation
        private sealed class DefaultApplicationQuitter : IApplicationQuitter
        {
            public void Quit()
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}