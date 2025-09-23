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

        public string HeroId => _heroId;
        public HeroAgentComponent Agent
        {
            get
            {
                if (_agent == null) _agent = GetComponent<HeroAgentComponent>();
                return _agent;
            }
        }

        private void OnValidate()
        {
            if (_agent == null) _agent = GetComponent<HeroAgentComponent>();
        }
    }
}

