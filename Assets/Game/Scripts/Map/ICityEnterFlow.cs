using System;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Abstraction for initiating the City scene flow when a hero enters an owned city.
    /// Implement in Game.Core to drive SceneFlowController without creating a Map -> Core dependency.
    /// </summary>
    public interface ICityEnterFlow
    {
        /// <summary>
        /// Requests entering the City scene for the given city and hero.
        /// Implementations decide how to transition (fade, stacking, etc.).
        /// </summary>
        void EnterCity(string cityId, string heroId);
    }
}

