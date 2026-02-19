// Utility: this script defines a grid-space coordinate value object used by tactical actions.
namespace PES.Grid.Grid3D
{
    /// <summary>
    /// Immutable integer 3D grid coordinate (X, Y, Z) for map cells and movement targets.
    /// </summary>
    public readonly struct GridCoord3
    {
        /// <summary>
        /// Creates a coordinate from explicit components.
        /// </summary>
        public GridCoord3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Horizontal X component.</summary>
        public int X { get; }

        /// <summary>Horizontal Y component.</summary>
        public int Y { get; }

        /// <summary>Vertical Z component.</summary>
        public int Z { get; }

        /// <summary>
        /// Human-readable coordinate format for debugging and logs.
        /// </summary>
        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
