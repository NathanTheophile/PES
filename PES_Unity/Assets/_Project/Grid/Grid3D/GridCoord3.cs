// Utilité : ce script définit une coordonnée de grille utilisée par les actions tactiques.
namespace PES.Grid.Grid3D
{
    /// <summary>
    /// Coordonnée 3D entière immuable (X, Y, Z) pour les cellules de map et cibles de déplacement.
    /// </summary>
    public readonly struct GridCoord3
    {
        /// <summary>
        /// Construit une coordonnée à partir de composantes explicites.
        /// </summary>
        public GridCoord3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Composante horizontale X.</summary>
        public int X { get; }

        /// <summary>Composante horizontale Y.</summary>
        public int Y { get; }

        /// <summary>Composante verticale Z.</summary>
        public int Z { get; }

        /// <summary>
        /// Format de coordonnées lisible pour debug et logs.
        /// </summary>
        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
