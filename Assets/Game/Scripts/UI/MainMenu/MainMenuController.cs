using UnityEngine;
using UnityEngine.UI;

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
            if (_quitButton != null) {
                _quitButton.onClick.AddListener(OnQuitClicked);
                _quitWired = true;
            }
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
            if (_quitButton != null) {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
                _quitWired = false;
            }
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

        /// <summary>
        /// Shows the main menu.
        /// </summary>
        public void Show()
        {
            EnsureWired();
            SetVisible(true);
        }

        /// <summary>
        /// Hides the main menu.
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// Toggles the main menu visibility.
        /// </summary>
        public void Toggle()
        {
            SetVisible(!_isVisible);
        }

        /// <summary>
        /// Public entry to trigger save from inspector or other scripts.
        /// </summary>
        public void RequestSave()
        {
            OnSaveClicked();
        }

        /// <summary>
        /// Public entry to trigger load from inspector or other scripts.
        /// </summary>
        public void RequestLoad()
        {
            OnLoadClicked();
        }

        /// <summary>
        /// Public entry to quit the application from inspector or other scripts.
        /// </summary>
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
            if (_quitButton != null && !_quitWired) { _quitButton.onClick.AddListener(OnQuitClicked); _quitWired = true; }
            
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
            // Close the menu after saving completes
            Hide();
        }

        private void OnLoadClicked()
        {
            _onLoadRequested?.Invoke();
            // Close the menu after loading completes
            Hide();
        }

        private void OnQuitClicked()
        {
            // Hide first to avoid flicker while quitting (harmless in Editor)
            Hide();
            _quitter?.Quit();
        }

        /// <summary>
        /// Default quitter implementation that calls Application.Quit, and stops play mode in Editor.
        /// </summary>
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