using System;
using System.Collections.Generic;
using NUnit.Framework;
using SevenCrowns.Map;
using UnityEngine;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class HeroMapAgentTests
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

        private static List<GridCoord> Path(params GridCoord[] pts) => new List<GridCoord>(pts);

        [Test]
        public void Advance_All_ReachesGoal_WhenEnoughMP()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(4, 1, grass);
            var mp = new MapMovementService(240);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0));
            agent.SetPath(Path(new GridCoord(0,0), new GridCoord(1,0), new GridCoord(2,0), new GridCoord(3,0)));

            var res = agent.AdvanceAllAvailable();
            Assert.AreEqual(3, res.StepsCommitted);
            Assert.AreEqual(30, res.MpSpent);
            Assert.AreEqual(new GridCoord(3,0), res.NewPosition);
            Assert.AreEqual(StopReason.ReachedGoal, res.Reason);
            Assert.AreEqual(210, agent.RemainingMP);
        }

        [Test]
        public void Advance_Stops_OnInsufficientMP()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(4, 1, grass);
            var mp = new MapMovementService(20);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0));
            agent.SetPath(Path(new GridCoord(0,0), new GridCoord(1,0), new GridCoord(2,0), new GridCoord(3,0)));

            var res = agent.AdvanceAllAvailable();
            Assert.AreEqual(2, res.StepsCommitted);
            Assert.AreEqual(20, res.MpSpent);
            Assert.AreEqual(new GridCoord(2,0), res.NewPosition);
            Assert.AreEqual(StopReason.InsufficientMP, res.Reason);
            Assert.AreEqual(0, agent.RemainingMP);
        }

        [Test]
        public void Advance_Stops_OnBlockedTerrain()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var mountain = MakeTile(TerrainType.Mountain, false, 25, 36, EnterMask8.None, TileFlags.IsMountain);
            var prov = new ArrayProvider(4, 1, grass);
            prov.Set(2,0, mountain);
            var mp = new MapMovementService(240);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0));
            agent.SetPath(Path(new GridCoord(0,0), new GridCoord(1,0), new GridCoord(2,0), new GridCoord(3,0)));

            var res = agent.AdvanceAllAvailable();
            Assert.AreEqual(1, res.StepsCommitted);
            Assert.AreEqual(10, res.MpSpent);
            Assert.AreEqual(new GridCoord(1,0), res.NewPosition);
            Assert.AreEqual(StopReason.BlockedByTerrain, res.Reason);
        }

        [Test]
        public void Advance_Respects_AllowedMoves_CardinalsOnly()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(2, 2, grass);
            var mp = new MapMovementService(240);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0), EnterMask8.N | EnterMask8.E | EnterMask8.S | EnterMask8.W);
            // Diagonal move is disallowed by allowedMoves
            agent.SetPath(Path(new GridCoord(0,0), new GridCoord(1,1)));
            var res = agent.AdvanceAllAvailable();
            Assert.AreEqual(0, res.StepsCommitted);
            Assert.AreEqual(0, res.MpSpent);
            Assert.AreEqual(StopReason.BlockedByTerrain, res.Reason);
            Assert.AreEqual(new GridCoord(0,0), agent.Position);
        }

        [Test]
        public void Preview_DoesNotMutate()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(3, 1, grass);
            var mp = new MapMovementService(24);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0));
            agent.SetPath(Path(new GridCoord(0,0), new GridCoord(1,0), new GridCoord(2,0)));

            var preview = agent.Preview();
            Assert.AreEqual(2, preview.StepsPayable);
            Assert.AreEqual(20, preview.MpNeeded);
            Assert.AreEqual(24, agent.RemainingMP);
            Assert.AreEqual(new GridCoord(0,0), agent.Position);
        }

        [Test]
        public void Events_Fire_InOrder()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(3, 1, grass);
            var mp = new MapMovementService(30);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0));
            agent.SetPath(Path(new GridCoord(0,0), new GridCoord(1,0), new GridCoord(2,0)));

            int started = 0, steps = 0, posChanged = 0, mpChanged = 0, stopped = 0;
            agent.Started += () => started++;
            agent.StepCommitted += (_, _, __) => steps++;
            agent.PositionChanged += _ => posChanged++;
            agent.RemainingMPChanged += (_, __) => mpChanged++;
            agent.Stopped += _ => stopped++;

            var res = agent.AdvanceAllAvailable();
            Assert.AreEqual(1, started);
            Assert.AreEqual(2, steps);
            Assert.AreEqual(2, posChanged);
            Assert.GreaterOrEqual(mpChanged, 1); // at least one change emitted by MP service
            Assert.AreEqual(1, stopped);
            Assert.AreEqual(StopReason.ReachedGoal, res.Reason);
        }

        [Test]
        public void Invalid_StartPath_ReturnsFalse()
        {
            var grass = MakeTile(TerrainType.Grass, true, 10, 14, EnterMask8.All);
            var prov = new ArrayProvider(2, 1, grass);
            var mp = new MapMovementService(10);
            var agent = new HeroMapAgent(prov, mp, new GridCoord(0,0));
            bool ok = agent.SetPath(Path(new GridCoord(1,0), new GridCoord(0,0)));
            Assert.IsFalse(ok);
            Assert.AreEqual(new GridCoord(0,0), agent.Position);
        }
    }
}

