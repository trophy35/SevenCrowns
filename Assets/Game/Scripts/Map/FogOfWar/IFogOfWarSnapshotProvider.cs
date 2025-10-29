namespace SevenCrowns.Map.FogOfWar
{
    /// <summary>
    /// Exposes fog-of-war internal state for save/load.
    /// Implement in FogOfWarService.
    /// </summary>
    public interface IFogOfWarSnapshotProvider
    {
        (int width, int height, byte[] states) Capture();
        void Apply(int width, int height, byte[] states);
    }
}

