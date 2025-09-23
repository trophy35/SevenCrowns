using System;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Map-facing provider for the currently selected hero agent.
    /// Implement this in Core to decouple Map from Core.
    /// </summary>
    public interface ISelectedHeroAgentProvider
    {
        HeroAgentComponent CurrentHero { get; }
        event Action<HeroAgentComponent> SelectedHeroChanged;

        void SelectById(string heroId);
    }
}
