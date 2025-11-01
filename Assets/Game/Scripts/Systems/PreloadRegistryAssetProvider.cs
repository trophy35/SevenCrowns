using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Bridges UI asset requests to the PreloadRegistry so that Game.UI
    /// can remain independent of Game.Core (avoids cyclic references).
    /// Place this on any Scene object present during UI usage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreloadRegistryAssetProvider : MonoBehaviour, IUiAssetProvider
    {
        [Header("Sprite Conversion (Optional)")]
        [Tooltip("If the Addressables key resolves to a Texture2D, convert it to a Sprite at runtime. Prefer fixing the Addressables entry to point to a Sprite sub-object.")]
        [SerializeField] private bool _convertTextureToSpriteIfNeeded = false;
        [Tooltip("If the requested key is not found in PreloadRegistry, auto-load it via Addressables and cache the handle for subsequent retries.")]
        [SerializeField] private bool _autoLoadIfMissing = true;

        private readonly Dictionary<string, Sprite> _createdSprites = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
        private readonly HashSet<string> _pendingSpriteLoads = new HashSet<string>(System.StringComparer.Ordinal);
        private readonly HashSet<string> _pendingClipLoads = new HashSet<string>(System.StringComparer.Ordinal);

        public bool TryGetSprite(string key, out Sprite sprite)
        {
#if ADDRESSABLES
            if (PreloadRegistry.TryGet<Sprite>(key, out sprite))
                return true;

            // Diagnose common misconfig: key points to Texture2D (main asset) rather than Sprite sub-asset.
            if (PreloadRegistry.TryGetRaw(key, out var raw) && raw != null)
            {
                if (raw is Texture2D tex)
                {
                    if (_convertTextureToSpriteIfNeeded)
                    {
                        if (_createdSprites.TryGetValue(key, out var cached) && cached != null)
                        {
                            sprite = cached;
                            return true;
                        }

                        // Create a runtime sprite from the full texture. Pixels-per-unit set to 100 by convention.
                        var rect = new Rect(0, 0, tex.width, tex.height);
                        var pivot = new Vector2(0.5f, 0.5f);
                        sprite = Sprite.Create(tex, rect, pivot, 100f);
                        _createdSprites[key] = sprite;
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Addressables key '{key}' resolved to Texture2D. Enable 'Convert Texture To Sprite' on {nameof(PreloadRegistryAssetProvider)} or point the key to a Sprite sub-object.", this);
                    }
                }
                else
                {
                    var rawType = raw.GetType().Name;
                    Debug.LogWarning($"Addressables key '{key}' resolved to type '{rawType}', not Sprite. Ensure the key targets the Sprite sub-object.", this);
                }
            }
            else if (_autoLoadIfMissing && !_pendingSpriteLoads.Contains(key))
            {
                // Kick off an async Addressables load for the Sprite; a later retry will pick it up from the registry.
                _pendingSpriteLoads.Add(key);
                AsyncOperationHandle<Sprite> h = Addressables.LoadAssetAsync<Sprite>(key);
                PreloadRegistry.Register(key, h);
                Debug.Log($"[UI Assets] Auto-loading Sprite for key='{key}' via Addressables.", this);
            }
            return false;
#else
            sprite = null;
            return false;
#endif
        }

        public bool TryGetAudioClip(string key, out AudioClip clip)
        {
#if ADDRESSABLES
            if (PreloadRegistry.TryGet<AudioClip>(key, out clip))
                return true;
            if (_autoLoadIfMissing && !_pendingClipLoads.Contains(key))
            {
                _pendingClipLoads.Add(key);
                AsyncOperationHandle<AudioClip> h = Addressables.LoadAssetAsync<AudioClip>(key);
                PreloadRegistry.Register(key, h);
                Debug.Log($"[UI Assets] Auto-loading AudioClip for key='{key}' via Addressables.", this);
            }
            return false;
#else
            clip = null;
            return false;
#endif
        }

        private void OnDestroy()
        {
            // Clean up any runtime sprites we created.
            foreach (var kv in _createdSprites)
            {
                var s = kv.Value;
                if (s != null)
                    Destroy(s);
            }
            _createdSprites.Clear();
        }
    }
}
