using UnityEngine;
using SevenCrowns.Systems.Save;
using SevenCrowns.Map;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Applies a pending WorldMap snapshot when returning from the City scene.
    /// Place one instance in the WorldMap scene on a Core object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapReturnInitializer : MonoBehaviour
    {
        [SerializeField] private bool _debugLogs;

        private void Start()
        {
            if (WorldMapReturnTransfer.TryConsume(out var data) && data != null && data.Length > 0)
            {
                try
                {
                    var snapshot = JsonWorldMapSerializer.Deserialize(data);
                    var reader = new WorldMapStateReader();
                    using (WorldMapRestoreScope.Enter())
                    {
                        reader.Apply(snapshot);
                    }
                    if (_debugLogs)
                        Debug.Log("[WorldMapReturn] Applied pending world snapshot.", this);
                }
                catch
                {
                    if (_debugLogs)
                        Debug.LogWarning("[WorldMapReturn] Failed to apply pending world snapshot.", this);
                }
            }
            else if (_debugLogs)
            {
                Debug.Log("[WorldMapReturn] No pending snapshot to apply.", this);
            }
        }
    }
}
