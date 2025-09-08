using System;
using System.Diagnostics;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Immutable axis-aligned bounds over a 2D grid with origin (0,0)
    /// and inclusive min / exclusive max semantics: valid X in [0..Width-1],
    /// valid Y in [0..Height-1].
    /// </summary>
    [Serializable]
    [DebuggerDisplay("W={Width}, H={Height}")]
    public readonly struct GridBounds
    {
        /// <summary>The width of the grid bounds.</summary>
        public int Width { get; }
        /// <summary>The height of the grid bounds.</summary>
        public int Height { get; }

        /// <summary>Whether the bounds are empty (either width or height is zero).</summary>
        public bool IsEmpty => Width == 0 || Height == 0;
        /// <summary>The total number of grid cells within the bounds.</summary>
        public int Area => Width * Height;

        /// <summary>
        /// Create bounds for a grid sized width x height. Width/height must be non-negative.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <exception cref="ArgumentOutOfRangeException">If width or height is negative.</exception>
        public GridBounds(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width cannot be negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Determines whether the specified coordinates are contained within the bounds.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns>
        ///   <c>true</c> if the specified coordinates are contained within the bounds; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        /// <summary>
        /// Determines whether the specified coordinate is contained within the bounds.
        /// </summary>
        /// <param name="c">The grid coordinate.</param>
        /// <returns>
        ///   <c>true</c> if the specified coordinate is contained within the bounds; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(GridCoord c) => Contains(c.X, c.Y);

        /// <summary>
        /// Clamp the given coordinate to lie within this bounds. If the bounds are empty,
        /// returns (0,0).
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns>The clamped grid coordinate.</returns>
        public GridCoord Clamp(int x, int y)
        {
            if (IsEmpty) return GridCoord.Zero;
            if (x < 0) x = 0; else if (x >= Width) x = Width - 1;
            if (y < 0) y = 0; else if (y >= Height) y = Height - 1;
            return new GridCoord(x, y);
        }

        /// <summary>
        /// Clamp the given coordinate to lie within this bounds. If the bounds are empty,
        /// returns (0,0).
        /// </summary>
        /// <param name="c">The grid coordinate.</param>
        /// <returns>The clamped grid coordinate.</returns>
        public GridCoord Clamp(GridCoord c) => Clamp(c.X, c.Y);

        /// <summary>
        /// Returns a string representation of this <see cref="GridBounds"/>.
        /// </summary>
        /// <returns>A string that represents this <see cref="GridBounds"/>.</returns>
        public override string ToString() => $"[0..{Width - 1} x 0..{Height - 1}] (W={Width},H={Height})";
    }
}