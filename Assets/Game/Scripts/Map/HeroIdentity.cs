using System;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Provides a stable hero ID and access to its HeroAgentComponent.
    /// Attach this to hero prefabs/instances (e.g., "Hero.Harry").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroIdentity : MonoBehaviour
    {
        [SerializeField] private string _heroId; // e.g., "hero.harry"
        [SerializeField] private HeroAgentComponent _agent;
        [SerializeField, Min(1)] private int _level = 1;

        public string HeroId => _heroId;
        public HeroAgentComponent Agent
        {
            get
            {
                if (_agent == null) _agent = GetComponent<HeroAgentComponent>();
                return _agent;
            }
        }

        public int Level => _level;
        public event Action<int> LevelChanged;

        public void SetLevel(int level)
        {
            int normalized = Mathf.Max(1, level);
            if (_level == normalized) return;
            _level = normalized;
            LevelChanged?.Invoke(_level);
        }

        private void OnValidate()
        {
            if (_agent == null) _agent = GetComponent<HeroAgentComponent>();
            _level = Mathf.Max(1, _level);
        }
    }
}

