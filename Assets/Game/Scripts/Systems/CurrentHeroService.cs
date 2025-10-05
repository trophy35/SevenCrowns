using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.UI;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Tracks the current hero and exposes its portrait Addressables key to UI.
    /// Configure id-to-portraitKey pairs in the inspector. Other systems set the current hero by id.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrentHeroService : MonoBehaviour, ICurrentHeroPortraitKeyProvider
    {
        [Serializable]
        private struct HeroPortraitEntry
        {
            public string heroId;       // stable ID, e.g., "hero.knight01"
            public string portraitKey;  // Addressables Sprite key, e.g., "UI/Heroes/Knight01[Knight01_0]"
            [Min(1)] public int level;
        }

        private sealed class HeroRecord
        {
            public HeroRecord(string portraitKey, int level)
            {
                PortraitKey = portraitKey;
                Level = level;
            }

            public string PortraitKey;
            public int Level;
        }

        [Header("Config")]
        [Tooltip("Known hero IDs mapped to portrait Addressables keys (Sprite sub-asset recommended).")]
        [SerializeField] private List<HeroPortraitEntry> _entries = new();

        [Tooltip("Optional default hero id on scene start.")]
        [SerializeField] private string _defaultHeroId;

        private readonly Dictionary<string, HeroRecord> _map = new(StringComparer.Ordinal);
        private string _currentId;
        private string _currentKey;
        private int _currentLevel = 1;

        public string CurrentHeroId => _currentId;
        public string CurrentPortraitKey => _currentKey;
        public int CurrentLevel => _currentLevel;
        public event Action<string, string> CurrentHeroChanged;
        public event Action<string, int> CurrentHeroLevelChanged;
        public IReadOnlyCollection<string> KnownHeroIds => _map.Keys;
        public bool IsKnownHeroId(string heroId) => !string.IsNullOrWhiteSpace(heroId) && _map.ContainsKey(heroId);

        private void Awake()
        {
            _map.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (!string.IsNullOrWhiteSpace(entry.heroId) && !_map.ContainsKey(entry.heroId))
                {
                    _map.Add(entry.heroId, new HeroRecord(
                        string.IsNullOrWhiteSpace(entry.portraitKey) ? null : entry.portraitKey,
                        NormalizeLevel(entry.level)));
                }
            }

            if (!string.IsNullOrEmpty(_defaultHeroId))
            {
                SetCurrentHeroById(_defaultHeroId);
            }
        }

        /// <summary>Sets the current hero by ID (looks up portrait key and level from configured map).</summary>
        public void SetCurrentHeroById(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return;
            if (_map.TryGetValue(heroId, out var record))
            {
                ApplyCurrentHero(heroId, record.PortraitKey, record.Level);
            }
            else
            {
                // Unknown hero id; clear portrait key but still update current id
                ApplyCurrentHero(heroId, null, 1);
            }
        }

        /// <summary>Directly sets both id and portrait key (legacy signature for compatibility).</summary>
        public void SetCurrentHeroDirect(string heroId, string portraitKey)
        {
            int level = _currentLevel > 0 ? _currentLevel : 1;

            if (!string.IsNullOrWhiteSpace(heroId))
            {
                if (_map.TryGetValue(heroId, out var record))
                {
                    if (!string.IsNullOrEmpty(portraitKey))
                    {
                        record.PortraitKey = portraitKey;
                    }

                    portraitKey = string.IsNullOrEmpty(portraitKey) ? record.PortraitKey : portraitKey;
                    level = record.Level;
                }
                else
                {
                    _map.Add(heroId, new HeroRecord(portraitKey, level));
                }
            }

            ApplyCurrentHero(heroId, portraitKey, level);
        }

        /// <summary>Directly sets id, portrait key, and level (useful if computed externally).</summary>
        public void SetCurrentHeroDirect(string heroId, string portraitKey, int level)
        {
            level = NormalizeLevel(level);

            if (!string.IsNullOrWhiteSpace(heroId))
            {
                if (_map.TryGetValue(heroId, out var record))
                {
                    if (!string.IsNullOrEmpty(portraitKey))
                    {
                        record.PortraitKey = portraitKey;
                    }

                    record.Level = level;
                    portraitKey = string.IsNullOrEmpty(portraitKey) ? record.PortraitKey : portraitKey;
                }
                else
                {
                    _map.Add(heroId, new HeroRecord(portraitKey, level));
                }
            }

            ApplyCurrentHero(heroId, portraitKey, level);
        }

        public void SetHeroLevelById(string heroId, int level)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return;

            level = NormalizeLevel(level);

            if (_map.TryGetValue(heroId, out var record))
            {
                if (record.Level == level)
                {
                    if (string.Equals(_currentId, heroId, StringComparison.Ordinal))
                    {
                        UpdateCurrentLevel(level);
                    }
                    return;
                }

                record.Level = level;
            }
            else
            {
                _map.Add(heroId, new HeroRecord(null, level));
            }

            if (string.Equals(_currentId, heroId, StringComparison.Ordinal))
            {
                UpdateCurrentLevel(level);
            }
        }

        public void SetCurrentHeroLevel(int level)
        {
            level = NormalizeLevel(level);

            if (string.IsNullOrEmpty(_currentId))
            {
                UpdateCurrentLevel(level);
                return;
            }

            SetHeroLevelById(_currentId, level);
        }

        private void ApplyCurrentHero(string heroId, string portraitKey, int level)
        {
            bool idChanged = !string.Equals(_currentId, heroId, StringComparison.Ordinal);
            bool portraitChanged = !string.Equals(_currentKey, portraitKey, StringComparison.Ordinal);

            _currentId = heroId;
            _currentKey = portraitKey;

            if (idChanged || portraitChanged)
            {
                CurrentHeroChanged?.Invoke(_currentId, _currentKey);
            }

            UpdateCurrentLevel(level);
        }

        private void UpdateCurrentLevel(int level)
        {
            level = NormalizeLevel(level);
            if (_currentLevel == level) return;

            _currentLevel = level;
            CurrentHeroLevelChanged?.Invoke(_currentId, _currentLevel);
        }

        private static int NormalizeLevel(int level) => level < 1 ? 1 : level;
    }
}

