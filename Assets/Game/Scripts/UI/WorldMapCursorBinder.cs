using UnityEngine;
using SevenCrowns.Map;

namespace SevenCrowns.UI.CursorSystem
{
    /// <summary>
    /// Bridges world cursor hints from Map (IWorldCursorHintSource) to the UI CursorManager.
    /// Keeps dependencies one-way: UI -> Map, avoiding Map -> UI references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapCursorBinder : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private MonoBehaviour _hintSourceBehaviour; // Optional; must implement IWorldCursorHintSource

        [Header("Manager")]
        [SerializeField, Tooltip("Create a CursorManager if none exists (useful when starting world scene directly in Editor).")]
        private bool _ensureManagerIfMissing = true;
        [SerializeField, Tooltip("CursorSet to use when we create a manager (Editor world-scene play). Optional.")]
        private CursorSet _defaultSet;

        [Header("Cursor Set Override")]
        [SerializeField, Tooltip("If assigned and a manager exists (e.g., from Boot), re-initializes it with this set on enable.")]
        private CursorSet _overrideSet;

        [Header("Priorities")]
        [SerializeField] private int _hoverPriority = 100;
        [SerializeField] private int _movePriority = 10;

        private IWorldCursorHintSource _source;
        private const string TagHover = "map-hover-hero";
        private const string TagMove = "map-move-hint";

        [Header("Discovery")]
        [SerializeField, Tooltip("Auto-discover a hint source if none assigned.")]
        private bool _autoDiscover = true;
        [SerializeField, Tooltip("Seconds between discovery attempts when source is not yet available.")]
        private float _discoverInterval = 0.5f;
        private float _discoverTimer;

        private void OnEnable()
        {
            // Ensure a manager exists when playing a single scene in Editor
            if (_ensureManagerIfMissing && CursorManager.Instance == null)
            {
                var go = new GameObject("CursorManager");
                var mgr = go.AddComponent<CursorManager>();
                if (_defaultSet != null)
                    mgr.Initialize(_defaultSet);
            }

            // If a manager already exists (e.g., from Boot), optionally reconfigure its set
            if (_overrideSet != null && CursorManager.Instance != null)
            {
                CursorManager.Instance.Initialize(_overrideSet);
            }

            TryBindSource();
        }

        private void OnDisable()
        {
            if (_source != null)
            {
                _source.CursorHintsChanged -= OnHintsChanged;
                _source = null;
            }
            // Clear any overrides we may have set
            var cm = CursorManager.Instance;
            if (cm != null)
            {
                cm.Clear(TagHover);
                cm.Clear(TagMove);
            }
        }

        private void Update()
        {
            if (_source == null && _autoDiscover)
            {
                _discoverTimer += Time.unscaledDeltaTime;
                if (_discoverTimer >= Mathf.Max(0.1f, _discoverInterval))
                {
                    _discoverTimer = 0f;
                    TryBindSource();
                }
            }
        }

        private void TryBindSource()
        {
            if (_source != null) return;
            if (_hintSourceBehaviour != null && _hintSourceBehaviour is IWorldCursorHintSource s)
            {
                _source = s;
            }
            else
            {
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length && _source == null; i++)
                {
                    if (behaviours[i] is IWorldCursorHintSource s2)
                        _source = s2;
                }
            }
            if (_source != null)
            {
                _source.CursorHintsChanged += OnHintsChanged;
                OnHintsChanged(_source.HoveringHero, _source.MoveHint);
            }
        }

        private void OnHintsChanged(bool hoverHero, bool moveHint)
        {
            var cm = CursorManager.Instance;
            if (cm == null) return;

            if (hoverHero) cm.Set(CursorState.Hover, _hoverPriority, TagHover); else cm.Clear(TagHover);
            if (moveHint) cm.Set(CursorState.Move, _movePriority, TagMove); else cm.Clear(TagMove);
        }
    }
}
