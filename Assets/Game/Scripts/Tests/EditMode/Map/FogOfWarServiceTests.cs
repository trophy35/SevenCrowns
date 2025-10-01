using NUnit.Framework;
using UnityEngine;

namespace SevenCrowns.Map.FogOfWar.Tests
{
    public sealed class FogOfWarServiceTests
    {
        [Test]
        public void RevealArea_MarksCenterAndNeighborsVisible()
        {
            var context = CreateContext(5, 5);
            try
            {
                var service = context.service;

                var center = new GridCoord(2, 2);
                service.RevealArea(center, 1);

                Assert.That(service.IsVisible(center), Is.True);
                Assert.That(service.IsVisible(new GridCoord(3, 2)), Is.True);
                Assert.That(service.IsExplored(new GridCoord(4, 4)), Is.False);
            }
            finally
            {
                Cleanup(context.root);
            }
        }

        [Test]
        public void RevealArea_UsesCircularRadius()
        {
            var context = CreateContext(7, 7);
            try
            {
                var service = context.service;
                var center = new GridCoord(3, 3);

                service.RevealArea(center, 1);

                Assert.That(service.IsVisible(center), Is.True);
                Assert.That(service.IsVisible(new GridCoord(4, 3)), Is.True); // cardinal neighbour
                Assert.That(service.IsVisible(new GridCoord(4, 4)), Is.False); // diagonal should remain hidden
            }
            finally
            {
                Cleanup(context.root);
            }
        }

        [Test]
        public void ClearTransientVisibility_DowngradesVisibleCellsToExplored()
        {
            var context = CreateContext(4, 4);
            try
            {
                var service = context.service;
                var center = new GridCoord(1, 1);

                service.RevealArea(center, 1);
                service.ClearTransientVisibility();

                Assert.That(service.IsVisible(center), Is.False);
                Assert.That(service.IsExplored(center), Is.True);
            }
            finally
            {
                Cleanup(context.root);
            }
        }

        [Test]
        public void RevealArea_StopsBehindVisionBlockingTiles()
        {
            var blocker = new GridCoord(3, 2);
            var context = CreateContext(6, 4, blocker);
            try
            {
                var service = context.service;

                var center = new GridCoord(2, 2);
                service.RevealArea(center, 3);

                Assert.That(service.IsVisible(blocker), Is.True, "Blocking tile itself should become visible.");
                Assert.That(service.GetState(new GridCoord(4, 2)), Is.EqualTo(FogOfWarState.Unknown));
            }
            finally
            {
                Cleanup(context.root);
            }
        }

        [Test]
        public void RevealArea_BeforeInitialization_DoesNothing()
        {
            var root = new GameObject("FogDeferredTestRoot");
            try
            {
                var providerGo = new GameObject("StubProvider");
                providerGo.transform.SetParent(root.transform);
                var provider = providerGo.AddComponent<StubTileProvider>();
                provider.Setup(0, 0);

                var serviceGo = new GameObject("FogService");
                serviceGo.transform.SetParent(root.transform);
                var service = serviceGo.AddComponent<FogOfWarService>();

                var center = new GridCoord(1, 1);
                service.RevealArea(center, 2); // ignored because provider not ready

                provider.Setup(5, 5); // simulate Tilemap bake completing
                service.Configure(provider);

                Assert.That(service.IsVisible(center), Is.False);

                service.RevealArea(center, 2);

                Assert.That(service.IsVisible(center), Is.True);
            }
            finally
            {
                Cleanup(root);
            }
        }

        private static (FogOfWarService service, GameObject root) CreateContext(int width, int height, params GridCoord[] blockers)
        {
            var root = new GameObject("FogOfWarTestRoot");

            var providerGo = new GameObject("StubProvider");
            providerGo.transform.SetParent(root.transform);
            var provider = providerGo.AddComponent<StubTileProvider>();
            provider.Setup(width, height, blockers);

            var serviceGo = new GameObject("FogOfWarService");
            serviceGo.transform.SetParent(root.transform);
            var service = serviceGo.AddComponent<FogOfWarService>();
            service.Configure(provider);

            return (service, root);
        }

        private static void Cleanup(GameObject root)
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class StubTileProvider : MonoBehaviour, ITileDataProvider
        {
            private GridBounds _bounds;
            private System.Collections.Generic.HashSet<GridCoord> _blockers;
            private TileData _passable;
            private TileData _blocker;

            public GridBounds Bounds => _bounds;

            public void Setup(int width, int height, params GridCoord[] blockers)
            {
                _bounds = new GridBounds(width, height);
                _blockers = blockers != null && blockers.Length > 0
                    ? new System.Collections.Generic.HashSet<GridCoord>(blockers)
                    : new System.Collections.Generic.HashSet<GridCoord>();

                _passable = ScriptableObject.CreateInstance<TileData>();
                _passable.flags = TileFlags.Passable;

                _blocker = ScriptableObject.CreateInstance<TileData>();
                _blocker.flags = TileFlags.BlocksVision;
            }

            private void OnDestroy()
            {
                if (_passable != null)
                {
                    Object.DestroyImmediate(_passable);
                    _passable = null;
                }

                if (_blocker != null)
                {
                    Object.DestroyImmediate(_blocker);
                    _blocker = null;
                }
            }

            public bool TryGet(GridCoord c, out TileData data)
            {
                if (!_bounds.Contains(c))
                {
                    data = null;
                    return false;
                }

                if (_blockers.Contains(c))
                {
                    data = _blocker;
                }
                else
                {
                    data = _passable;
                }

                return true;
            }
        }
    }
}
