using NUnit.Framework;
using SevenCrowns.Map;
using UnityEngine;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class HeroMapAgent_OccupancyTests
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
            public bool TryGet(GridCoord c, out TileData data)
            {
                data = _data[c.X, c.Y];
                return true;
            }
        }

        private static TileData Grass()
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
        public void Movement_Stops_When_Next_Tile_Is_Occupied()
        {
            var prov = new ArrayProvider(3, 1, Grass());
            var mp = new MapMovementService(max: 20, current: 20);
            var start = new GridCoord(0, 0);
            bool Blocked(GridCoord c) => c.X == 1 && c.Y == 0;

            var agent = new HeroMapAgent(prov, mp, start, EnterMask8.N | EnterMask8.S | EnterMask8.E | EnterMask8.W, validateSteps: true, isBlocked: Blocked);
            var path = new System.Collections.Generic.List<GridCoord> { start, new GridCoord(1, 0), new GridCoord(2, 0) };
            Assert.IsTrue(agent.SetPath(path));
            var res = agent.AdvanceAllAvailable();
            Assert.AreEqual(0, res.StepsCommitted);
            Assert.AreEqual(StopReason.BlockedByTerrain, res.Reason);
            Assert.AreEqual(start, res.NewPosition);
        }
    }
}

