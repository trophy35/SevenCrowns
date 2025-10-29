using UnityEngine;
using UnityEngine.UI;

namespace SevenCrowns.UI
{
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

        [Header("Behavior")]
        [SerializeField] private bool _startHidden = true;
        [Tooltip("Listen for ESC/Cancel in Update to toggle visibility.")]
        [SerializeField] private bool _listenForCancel = true;

        private bool _isVisible;
        private bool _wired;

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
        }

        private void OnDisable()
        {
            if (_wired && _cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(Hide);
                _wired = false;
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
        }
    }
}
