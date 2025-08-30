using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SevenCrowns.Boot
{
    [DisallowMultipleComponent]
    public class BootLoaderController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private SevenCrowns.UI.UiProgressBar _progressBar;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _pressAnyKeyRoot;

        [Header("Flow")]
        [SerializeField] private string _nextSceneName = "MainMenu";
        [SerializeField] private bool _smoothProgress = true;
        [SerializeField, Min(0f), Tooltip("Minimum time the boot screen stays visible to avoid a flash effect.")]
        private float _minBootScreenSeconds = 0.6f;

        [Header("Preload Tasks (ScriptableObjects)")]
        [SerializeField, Tooltip("Ordered list of preload tasks. Runtime-weighted tasks override their serialized Weight.")]
        private List<BasePreloadTask> _tasks = new();

        [Header("SFX")]
        [SerializeField, Tooltip("Addressables key of the SFX played when the user presses a key")]
        private string _pressAnyKeySfxKey = "SFX/click";

        private float _elapsed;
        private AudioSource _audio;

        private void Awake()
        {
            if (_pressAnyKeyRoot != null) _pressAnyKeyRoot.SetActive(false);
            if (_progressBar != null) _progressBar.SetImmediate(0f);
            if (_statusText != null) _statusText.text = "Starting...";

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
                    UpdateProgress(current);
                });

                while (routine.MoveNext())
                {
                    _elapsed += Time.deltaTime;
                    yield return routine.Current;
                }

                // Task finished → add its full weight
                accumulated += taskWeight;
                UpdateProgress(accumulated / totalWeight);
                yield return null;
            }

            UpdateProgress(1f);
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

        private void UpdateProgress(float value01)
        {
            if (_progressBar == null) return;

            if (_smoothProgress) _progressBar.SetSmooth(value01);
            else _progressBar.SetImmediate(value01);
        }

        private IEnumerator WaitForAnyKeyOrClickAndPlaySfx()
        {
            while (true)
            {
#if ENABLE_INPUT_SYSTEM
                // New Input System: check keyboard or mouse press this frame
                bool pressed =
                    (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                    (Mouse.current != null && (
                        Mouse.current.leftButton.wasPressedThisFrame ||
                        Mouse.current.rightButton.wasPressedThisFrame ||
                        Mouse.current.middleButton.wasPressedThisFrame));

                if (pressed)
                {
                    if (PreloadRegistry.TryGet<AudioClip>(_pressAnyKeySfxKey, out var clip) && clip != null && _audio != null)
                    {
                        _audio.PlayOneShot(clip);
                    }
                    yield return null;
                    yield break;
                }
#else
        // Legacy Input Manager fallback
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            if (PreloadRegistry.TryGet<AudioClip>(_pressAnyKeySfxKey, out var clip) && clip != null && _audio != null)
            {
                _audio.PlayOneShot(clip);
            }
            yield return null;
            yield break;
        }
#endif
                yield return null;
            }
        }

    }
}
