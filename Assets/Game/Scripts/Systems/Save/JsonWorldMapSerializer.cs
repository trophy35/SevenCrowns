using System.Text;
using UnityEngine;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Simple JSON serialization using Unity's JsonUtility.
    /// </summary>
    public static class JsonWorldMapSerializer
    {
        public static byte[] Serialize(WorldMapSnapshot snapshot)
        {
            if (snapshot == null) snapshot = WorldMapSnapshot.CreateEmpty();
            string json = JsonUtility.ToJson(snapshot, prettyPrint: false);
            return Encoding.UTF8.GetBytes(json);
        }

        public static WorldMapSnapshot Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0) return WorldMapSnapshot.CreateEmpty();
            string json = Encoding.UTF8.GetString(data);
            var obj = JsonUtility.FromJson<WorldMapSnapshot>(json);
            return obj ?? WorldMapSnapshot.CreateEmpty();
        }
    }
}

