using SevenCrowns.Systems.Save;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Cross-scene transfer for a serialized WorldMap snapshot.
    /// CityEnterFlowService captures the snapshot before leaving; WorldMapReturnInitializer applies it after return.
    /// </summary>
    public static class WorldMapReturnTransfer
    {
        private static byte[] s_SnapshotBytes;

        public static void SetSnapshot(byte[] data)
        {
            s_SnapshotBytes = data;
        }

        public static bool TryConsume(out byte[] data)
        {
            data = s_SnapshotBytes;
            s_SnapshotBytes = null;
            return data != null && data.Length > 0;
        }
    }
}

