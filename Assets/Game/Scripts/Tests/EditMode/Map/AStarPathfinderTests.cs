using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class AStarPathfinderTests
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

        private static TileData MakeTile(TerrainType type, bool passable, int card, int diag, EnterMask8 enterMask, TileFlags extraFlags = TileFlags.None)
        {
            var td = ScriptableObject.CreateInstance<TileData>();
            td.terrainType = type;
            td.flags = (passable ? TileFlags.Passable : TileFlags.None) | extraFlags;
            td.moveCostCardinal = Mathf.Max(1, card);
            td.moveCostDiagonal = Mathf.Max(1, diag);
            td.enterMask = enterMask;
            return td;
        }

        private static int PathCost(List<GridCoord> path, ITileDataProvider prov)
        {
            int sum = 0;
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                prov.TryGet(b, out var tdNext);
                bool diag = TileData.IsDiagonalStep(b.X - a.X, b.Y - a.Y);
                sum += tdNext.GetMoveCost(diag);
            }
            return sum;
        }

        [Test]
        public void Grass_Allows_StraightDiagonal_Path()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(4, 4, grass);
            var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());

            var path = pf.GetPath(new GridCoord(0,0), new GridCoord(3,3));
            Assert.Greater(path.Count, 0);
            Assert.AreEqual(new GridCoord(0,0), path[0]);
            Assert.AreEqual(new GridCoord(3,3), path[^1]);
            Assert.AreEqual(14*3, PathCost(path, prov));
        }

        [Test]
        public void Impassable_Block_Detours()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var mountain = MakeTile(TerrainType.Mountain, false, 25, 36, EnterMask8.None, TileFlags.IsMountain);
            var prov = new ArrayProvider(3, 3, grass);
            prov.Set(1,1, mountain);
            var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());

            var path = pf.GetPath(new GridCoord(0,0), new GridCoord(2,2));
            Assert.Greater(path.Count, 0);
            // Cost should be higher than straight diagonal (which would be 28)
            Assert.Greater(PathCost(path, prov), 28);
        }

        [Test]
        public void Directional_Cliff_Allows_Only_NS()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var cliffNS = MakeTile(TerrainType.CliffRamp, true, 12, 17, EnterMask8.N | EnterMask8.S, TileFlags.IsCliffRamp | TileFlags.BlocksVision);
            // Case 1: East entry must be disallowed. Block the alternative north approach to prevent a detour.
            {
                var mountain = MakeTile(TerrainType.Mountain, false, 25, 36, EnterMask8.None, TileFlags.IsMountain);
                var prov = new ArrayProvider(2, 2, grass);
                prov.Set(1,0, cliffNS);   // target tile (east of start)
                prov.Set(1,1, mountain);  // block entering from south/north detour via (1,1)

                var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());
                var pathE = pf.GetPath(new GridCoord(0,0), new GridCoord(1,0));
                Assert.AreEqual(0, pathE.Count, "East entry should be disallowed for NS-only cliff when detours are blocked");
            }

            // Case 2: North entry is allowed
            {
                var prov = new ArrayProvider(2, 2, grass);
                prov.Set(0,1, cliffNS);   // target tile (north of start)

                var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());
                var pathN = pf.GetPath(new GridCoord(0,0), new GridCoord(0,1));
                Assert.Greater(pathN.Count, 0, "North entry should be allowed for NS-only cliff");
            }
        }

        [Test]
        public void CornerCutting_Disallowed_Blocks_Diagonal()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var mountain = MakeTile(TerrainType.Mountain, false, 25, 36, EnterMask8.None, TileFlags.IsMountain);
            var prov = new ArrayProvider(2, 2, grass);
            prov.Set(1,0, mountain);
            prov.Set(0,1, mountain);
            var cfg = new AStarPathfinder.Config { DisallowCornerCutting = true };
            var pf = new AStarPathfinder(prov, prov.Bounds, cfg);
            var path = pf.GetPath(new GridCoord(0,0), new GridCoord(1,1));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void AllowedMoves_CardinalOnly_Avoids_Diagonals()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(5, 5, grass);
            var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());

            var path4 = pf.GetPath(new GridCoord(0,0), new GridCoord(2,1), EnterMask8.N | EnterMask8.S | EnterMask8.E | EnterMask8.W);
            Assert.Greater(path4.Count, 0);
            // 3 steps expected: E,E,N (or N,E,E)
            Assert.AreEqual(4, path4.Count); // includes start => 3 edges

            var path8 = pf.GetPath(new GridCoord(0,0), new GridCoord(2,1), EnterMask8.All);
            Assert.Greater(path8.Count, 0);
            // Should use one diagonal and one cardinal => 2 edges => count 3
            Assert.AreEqual(3, path8.Count);
        }

        [Test]
        public void StraightCardinal_UsesCorrectCost()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(1, 5, grass);
            var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());

            var start = new GridCoord(0, 0);
            var goal = new GridCoord(0, 4);
            var path = pf.GetPath(start, goal);
            Assert.AreEqual(5, path.Count); // 4 edges
            Assert.AreEqual(40, PathCost(path, prov));
        }

        [Test]
        public void Unreachable_ReturnsEmpty()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var mountain = MakeTile(TerrainType.Mountain, false, 25, 36, EnterMask8.None, TileFlags.IsMountain);
            var prov = new ArrayProvider(3, 3, grass);
            // Goal at center (1,1), surrounded by impassables
            prov.Set(0,1, mountain);
            prov.Set(2,1, mountain);
            prov.Set(1,0, mountain);
            prov.Set(1,2, mountain);

            var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());
            var path = pf.GetPath(new GridCoord(0,0), new GridCoord(1,1));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void Deterministic_Tie_WhenMultipleShortestPaths()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(4, 3, grass);
            var pf = new AStarPathfinder(prov, prov.Bounds, new AStarPathfinder.Config());

            var start = new GridCoord(0,0);
            var goal = new GridCoord(2,1);
            var p1 = pf.GetPath(start, goal);
            var p2 = pf.GetPath(start, goal);

            Assert.Greater(p1.Count, 0);
            Assert.AreEqual(PathCost(p1, prov), PathCost(p2, prov));
            // Optimal cost should be one diagonal + one cardinal: 14 + 10 = 24
            Assert.AreEqual(24, PathCost(p1, prov));
            Assert.AreEqual(p1.Count, p2.Count);
            for (int i = 0; i < p1.Count; i++)
            {
                Assert.AreEqual(p1[i], p2[i], $"Path tie-breaking not deterministic at index {i}");
            }
        }
    }
}
