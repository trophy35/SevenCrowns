using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map;
using System.Collections.Generic;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class AStarPathfinder_OccupancyTests
    {
        private class ArrayProvider : ITileDataProvider
        {
            private readonly TileData[,] _data;
            public GridBounds Bounds { get; }
            public ArrayProvider(int w, int h, TileData fill)
            {
                _data = new TileData[w, h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        _data[x, y] = fill;
                Bounds = new GridBounds(w, h);
            }
            public void Set(int x, int y, TileData td) => _data[x, y] = td;
            public bool TryGet(GridCoord c, out TileData data)
            {
                data = _data[c.X, c.Y];
                return true;
            }
        }

        private class FakeOccupancy : IGridOccupancyProvider
        {
            private readonly HashSet<GridCoord> _blocked = new HashSet<GridCoord>();
            public void Add(GridCoord c) => _blocked.Add(c);
            public bool IsOccupied(GridCoord c) => _blocked.Contains(c);
            public bool IsOccupiedByOther(GridCoord c, HeroIdentity self) => _blocked.Contains(c);
            public bool TryGetOccupant(GridCoord c, out HeroIdentity hero) { hero = null; return _blocked.Contains(c); }
        }

        private static TileData MakeGrass()
        {
            var td = ScriptableObject.CreateInstance<TileData>();
            td.terrainType = TerrainType.Grass;
            td.flags = TileFlags.Passable;
            td.moveCostCardinal = 10;
            td.moveCostDiagonal = 14;
            td.enterMask = EnterMask8.All;
            return td;
        }

        [Test]
        public void Occupied_Tile_Is_Treated_As_Blocked_By_Pathfinder()
        {
            var grass = MakeGrass();
            var prov = new ArrayProvider(3, 3, grass);
            var occ = new FakeOccupancy();
            occ.Add(new GridCoord(1, 0)); // block middle of top row

            var blocked = new BlockingOverlayTileDataProvider(prov, occ);
            var pf = new AStarPathfinder(blocked, prov.Bounds, new AStarPathfinder.Config { AllowDiagonal = false });

            var start = new GridCoord(0, 0);
            var goal = new GridCoord(2, 0);
            var path = pf.GetPath(start, goal, EnterMask8.N | EnterMask8.S | EnterMask8.E | EnterMask8.W);

            Assert.Greater(path.Count, 0);
            // Path should avoid (1,0); with 4-way, the shortest detour is down and around: (0,0)->(0,1)->(1,1)->(2,1)->(2,0)
            CollectionAssert.DoesNotContain(path, new GridCoord(1, 0));
        }

        [Test]
        public void Occupied_Goal_Makes_Path_Unreachable()
        {
            var grass = MakeGrass();
            var prov = new ArrayProvider(3, 1, grass);
            var occ = new FakeOccupancy();
            occ.Add(new GridCoord(2, 0)); // block goal

            var blocked = new BlockingOverlayTileDataProvider(prov, occ);
            var pf = new AStarPathfinder(blocked, prov.Bounds, new AStarPathfinder.Config { AllowDiagonal = false });

            var path = pf.GetPath(new GridCoord(0, 0), new GridCoord(2, 0), EnterMask8.N | EnterMask8.S | EnterMask8.E | EnterMask8.W);
            Assert.AreEqual(0, path.Count);
        }
    }
}

