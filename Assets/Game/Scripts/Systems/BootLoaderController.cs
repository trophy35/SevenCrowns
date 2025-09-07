using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        [SerializeField] private Image _progressFill; // Image with Fill Method set to Horizontal
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _pressAnyKeyRoot;

        [Header("Flow")]
        [SerializeField] private string _nextSceneName = "MainMenu";
        [SerializeField, Min(0f)] private float _minBootScreenSeconds = 0.6f;

        [Header("Preload Tasks (ScriptableObjects)")]
        [SerializeField] private List<BasePreloadTask> _tasks = new();

        [Header("SFX")]
        [SerializeField, Tooltip("Addressables key for the SFX played on any key press (optional)")]
        private string _pressAnyKeySfxKey = "SFX/click";

        private float _elapsed;
        private AudioSource _audio;

        private void Awake()
        {
            if (_pressAnyKeyRoot != null) _pressAnyKeyRoot.SetActive(false);
            SetProgressImmediate(0f);
            if (_statusText != null) _statusText.text = "Loading...";

            // Ensure we have an AudioSource to play UI SFX
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
        }

        private void Start()
        {
            StartCoroutine(RunBootSequence());
        }

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
                    SetProgressImmediate(current);
                });

                while (routine.MoveNext())
                {
                    _elapsed += Time.deltaTime;
                    yield return routine.Current;
                }

                // Task finished: add its full weight
                accumulated += taskWeight;
                SetProgressImmediate(accumulated / totalWeight);
                yield return null;
            }

            SetProgressImmediate(1f);
            if (_statusText != null) _statusText.gameObject.SetActive(false);

            // Enforce minimal display duration to avoid flash
            while (_elapsed < _minBootScreenSeconds)
            {
                _elapsed += Time.deltaTime;
                yield return null;
            }

            if (_pressAnyKeyRoot != null) _pressAnyKeyRoot.SetActive(true);

            // Wait for user input and play preloaded SFX if available
            yield return WaitForAnyKeyOrClickAndPlaySfx();

            if (!string.IsNullOrEmpty(_nextSceneName))
            {
                SceneManager.LoadScene(_nextSceneName);
            }
        }

        private void SetProgressImmediate(float value01)
        {
            if (_progressFill != null)
            {
                _progressFill.fillAmount = Mathf.Clamp01(value01);
            }
        }

        private IEnumerator WaitForAnyKeyOrClickAndPlaySfx()
        {
            while (true)
            {
#if ENABLE_INPUT_SYSTEM
                bool pressed =
                    (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                    (Mouse.current != null && (
                        Mouse.current.leftButton.wasPressedThisFrame ||
                        Mouse.current.rightButton.wasPressedThisFrame ||
                        Mouse.current.middleButton.wasPressedThisFrame));
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

        private void TryPlayPressAnyKeySfx()
        {
            if (string.IsNullOrEmpty(_pressAnyKeySfxKey) || _audio == null) return;
            if (PreloadRegistry.TryGet<AudioClip>(_pressAnyKeySfxKey, out var clip) && clip != null)
            {
                _audio.PlayOneShot(clip);
            }
        }
    }
}

