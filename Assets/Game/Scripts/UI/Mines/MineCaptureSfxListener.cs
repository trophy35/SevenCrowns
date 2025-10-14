using System.Collections;
using UnityEngine;
using SevenCrowns.Map.Mines;

namespace SevenCrowns.UI
{
    /// <summary>
    /// Plays SFX when a mine is captured, using the common UI asset provider pattern.
    /// Place one in the world UI; it auto-discovers mines and IUiAssetProvider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MineCaptureSfxListener : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField, Tooltip("Optional explicit asset provider. When null, auto-discovers a PreloadRegistryAssetProvider.")]
        private MonoBehaviour _assetProviderBehaviour; // IUiAssetProvider
        [SerializeField, Tooltip("Addressables key for the mine capture SFX.")]
        private string _sfxKey = "Audio/SFX/World/Collect-mine";
        [SerializeField, Range(0f,1f), Tooltip("Playback volume for the capture SFX.")]
        private float _volume = 1f;

        private IUiAssetProvider _provider;
        private AudioSource _audio;
        private AudioClip _clip;
        private Coroutine _warmup;

        private void OnEnable()
        {
            EnsureAudioSource();
            ResolveProvider();
            SubscribeMines();
            StartWarmup();
        }

        private void OnDisable()
        {
            UnsubscribeMines();
            if (_warmup != null)
            {
                StopCoroutine(_warmup);
                _warmup = null;
            }
        }

        private void EnsureAudioSource()
        {
            if (_audio == null)
            {
                _audio = GetComponent<AudioSource>();
                if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
                _audio.loop = false;
                _audio.volume = Mathf.Clamp01(_volume);
            }
        }

        private void ResolveProvider()
        {
            if (_provider != null) return;
            if (_assetProviderBehaviour != null && _assetProviderBehaviour is IUiAssetProvider p)
            {
                _provider = p;
                return;
            }
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length && _provider == null; i++)
            {
                if (behaviours[i] is IUiAssetProvider ap) _provider = ap;
            }
        }

        private void SubscribeMines()
        {
            var mines = FindObjectsOfType<MineAuthoring>(true);
            for (int i = 0; i < mines.Length; i++)
            {
                var m = mines[i];
                m.Claimed -= OnMineClaimed;
                m.Claimed += OnMineClaimed;
            }
        }

        private void UnsubscribeMines()
        {
            var mines = FindObjectsOfType<MineAuthoring>(true);
            for (int i = 0; i < mines.Length; i++)
            {
                var m = mines[i];
                m.Claimed -= OnMineClaimed;
            }
        }

        private void StartWarmup()
        {
            if (_warmup != null || string.IsNullOrWhiteSpace(_sfxKey)) return;
            _warmup = StartCoroutine(Warmup());
        }

        private IEnumerator Warmup()
        {
            const int maxFrames = 60;
            int frames = 0;
            while (frames++ < maxFrames)
            {
                if (_provider == null) ResolveProvider();
                if (_provider != null && _provider.TryGetAudioClip(_sfxKey, out var clip) && clip != null)
                {
                    _clip = clip;
                    break;
                }
                yield return null;
            }
            _warmup = null;
        }

        private void OnMineClaimed(MineAuthoring mine)
        {
            if (_audio == null) EnsureAudioSource();
            if (_clip == null && _provider != null)
            {
                _provider.TryGetAudioClip(_sfxKey, out _clip);
            }
            if (_clip != null)
            {
                _audio.PlayOneShot(_clip, Mathf.Clamp01(_volume));
            }
        }
    }
}

