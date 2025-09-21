using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SevenCrowns.SceneFlow;
using SevenCrowns.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Minimal boot loader controller that runs preload tasks, updates a UI Image fill as a progress bar,
    /// displays a status text, waits for any key/click, optionally plays a preloaded SFX, then loads the next scene.
    /// This version avoids dependencies on project-specific types so it can be used early in setup.
    /// </summary>
    [DisallowMultipleComponent]
    public class BootLoaderController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UiProgressBar _progressBar; // Reusable progress bar component
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _pressAnyKeyRoot;
        [SerializeField, Tooltip("TMP label inside PressAnyKey root (optional; auto-found if null)")]
        private TextMeshProUGUI _pressAnyKeyText;

        [Header("Flow")]
        [SerializeField] private string _nextSceneName = "MainMenu";
        [SerializeField, Min(0f)] private float _minBootScreenSeconds = 0.6f;

        [Header("Preload Tasks (ScriptableObjects)")]
        [SerializeField] private List<BasePreloadTask> _tasks = new();

        [Header("SFX")]
        [SerializeField, Tooltip("Addressables key for the SFX played on any key press (optional)")]
        private string _pressAnyKeySfxKey = "SFX/click";
        [SerializeField, Range(0f,1f), Tooltip("Volume scale for the SFX (0..1)")]
        private float _pressAnyKeyVolume = 1f;

        private float _elapsed;
        private AudioSource _audio;

        /// <summary>
        /// Initializes the component, setting up UI elements and ensuring an AudioSource exists.
        /// </summary>
        private void Awake()
        {
            // Initially hide the "press any key" prompt
            if (_pressAnyKeyRoot != null) _pressAnyKeyRoot.SetActive(false);
            // Set the progress bar to 0 immediately
            if (_progressBar != null) _progressBar.SetImmediate(0f);
            // Set the initial status text
            if (_statusText != null) _statusText.text = "Loading...";

            // Ensure we have an AudioSource to play UI SFX
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false; // Disable Play On Awake to control playback manually
        }

        /// <summary>
        /// Starts the boot sequence coroutine.
        /// </summary>
        private void Start()
        {
            StartCoroutine(RunBootSequence());
        }

        /// <summary>
        /// Executes the boot sequence, running preload tasks, updating the progress bar,
        /// and loading the next scene after user input.
        /// </summary>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator RunBootSequence()
        {
            _elapsed = 0f;

            // Compute total weight (runtime weight takes precedence when available)
            float totalWeight = 0f;
            foreach (var t in _tasks)
            {
                if (t == null) continue;
                float w = (t is IRuntimeWeightedTask rt) ? rt.GetRuntimeWeight() : t.Weight;
                if (w <= 0f) w = 1f;
                totalWeight += w;
            }
            if (totalWeight <= 0f) totalWeight = 1f;

            float accumulated = 0f;

            foreach (var task in _tasks)
            {
                if (task == null) continue;

                if (_statusText != null) _statusText.text = task.DisplayName;

                float local = 0f;
                float taskWeight = (task is IRuntimeWeightedTask rt) ? rt.GetRuntimeWeight() : task.Weight;
                if (taskWeight <= 0f) taskWeight = 1f;

                IEnumerator routine = task.Run(p =>
                {
                    local = Mathf.Clamp01(p);
                    float current = (accumulated + local * taskWeight) / totalWeight;
                    Debug.Log($"-- {current}");
                    if (_progressBar != null) _progressBar.SetSmooth(current);
                });

                while (routine.MoveNext())
                {
                    _elapsed += Time.deltaTime;
                    yield return routine.Current;
                }

                // Task finished: add its full weight
                accumulated += taskWeight;
                Debug.Log($"- {accumulated / totalWeight}");
                if (_progressBar != null) _progressBar.SetSmooth(accumulated / totalWeight);
                yield return null;
            }

            // Clear status text first (it may share the same TMP as the bar's label)
            if (_statusText != null) _statusText.text = string.Empty;
            // Snap the bar to 100%
            if (_progressBar != null)
            {
                _progressBar.SetImmediate(1f);
                // Hide the numeric percentage so only the prompt remains visible
                _progressBar.UseNumericLabel(false);
                _progressBar.SetLabel(string.Empty);
            }

            // Enforce minimal display duration to avoid flash
            while (_elapsed < _minBootScreenSeconds)
            {
                _elapsed += Time.deltaTime;
                yield return null;
            }

            // Show the "press any key" prompt
            if (_pressAnyKeyRoot != null)
            {
                _pressAnyKeyRoot.SetActive(true);
                if (_pressAnyKeyText == null)
                    _pressAnyKeyText = _pressAnyKeyRoot.GetComponentInChildren<TextMeshProUGUI>(true);
#if UNITY_LOCALIZATION
                // Localize the prompt if Localization is available and tables are preloaded
                if (_pressAnyKeyText != null)
                {
                    var localized = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI.Common", "PressAnyKey");
                    if (!string.IsNullOrEmpty(localized))
                        _pressAnyKeyText.text = localized;
                }
#endif
            }

            // Wait for user input and play preloaded SFX if available
            yield return WaitForAnyKeyOrClickAndPlaySfx();

            // Load the next scene
            if (!string.IsNullOrEmpty(_nextSceneName))
            {
                SceneFlowController.GoToBySceneName(_nextSceneName);
            }
        }

        /// <summary>
        /// Waits for any key press or mouse click, and plays the "press any key" SFX if available.
        /// </summary>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator WaitForAnyKeyOrClickAndPlaySfx()
        {
            while (true)
            {
                // Keep the progress bar visually locked at 100% during the post-load wait
                if (_progressBar != null)
                    _progressBar.SetImmediate(1f);
#if ENABLE_INPUT_SYSTEM
                bool pressed =
                    (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                    (Mouse.current != null && (
                        Mouse.current.leftButton.wasPressedThisFrame ||
                        Mouse.current.rightButton.wasPressedThisFrame ||
                        Mouse.current.middleButton.wasPressedThisFrame)) ||
                    (Gamepad.current != null && (
                        Gamepad.current.buttonSouth.wasPressedThisFrame ||
                        Gamepad.current.buttonNorth.wasPressedThisFrame ||
                        Gamepad.current.buttonEast.wasPressedThisFrame ||
                        Gamepad.current.buttonWest.wasPressedThisFrame ||
                        Gamepad.current.startButton.wasPressedThisFrame ||
                        Gamepad.current.selectButton.wasPressedThisFrame ||
                        Gamepad.current.leftShoulder.wasPressedThisFrame ||
                        Gamepad.current.rightShoulder.wasPressedThisFrame ||
                        Gamepad.current.leftTrigger.wasPressedThisFrame ||
                        Gamepad.current.rightTrigger.wasPressedThisFrame ||
                        Gamepad.current.leftStickButton.wasPressedThisFrame ||
                        Gamepad.current.rightStickButton.wasPressedThisFrame ||
                        Gamepad.current.dpad.up.wasPressedThisFrame ||
                        Gamepad.current.dpad.down.wasPressedThisFrame ||
                        Gamepad.current.dpad.left.wasPressedThisFrame ||
                        Gamepad.current.dpad.right.wasPressedThisFrame)) ||
                    (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
                if (pressed)
                {
                    TryPlayPressAnyKeySfx();
                    yield return null;
                    yield break;
                }
#else
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    TryPlayPressAnyKeySfx();
                    yield return null;
                    yield break;
                }
#endif
                yield return null;
            }
        }

        /// <summary>
        /// Tries to play the "press any key" SFX, if a key is specified and the AudioSource is available.
        /// </summary>
        private void TryPlayPressAnyKeySfx()
        {
            if (string.IsNullOrEmpty(_pressAnyKeySfxKey) || _audio == null) return;
            if (PreloadRegistry.TryGet<AudioClip>(_pressAnyKeySfxKey, out var clip) && clip != null)
            {
                _audio.PlayOneShot(clip, Mathf.Clamp01(_pressAnyKeyVolume));
            }
        }
    }
}
