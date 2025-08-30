using System.Collections;
using UnityEngine;

namespace SevenCrowns.Boot
{
    /// <summary>
    /// Basic ScriptableObject for a preloading task
    /// </summary>
    public abstract class BasePreloadTask : ScriptableObject
    {
        [SerializeField, Tooltip("Text displayed on the loading bar...")]
        private string _displayName = "Loading...";

        [SerializeField, Min(0f), Tooltip("Global task weight.")]
        private float _weight = 1f;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public float Weight => Mathf.Max(0f, _weight);

        /// <summary>
        /// Run the task et report a local progression [0..1].
        /// </summary>
        public abstract IEnumerator Run(System.Action<float> reportProgress);
    }
}
