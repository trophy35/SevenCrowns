using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.UI.CursorSystem
{
    /// <summary>
    /// Centralized cursor controller with simple priority-based overrides and optional animation support.
    /// Place one instance in a boot/persistent scene and assign a CursorSet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CursorManager : MonoBehaviour
    {
        [Serializable]
        private struct CursorOverride
        {
            public CursorState state;
            public int priority;
            public string tag;
        }

        public static CursorManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private CursorSet _set;

        private readonly List<CursorOverride> _overrides = new();
        private CursorState _currentState = CursorState.Default;
        private CursorSet.Entry _currentEntry;
        private int _frameIndex;
        private float _timer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyState(CursorState.Default, force: true);
        }

        private void Update()
        {
            // Animate if there are multiple frames
            if (_currentEntry == null || _currentEntry.frames == null) return;
            int frames = _currentEntry.frames.Length;
            if (frames <= 1 || _currentEntry.frameRate <= 0f) return;

            _timer += Time.unscaledDeltaTime;
            float frameTime = 1f / Mathf.Max(0.01f, _currentEntry.frameRate);
            while (_timer >= frameTime)
            {
                _timer -= frameTime;
                int next = (_frameIndex + 1) % frames;
                if (next != _frameIndex)
                {
                    _frameIndex = next;
                    ApplyCurrentFrame();
                }
            }
        }

        /// <summary>Initialize/replace the active cursor set at runtime.</summary>
        public void Initialize(CursorSet set)
        {
            _set = set;
            _overrides.Clear();
            ApplyState(CursorState.Default, force: true);
        }

        public CursorState Current => _currentState;

        /// <summary>Push or update a cursor override with a priority and optional tag.</summary>
        public void Set(CursorState state, int priority = 0, string tag = null)
        {
            // Update if tag exists, else add
            if (!string.IsNullOrEmpty(tag))
            {
                for (int i = 0; i < _overrides.Count; i++)
                {
                    if (_overrides[i].tag == tag)
                    {
                        _overrides[i] = new CursorOverride { state = state, priority = priority, tag = tag };
                        Recompute();
                        return;
                    }
                }
            }
            _overrides.Add(new CursorOverride { state = state, priority = priority, tag = tag });
            Recompute();
        }

        /// <summary>Clear a tagged override.</summary>
        public void Clear(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            for (int i = _overrides.Count - 1; i >= 0; i--)
            {
                if (_overrides[i].tag == tag)
                    _overrides.RemoveAt(i);
            }
            Recompute();
        }

        /// <summary>Clear all overrides and revert to Default.</summary>
        public void ClearAll()
        {
            _overrides.Clear();
            ApplyState(CursorState.Default);
        }

        private void Recompute()
        {
            if (_overrides.Count == 0)
            {
                ApplyState(CursorState.Default);
                return;
            }
            // Highest priority wins; in a tie, last-in wins
            int bestIndex = 0;
            int bestPriority = _overrides[0].priority;
            for (int i = 1; i < _overrides.Count; i++)
            {
                var p = _overrides[i].priority;
                if (p > bestPriority || (p == bestPriority && i == _overrides.Count - 1))
                    bestIndex = i;
                if (p >= bestPriority) bestPriority = p;
            }
            ApplyState(_overrides[bestIndex].state);
        }

        private void ApplyState(CursorState state, bool force = false)
        {
            if (!force && _currentState == state) return;
            _currentState = state;

            if (_set == null || !_set.TryGet(state, out var entry))
            {
                // Fallback to Default if available
                if (_set == null || !_set.TryGet(CursorState.Default, out entry))
                {
                    return; // nothing we can do
                }
            }

            _currentEntry = entry;
            _frameIndex = 0;
            _timer = 0f;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            if (_currentEntry == null || _currentEntry.frames == null || _currentEntry.frames.Length == 0)
                return;

            var tex = _currentEntry.frames[Mathf.Clamp(_frameIndex, 0, _currentEntry.frames.Length - 1)];
            if (tex == null) return;

            // Unity expects hotspot from top-left in pixels; convert Vector2 directly
            Vector2 hs = _currentEntry.hotspot;
            Cursor.SetCursor(tex, hs, _currentEntry.mode);
        }
    }
}

