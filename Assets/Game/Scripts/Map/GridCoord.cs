using System;
using System.Diagnostics;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Immutable 2D grid coordinate with value semantics suitable for use as a key in dictionaries/sets.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("({X},{Y})")]
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public int X { get; }
        public int Y { get; }

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);
        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);

        public override string ToString() => $"({X},{Y})";

        /// <summary>Enables tuple deconstruction: <c>var (x,y) = coord;</c></summary>
        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        /// <summary>A convenient (0,0) constant.</summary>
        public static GridCoord Zero => new GridCoord(0, 0);
    }
}

