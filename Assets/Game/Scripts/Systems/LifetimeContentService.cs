// Assets/Game/Scripts/Systems/LifetimeContentService.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Centralized loader for Addressables grouped by "lifetime" labels (e.g., "boot", "frontend", "world", "combat", "shared").
    /// This service provides a way to manage the loading and unloading of assets and scenes using Addressables,
    /// grouping them by "lifetime" labels.  This helps ensure assets are loaded when needed and released when no longer required,
    /// preventing memory leaks and improving performance.
    ///
    /// Key Features:
    /// - **Grouping by Labels:** Assets are loaded and managed based on labels (e.g., "boot", "frontend", "world", "combat", "shared").
    /// - **Idempotency:** Loading a label multiple times only loads the assets once.
    /// - **Handles Tracking:**  Keeps track of all loaded assets and scenes for later release.
    /// - **Additive Scene Loading:** Supports loading scenes additively via Addressables.
    /// - **Centralized Release:** Provides methods to release assets and scenes, either by label or individually.
    /// - **Error Handling:** Includes error handling for load failures, throwing exceptions when necessary.
    ///
    /// Usage example:
    ///   await _lifetime.LoadLabelAsync("shared"); // Load all assets tagged with "shared"
    ///   await _lifetime.LoadLabelAsync("boot");   // Load all assets tagged with "boot"
    ///   _lifetime.ReleaseLabel("boot");          // Release all assets tagged with "boot"
    ///   await _lifetime.LoadLabelAsync("world");  // Load all assets tagged with "world"
    ///
    /// Methods:
    ///   - LoadLabelAsync: loads all assets tagged with a label and tracks handles, with idempotency.
    ///   - ReleaseLabel: releases everything loaded for that label.
    ///   - LoadAssetAsync / ReleaseAsset: ad-hoc asset control (also tracked).  For loading single assets by address.
    ///   - LoadSceneAsync / UnloadSceneAsync: additive scene streaming with tracking.
    ///   - ReleaseAll: safety valve on scene transitions / shutdown.  Releases all loaded assets and scenes.
    /// </summary>
    public sealed class LifetimeContentService
    {
        #region Fields

        // Tracks all handles loaded per label.  A handle represents an ongoing or completed asynchronous operation.
        private readonly Dictionary<string, List<AsyncOperationHandle>> _labelHandles = new(StringComparer.Ordinal);
        // Tracks ad-hoc assets by address.  Ad-hoc assets are loaded individually, not as part of a label.
        private readonly Dictionary<string, List<AsyncOperationHandle>> _addressHandles = new(StringComparer.Ordinal);
        // Tracks additive scenes by address (kept distinct for clarity). Stores the handle to the loaded scene.
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _sceneHandles = new(StringComparer.Ordinal);

        #endregion

        #region Events

        // Events that can be subscribed to for label loading and releasing.  Useful for triggering other actions.
        public event Action<string> OnLabelLoaded;
        public event Action<string> OnLabelReleased;

        #endregion

        #region Label Management

        /// <summary>Loads all Addressables for a label and keeps the handle(s) for later release. Idempotent per label.</summary>
        /// <param name="label">The Addressables label to load.</param>
        /// <param name="progress">An optional progress object to track loading progress.</param>
        /// <param name="ct">An optional cancellation token to cancel the loading operation.</param>
        public async Task LoadLabelAsync(string label, IProgress<float> progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label cannot be null or empty.", nameof(label));

            // If we've already loaded this label (and it still has live handles), just report done.
            if (_labelHandles.TryGetValue(label, out var existing) && existing.Count > 0)
            {
                progress?.Report(1f);
                OnLabelLoaded?.Invoke(label);
                return;
            }

            // Load all assets tagged with the label.
            //  Addressables.LoadAssetsAsync loads all assets associated with the given label.
            //  The callback is intentionally empty as we don't need per-asset callbacks here, the aggregate handle is sufficient.
            var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label,
                callback: _ => { /* we don't need per-asset callback here */ });

            // Progress relay: Reports progress back to the caller.
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested(); // Check if the operation has been cancelled.
                progress?.Report(handle.PercentComplete); // Report the current progress.
                await Task.Yield(); // Allow other tasks to run.
            }

            // Throw if failed: If the load operation failed, release the handle and throw an exception.
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                // Release failed handle to avoid leaks
                Addressables.Release(handle);
                throw new InvalidOperationException($"Addressables: LoadLabelAsync failed for label '{label}'.");
            }

            // Store the aggregate handle; Addressables returns a "group handle" that owns sub-handles
            //  This stores the handle in the _labelHandles dictionary so it can be released later.
            var list = _labelHandles.ContainsKey(label) ? _labelHandles[label] : (_labelHandles[label] = new List<AsyncOperationHandle>(1));
            list.Add(handle);

            progress?.Report(1f);
            OnLabelLoaded?.Invoke(label); // Invoke the OnLabelLoaded event.
        }

        /// <summary>Loads multiple labels sequentially (keeps error surface simple). If any fails, previously loaded labels stay loaded.</summary>
        /// <param name="labels">A collection of Addressables labels to load.</param>
        /// <param name="progress">An optional progress object to track loading progress for each label.</param>
        /// <param name="ct">An optional cancellation token to cancel the loading operation.</param>
        public async Task LoadLabelsAsync(IEnumerable<string> labels, IProgress<(string label, float p)> progress = null, CancellationToken ct = default)
        {
            foreach (var label in labels)
            {
                ct.ThrowIfCancellationRequested();
                var p = new Progress<float>(v => progress?.Report((label, v))); // Create a progress object for each label.
                await LoadLabelAsync(label, p, ct); // Load each label.
            }
        }

        /// <summary>Releases everything loaded under a label.</summary>
        /// <param name="label">The Addressables label to release.</param>
        public void ReleaseLabel(string label)
        {
            // If the label is not loaded, return.
            if (!_labelHandles.TryGetValue(label, out var list) || list.Count == 0)
                return;

            // Release all handles associated with the label.
            foreach (var h in list)
            {
                if (h.IsValid()) // Check if the handle is still valid before releasing.
                    Addressables.Release(h);
            }
            list.Clear(); // Clear the list of handles.

            OnLabelReleased?.Invoke(label); // Invoke the OnLabelReleased event.
        }

        /// <summary>True if we hold at least one valid handle for this label.</summary>
        /// <param name="label">The Addressables label to check.</param>
        /// <returns>True if the label is loaded, false otherwise.</returns>
        public bool IsLabelLoaded(string label)
        {
            if (!_labelHandles.TryGetValue(label, out var list) || list.Count == 0) return false;
            foreach (var h in list)
                if (h.IsValid()) return true; // If at least one handle is valid, the label is considered loaded.
            return false;
        }

        #endregion

        #region Asset Management

        /// <summary>Loads an individual asset by address (or key) and tracks it for ReleaseAsset/ReleaseAll.</summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="address">The Addressables address (or key) of the asset to load.</param>
        /// <param name="ct">An optional cancellation token to cancel the loading operation.</param>
        /// <returns>The loaded asset.</returns>
        public async Task<T> LoadAssetAsync<T>(string address, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address cannot be null or empty.", nameof(address));

            // Load the asset.
            var handle = Addressables.LoadAssetAsync<T>(address);
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested(); // Check for cancellation.
                await Task.Yield();
            }

            // Throw if failed.
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new InvalidOperationException($"Addressables: LoadAssetAsync failed for '{address}'.");
            }

            TrackAddressHandle(address, handle); // Track the handle for later release.
            return handle.Result!; // Return the loaded asset.
        }

        /// <summary>Releases all handles we hold for a specific address.</summary>
        /// <param name="address">The Addressables address of the asset to release.</param>
        public void ReleaseAsset(string address)
        {
            // If the asset is not loaded, return.
            if (!_addressHandles.TryGetValue(address, out var list) || list.Count == 0) return;
            // Release all handles associated with the address.
            foreach (var h in list)
            {
                if (h.IsValid())
                    Addressables.Release(h);
            }
            list.Clear(); // Clear the list of handles.
        }

        #endregion

        #region Scene Management

        /// <summary>Additive scene load via Addressables. Keeps the SceneInstance handle for unloading.</summary>
        /// <param name="address">The Addressables address of the scene to load.</param>
        /// <param name="mode">The scene loading mode (default is additive).</param>
        /// <param name="activateOnLoad">Whether to activate the scene on load (default is true).</param>
        /// <param name="priority">The priority of the scene load (default is 100).</param>
        /// <param name="ct">An optional cancellation token to cancel the loading operation.</param>
        /// <returns>The loaded SceneInstance.</returns>
        public async Task<SceneInstance> LoadSceneAsync(string address, LoadSceneMode mode = LoadSceneMode.Additive, bool activateOnLoad = true, int priority = 100, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address cannot be null or empty.", nameof(address));
            // If the scene is already loaded, return the existing SceneInstance.
            if (_sceneHandles.ContainsKey(address) && _sceneHandles[address].IsValid())
                return _sceneHandles[address].Result;

            // Load the scene.
            var handle = Addressables.LoadSceneAsync(address, mode, activateOnLoad, priority);
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested(); // Check for cancellation.
                await Task.Yield();
            }

            // Throw if failed.
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new InvalidOperationException($"Addressables: LoadSceneAsync failed for '{address}'.");
            }

            _sceneHandles[address] = handle; // Store the handle.
            return handle.Result; // Return the SceneInstance.
        }

        /// <summary>Unloads an additive scene that was loaded via LoadSceneAsync.</summary>
        /// <param name="address">The Addressables address of the scene to unload.</param>
        /// <param name="autoReleaseHandle">Whether to automatically release the handle (default is true).</param>
        /// <param name="ct">An optional cancellation token to cancel the unloading operation.</param>
        public async Task UnloadSceneAsync(string address, bool autoReleaseHandle = true, CancellationToken ct = default)
        {
            // If the scene is not loaded, return.
            if (!_sceneHandles.TryGetValue(address, out var handle) || !handle.IsValid())
                return;

            // Unload the scene.
            var unload = Addressables.UnloadSceneAsync(handle, autoReleaseHandle);
            while (!unload.IsDone)
            {
                ct.ThrowIfCancellationRequested(); // Check for cancellation.
                await Task.Yield();
            }

            _sceneHandles.Remove(address); // Remove the handle from the dictionary.
        }

        #endregion

        #region Release All

        /// <summary>Releases everything (labels, ad-hoc assets, scenes). Use when returning to main menu or quitting.</summary>
        /// <param name="ct">An optional cancellation token to cancel the release operation.</param>
        public async Task ReleaseAllAsync(CancellationToken ct = default)
        {
            // Scenes first (so scene objects don’t outlive their assets)
            //  Unload scenes before releasing assets to avoid orphaned objects.
            foreach (var kvp in new List<KeyValuePair<string, AsyncOperationHandle<SceneInstance>>>(_sceneHandles))
            {
                await UnloadSceneAsync(kvp.Key, autoReleaseHandle: true, ct);
            }
            _sceneHandles.Clear();

            // Release all label-loaded assets.
            foreach (var kvp in _labelHandles)
            {
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var h = list[i];
                    if (h.IsValid())
                        Addressables.Release(h);
                }
                list.Clear();
            }

            // Release all ad-hoc loaded assets.
            foreach (var kvp in _addressHandles)
            {
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var h = list[i];
                    if (h.IsValid())
                        Addressables.Release(h);
                }
                list.Clear();
            }

            // Yield once to let Addressables finalize releases.  Allows Addressables to complete the release operations.
            await Task.Yield();
        }

        #endregion

        #region Internals

        /// <summary>Tracks the handle of an ad-hoc loaded asset.</summary>
        /// <param name="address">The Addressables address of the asset.</param>
        /// <param name="handle">The AsyncOperationHandle of the loaded asset.</param>
        private void TrackAddressHandle(string address, AsyncOperationHandle handle)
        {
            // Store the handle in the _addressHandles dictionary.
            if (!_addressHandles.TryGetValue(address, out var list))
            {
                list = new List<AsyncOperationHandle>(1);
                _addressHandles[address] = list;
            }
            list.Add(handle);
        }

        #endregion
    }
}
