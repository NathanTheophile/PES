// Utilité : ce script fournit une première implémentation de pathfinding de grille.
// Il génère un chemin discret simple (sans obstacles) pour alimenter la validation de déplacement.
using System.Collections.Generic;
using PES.Grid.Grid3D;

namespace PES.Grid.Pathfinding
{
    /// <summary>
    /// Service de cheminement minimal orienté grille.
    /// Cette version bootstrap ne gère pas encore les obstacles ni les coûts terrain.
    /// </summary>
    public sealed class PathfindingService
    {
        /// <summary>
        /// Calcule un chemin discret entre deux coordonnées.
        /// Le chemin avance pas à pas sur X puis Y puis Z, avec des incréments unitaires.
        /// </summary>
        public IReadOnlyList<GridCoord3> ComputePath(GridCoord3 from, GridCoord3 to)
        {
            var path = new List<GridCoord3> { from };

            var currentX = from.X;
            var currentY = from.Y;
            var currentZ = from.Z;

            // Déplacement horizontal sur X.
            while (currentX != to.X)
            {
                currentX += currentX < to.X ? 1 : -1;
                path.Add(new GridCoord3(currentX, currentY, currentZ));
            }

            // Déplacement horizontal sur Y.
            while (currentY != to.Y)
            {
                currentY += currentY < to.Y ? 1 : -1;
                path.Add(new GridCoord3(currentX, currentY, currentZ));
            }

            // Déplacement vertical sur Z.
            while (currentZ != to.Z)
            {
                currentZ += currentZ < to.Z ? 1 : -1;
                path.Add(new GridCoord3(currentX, currentY, currentZ));
            }

            return path;
        }
    }
}
