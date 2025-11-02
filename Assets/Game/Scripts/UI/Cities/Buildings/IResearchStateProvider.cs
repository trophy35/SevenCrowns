namespace SevenCrowns.UI.Cities.Buildings
{
    /// <summary>
    /// UI-facing provider for research/completion state relevant to building dependencies.
    /// Implement in Core and discover from UI at runtime.
    /// </summary>
    public interface IResearchStateProvider
    {
        /// <summary>Returns true if the specified research id is completed.</summary>
        bool IsCompleted(string researchId);
    }
}

