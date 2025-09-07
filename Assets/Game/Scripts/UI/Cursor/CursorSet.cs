using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.UI.CursorSystem
{
    /// <summary>
    /// Authorable set of cursor entries, one per CursorState.
    /// Supports static or animated cursors (via frames + frameRate).
    /// </summary>
    [CreateAssetMenu(fileName = "CursorSet", menuName = "SevenCrowns/UI/Cursor Set")]
    public sealed class CursorSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public CursorState state = CursorState.Default;
            [Tooltip("One or more frames. First frame required; multiple frames enable animation.")]
            public Texture2D[] frames;
            [Min(0f), Tooltip("Frames per second for animation (0 = static cursor).")]
            public float frameRate = 0f;
            [Tooltip("Hotspot in pixels from top-left of the texture.")]
            public Vector2 hotspot = new Vector2(0, 0);
            [Tooltip("Cursor mode. Auto is recommended; ForceSoftware if needed on specific platforms.")]
            public CursorMode mode = CursorMode.Auto;
        }

        [SerializeField] private List<Entry> _entries = new();

        private readonly Dictionary<CursorState, Entry> _byState = new();

        public bool TryGet(CursorState state, out Entry entry)
        {
            if (_byState.Count == 0)
                BuildLookup();
            return _byState.TryGetValue(state, out entry);
        }

        private void BuildLookup()
        {
            _byState.Clear();
            if (_entries == null) return;
            foreach (var e in _entries)
            {
                if (e == null) continue;
                if (e.frames == null || e.frames.Length == 0 || e.frames[0] == null) continue;
                if (!_byState.ContainsKey(e.state))
                    _byState.Add(e.state, e);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp FPS but do not strip null frame slots — Unity needs placeholders while editing arrays.
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (e == null) continue;
                    e.frameRate = Mathf.Max(0f, e.frameRate);
                }
            }
            BuildLookup();
        }
#endif
    }
}
