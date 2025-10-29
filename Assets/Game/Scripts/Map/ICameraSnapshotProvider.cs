using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Exposes camera state for save/load without coupling to specific controller internals.
    /// Implemented by MapCameraController.
    /// </summary>
    public interface ICameraSnapshotProvider
    {
        Vector3 GetCameraPosition();
        float GetCameraOrthographicSize();
        void ApplyCameraState(Vector3 position, float orthographicSize);
    }
}

