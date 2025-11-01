namespace SevenCrowns.Map
{
    /// <summary>
    /// Global guard used during world map snapshot application to suppress side-effect SFX/VFX.
    /// Use with a using block: using (WorldMapRestoreScope.Enter()) { Apply(...); }
    /// </summary>
    public static class WorldMapRestoreScope
    {
        private static int s_Depth;
        public static bool IsRestoring => s_Depth > 0;

        public static System.IDisposable Enter()
        {
            s_Depth++;
            return new Scope();
        }

        private sealed class Scope : System.IDisposable
        {
            private bool _disposed;
            public void Dispose()
            {
                if (_disposed) return;
                if (s_Depth > 0) s_Depth--;
                _disposed = true;
            }
        }
    }
}
