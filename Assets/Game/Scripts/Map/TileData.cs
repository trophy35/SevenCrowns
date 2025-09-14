using System;
using UnityEngine;

namespace SevenCrowns.Map
{
    public enum TerrainType
    {
        Grass,
        Road,
        Forest,
        Mountain,
        Water,
        CliffRampBottom,
        CliffRampTop
    }

    /// <summary>
    /// 8-way directional mask for tile entry rules.
    /// Convention (grid deltas):
    ///  N  = (0, +1)
    ///  NE = (+1, +1)
    ///  E  = (+1, 0)
    ///  SE = (+1, -1)
    ///  S  = (0, -1)
    ///  SW = (-1, -1)
    ///  W  = (-1, 0)
    ///  NW = (-1, +1)
    /// </summary>
    [Flags]
    public enum EnterMask8
    {
        None = 0,
        N  = 1 << 0,
        NE = 1 << 1,
        E  = 1 << 2,
        SE = 1 << 3,
        S  = 1 << 4,
        SW = 1 << 5,
        W  = 1 << 6,
        NW = 1 << 7,
        All = N | NE | E | SE | S | SW | W | NW
    }

    [Flags]
    public enum TileFlags
    {
        None         = 0,
        Passable     = 1 << 0,
        BlocksVision = 1 << 1,
        IsRoad       = 1 << 2,
        IsWater      = 1 << 3,
        IsForest     = 1 << 4,
        IsMountain   = 1 << 5,
        IsCliffRamp  = 1 << 6,
    }

    [CreateAssetMenu(fileName = "TileData", menuName = "SevenCrowns/Map/Tile Data")]
    public sealed class TileData : ScriptableObject
    {
        [Header("Identity")]
        public TerrainType terrainType = TerrainType.Grass;
        public TileFlags flags = TileFlags.Passable;

        [Header("Movement Cost (MP)")]
        [Min(1)] public int moveCostCardinal = 10; // N/E/S/W
        [Min(1)] public int moveCostDiagonal = 14; // NE/NW/SE/SW

        [Header("Entry Rules (8-way)")]
        public EnterMask8 enterMask = EnterMask8.All;

        [Header("Authoring Aids (optional)")]
        public Sprite editorPreview;
        [TextArea] public string notes;

        /// <summary>True if this tile can be entered at all.</summary>
        public bool IsPassable => (flags & TileFlags.Passable) != 0;

        /// <summary>Return movement cost depending on step type.</summary>
        public int GetMoveCost(bool isDiagonal) => isDiagonal ? moveCostDiagonal : moveCostCardinal;

        /// <summary>Return true if entering from the given direction is permitted.</summary>
        public bool CanEnterFrom(EnterMask8 fromDirection) => (enterMask & fromDirection) != 0;

        /// <summary>
        /// Map a neighbor step delta (dx,dy) in {-1,0,+1} to an 8-way enter mask flag.
        /// Returns EnterMask8.None for (0,0) or out-of-range deltas.
        /// </summary>
        public static EnterMask8 DirectionFromDelta(int dx, int dy)
        {
            dx = Mathf.Clamp(dx, -1, 1);
            dy = Mathf.Clamp(dy, -1, 1);
            if (dx == 0 && dy == 0) return EnterMask8.None;

            if (dx == 0 && dy == 1) return EnterMask8.N;
            if (dx == 1 && dy == 1) return EnterMask8.NE;
            if (dx == 1 && dy == 0) return EnterMask8.E;
            if (dx == 1 && dy == -1) return EnterMask8.SE;
            if (dx == 0 && dy == -1) return EnterMask8.S;
            if (dx == -1 && dy == -1) return EnterMask8.SW;
            if (dx == -1 && dy == 0) return EnterMask8.W;
            if (dx == -1 && dy == 1) return EnterMask8.NW;
            return EnterMask8.None;
        }

        /// <summary>Returns true if a step with delta (dx,dy) is diagonal.</summary>
        public static bool IsDiagonalStep(int dx, int dy) => (Mathf.Abs(dx) + Mathf.Abs(dy)) == 2;

#if UNITY_EDITOR
        private void OnValidate()
        {
            moveCostCardinal = Mathf.Max(1, moveCostCardinal);
            moveCostDiagonal = Mathf.Max(1, moveCostDiagonal);

            // If not passable, disallow any entry.
            if (!IsPassable)
            {
                enterMask = EnterMask8.None;
            }
        }
#endif
    }
}

