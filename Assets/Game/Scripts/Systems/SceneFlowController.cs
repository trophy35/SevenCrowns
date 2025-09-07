using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SevenCrowns.SceneFlow
{
    /// <summary>
    /// Central scene flow controller that performs fade transitions around scene loads.
    /// Place one instance in the Boot scene or call Ensure() and Configure() at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneFlowController : MonoBehaviour
    {
        public static SceneFlowController Instance { get; private set; }

        [Header("Config & Prefab")]
        [SerializeField] private SceneFlowConfig _config;
        [SerializeField] private SceneTransitionFader _faderPrefab;

        private SceneTransitionFader _faderInstance;
        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void EnsureFader()
        {
            if (_faderInstance != null) return;
            if (_faderPrefab == null)
            {
                Debug.LogWarning("SceneFlowController: No fader prefab assigned; transitions will not fade.");
                return;
            }
            _faderInstance = Instantiate(_faderPrefab);
            DontDestroyOnLoad(_faderInstance.gameObject);
            if (_config != null)
                _faderInstance.SetSortOrder(_config.canvasSortOrder);
        }

        public static void Configure(SceneFlowConfig config, SceneTransitionFader prefab)
        {
            Ensure();
            Instance._config = config;
            Instance._faderPrefab = prefab;
        }

        public static void Ensure()
        {
            if (Instance != null) return;
            var go = new GameObject("SceneFlowController");
            Instance = go.AddComponent<SceneFlowController>();
            DontDestroyOnLoad(go);
        }

        public static void GoToBySceneName(string sceneName)
        {
            Ensure();
            Instance.StartCoroutine(Instance.GoToBySceneNameRoutine(sceneName));
        }

        public IEnumerator GoToBySceneNameRoutine(string sceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("SceneFlowController: Transition already in progress; ignoring request.");
                yield break;
            }

            _isTransitioning = true;
            EnsureFader();

            var cfg = _config;
            if (_faderInstance != null && cfg != null)
            {
                yield return _faderInstance.FadeOut(cfg.fadeOutSeconds, cfg.fadeCurve, cfg.fadeColor, cfg.blockRaycastsDuringFade);
            }

            // Load the scene
            if (!string.IsNullOrEmpty(sceneName))
            {
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (op != null)
                {
                    op.allowSceneActivation = true;
                    while (!op.isDone)
                        yield return null;
                }
                else
                {
                    SceneManager.LoadScene(sceneName);
                }
            }

            if (_faderInstance != null && cfg != null)
            {
                yield return _faderInstance.FadeIn(cfg.fadeInSeconds, cfg.fadeCurve, cfg.fadeColor, cfg.blockRaycastsDuringFade);
            }

            _isTransitioning = false;
        }
    }
}

