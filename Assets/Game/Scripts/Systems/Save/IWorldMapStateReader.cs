namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Captures and applies a snapshot of the world map state by querying domain services.
    /// Implementation composes existing providers (heroes, nodes, wallet, etc.).
    /// </summary>
    public interface IWorldMapStateReader
    {
        WorldMapSnapshot Capture();
        void Apply(WorldMapSnapshot snapshot);
    }
}

