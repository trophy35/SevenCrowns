using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Tracks the current hero and exposes its portrait Addressables key to UI.
    /// Configure id???portraitKey pairs in the inspector. Other systems set the current hero by id.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrentHeroService : MonoBehaviour, ICurrentHeroPortraitKeyProvider
    {
        [Serializable]
        private struct HeroPortraitEntry
        {
            public string heroId;       // stable ID, e.g., "hero.knight01"
            public string portraitKey;  // Addressables Sprite key, e.g., "UI/Heroes/Knight01[Knight01_0]"
        }

        [Header("Config")]
        [Tooltip("Known hero IDs mapped to portrait Addressables keys (Sprite sub-asset recommended).")]
        [SerializeField] private List<HeroPortraitEntry> _entries = new();

        [Tooltip("Optional default hero id on scene start.")]
        [SerializeField] private string _defaultHeroId;

        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        private string _currentId;
        private string _currentKey;

        public string CurrentHeroId => _currentId;
        public string CurrentPortraitKey => _currentKey;
        public event Action<string, string> CurrentHeroChanged;
        public IReadOnlyCollection<string> KnownHeroIds => _map.Keys;
        public bool IsKnownHeroId(string heroId) => !string.IsNullOrWhiteSpace(heroId) && _map.ContainsKey(heroId);


        private void Awake()
        {
            _map.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (!string.IsNullOrWhiteSpace(e.heroId) && !string.IsNullOrWhiteSpace(e.portraitKey) && !_map.ContainsKey(e.heroId))
                {
                    _map.Add(e.heroId, e.portraitKey);
                }
            }

            if (!string.IsNullOrEmpty(_defaultHeroId))
            {
                SetCurrentHeroById(_defaultHeroId);
            }
        }

        /// <summary>Sets the current hero by ID (looks up portrait key from configured map).</summary>
        public void SetCurrentHeroById(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return;
            if (_map.TryGetValue(heroId, out var key))
            {
                SetCurrentHeroDirect(heroId, key);
            }
            else
            {
                // Unknown hero id; clear portrait key but still update current id
                SetCurrentHeroDirect(heroId, null);
            }
        }

        /// <summary>Directly sets both id and portrait key (useful if computed externally).</summary>
        public void SetCurrentHeroDirect(string heroId, string portraitKey)
        {
            bool changed = !string.Equals(_currentId, heroId, StringComparison.Ordinal) ||
                           !string.Equals(_currentKey, portraitKey, StringComparison.Ordinal);

            _currentId = heroId;
            _currentKey = portraitKey;

            if (changed)
            {
                CurrentHeroChanged?.Invoke(_currentId, _currentKey);
            }
        }
    }
}

