using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Save;

namespace SevenCrowns.Tests.EditMode.Systems.Save
{
    public sealed class WorldMapCameraSelectionFogTests
    {
        private sealed class DummyCameraProvider : MonoBehaviour, SevenCrowns.Map.ICameraSnapshotProvider
        {
            public Vector3 pos;
            public float size;
            public Vector3 GetCameraPosition() => pos;
            public float GetCameraOrthographicSize() => size;
            public void ApplyCameraState(Vector3 position, float orthographicSize)
            {
                pos = position;
                size = orthographicSize;
            }
        }

        private sealed class DummyFogProvider : MonoBehaviour, SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider
        {
            public int w;
            public int h;
            public byte[] data;
            (int width, int height, byte[] states) SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider.Capture()
            {
                return (w, h, data);
            }
            void SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider.Apply(int width, int height, byte[] states)
            {
                w = width; h = height; data = states;
            }
        }

        [Test]
        public void CaptureAndApply_CameraAndSelectionAndFog_Works()
        {
            var go = new GameObject("DummyCam");
            var cam = go.AddComponent<DummyCameraProvider>();
            cam.pos = new Vector3(10, 20, -5);
            cam.size = 7f;

            var fogGo = new GameObject("DummyFog");
            var fog = fogGo.AddComponent<DummyFogProvider>();
            fog.w = 2; fog.h = 2; fog.data = new byte[] { 1, 2, 0, 1 };

            var curGo = new GameObject("CurrentHeroService");
            var current = curGo.AddComponent<SevenCrowns.Systems.CurrentHeroService>();
            // prime known hero ids via direct call
            current.SetCurrentHeroDirect("hero.alpha", null, 3);
            current.SetCurrentHeroById("hero.alpha");

            var reader = new WorldMapStateReader();
            var snap = reader.Capture();

            // mutate world away from snapshot
            cam.ApplyCameraState(new Vector3(0, 0, -5), 3f);
            current.SetCurrentHeroById("hero.beta");
            ((SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider)fog).Apply(1, 1, new byte[] { 0 });

            // apply
            reader.Apply(snap);

            // camera restored
            Assert.That(cam.GetCameraPosition().x, Is.EqualTo(10));
            Assert.That(cam.GetCameraOrthographicSize(), Is.EqualTo(7f));
            // selection restored
            Assert.That(current.CurrentHeroId, Is.EqualTo("hero.alpha"));
            // fog restored
            var tuple = ((SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider)fog).Capture();
            Assert.That(tuple.width, Is.EqualTo(2));
            Assert.That(tuple.height, Is.EqualTo(2));
            Assert.That(tuple.states, Is.EquivalentTo(new byte[] { 1, 2, 0, 1 }));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(fogGo);
            Object.DestroyImmediate(curGo);
        }
    }
}

