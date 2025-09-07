using System.Collections.Generic;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Central registry for preloaded Addressables. Stores handles so assets remain alive in memory
    /// and can be retrieved later by key. Also provides bulk release when you want to free memory.
    /// </summary>
    public static class PreloadRegistry
    {
#if ADDRESSABLES
        /// <summary>
        /// Internal store for handles keyed by an identifier (usually the Addressables key string).
        /// </summary>
        private static readonly Dictionary<object, AsyncOperationHandle> _handlesByKey = new();

        /// <summary>
        /// Registers a handle with a given key. If the same key is registered again,
        /// the first valid handle is kept (to avoid duplicates).
        /// </summary>
        public static void Register(object key, AsyncOperationHandle handle)
        {
            if (key == null) return;

            if (_handlesByKey.TryGetValue(key, out var existing))
            {
                // If an existing valid handle is present, keep it.
                if (existing.IsValid())
                    return;

                _handlesByKey[key] = handle;
            }
            else
            {
                _handlesByKey.Add(key, handle);
            }
        }

        /// <summary>
        /// Try to get the loaded asset (typed) by key. Returns false if not found or not ready.
        /// </summary>
        public static bool TryGet<T>(object key, out T asset) where T : class
        {
            asset = null;
            if (key == null) return false;

            if (_handlesByKey.TryGetValue(key, out var handle) && handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                asset = handle.Result as T;
                return asset != null;
            }

            return false;
        }

        /// <summary>
        /// Releases a specific handle by key, if present.
        /// </summary>
        public static void Release(object key)
        {
            if (key == null) return;

            if (_handlesByKey.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                _handlesByKey.Remove(key);
            }
        }

        /// <summary>
        /// Releases all registered handles and clears the registry.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var kv in _handlesByKey)
            {
                var handle = kv.Value;
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _handlesByKey.Clear();
        }
#else
        // Addressables not enabled: keep API available as no-ops so the codebase compiles.
        public static void Register(object key, object handle) { }
        public static bool TryGet<T>(object key, out T asset) where T : class { asset = null; return false; }
        public static void Release(object key) { }
        public static void ReleaseAll() { }
#endif
    }
}
