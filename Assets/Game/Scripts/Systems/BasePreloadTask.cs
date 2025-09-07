// Assets/Game/Scripts/Systems/BasePreloadTask.cs
using System;
using System.Collections;
using UnityEngine;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Abstract base class for boot preload tasks, using a coroutine + progress callback.
    /// Tasks should yield while loading and call reportProgress(0..1) during their work.
    /// </summary>
    public abstract class BasePreloadTask : ScriptableObject
    {
        [SerializeField] private string _id = "task-id";
        [SerializeField] private string _displayName = "Loading...";
        [SerializeField] private int _order = 0;
        [SerializeField, Min(0.01f)] private float _weight = 1f;

        /// <summary>Stable identifier (used in logs/analytics).</summary>
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <summary>Localized name shown in boot UI.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? Id : _displayName;

        /// <summary>Order: lower runs first.</summary>
        public int Order => _order;

        /// <summary>Serialized weight used if IRuntimeWeightedTask is not implemented.</summary>
        public float Weight => Mathf.Max(0.01f, _weight);

        /// <summary>
        /// Run the task. Implementations should regularly call reportProgress(0..1).
        /// Ensure you call reportProgress(1f) before finishing.
        /// </summary>
        public abstract IEnumerator Run(Action<float> reportProgress);

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
            _weight = Mathf.Max(0.01f, _weight);
        }
#endif
    }
}
