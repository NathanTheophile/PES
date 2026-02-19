namespace PES.Grid.Grid3D
{
    public readonly struct GridCoord3
    {
        public GridCoord3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
