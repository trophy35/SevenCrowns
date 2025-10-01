namespace SevenCrowns.Map.FogOfWar
{
    /// <summary>
    /// Distinguishes cells that are unseen, explored, or currently visible.
    /// </summary>
    public enum FogOfWarState : byte
    {
        Unknown = 0,
        Explored = 1,
        Visible = 2,
    }
}
