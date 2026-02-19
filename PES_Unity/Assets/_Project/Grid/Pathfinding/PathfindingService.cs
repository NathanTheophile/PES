// Utilité : ce script fournit une première implémentation de pathfinding de grille.
// Il génère un chemin discret simple avec validation d'obstacles sur la trajectoire.
using System.Collections.Generic;
using PES.Grid.Grid3D;

namespace PES.Grid.Pathfinding
{
    /// <summary>
    /// Service de cheminement minimal orienté grille.
    /// Cette version conserve une trajectoire déterministe X -> Y -> Z.
    /// </summary>
    public sealed class PathfindingService
    {
        /// <summary>
        /// Calcule un chemin discret entre deux coordonnées sans contrainte de blocage.
        /// </summary>
        public IReadOnlyList<GridCoord3> ComputePath(GridCoord3 from, GridCoord3 to)
        {
            TryComputePath(from, to, new HashSet<GridCoord3>(), out var path);
            return path;
        }

        /// <summary>
        /// Tente de calculer un chemin discret entre deux coordonnées en rejetant les cellules bloquées.
        /// Retourne false si la trajectoire déterministe rencontre un blocage.
        /// </summary>
        public bool TryComputePath(GridCoord3 from, GridCoord3 to, ISet<GridCoord3> blockedCells, out IReadOnlyList<GridCoord3> path)
        {
            var steps = new List<GridCoord3> { from };

            var currentX = from.X;
            var currentY = from.Y;
            var currentZ = from.Z;

            while (currentX != to.X)
            {
                currentX += currentX < to.X ? 1 : -1;
                var next = new GridCoord3(currentX, currentY, currentZ);
                if (blockedCells.Contains(next))
                {
                    path = steps;
                    return false;
                }

                steps.Add(next);
            }

            while (currentY != to.Y)
            {
                currentY += currentY < to.Y ? 1 : -1;
                var next = new GridCoord3(currentX, currentY, currentZ);
                if (blockedCells.Contains(next))
                {
                    path = steps;
                    return false;
                }

                steps.Add(next);
            }

            while (currentZ != to.Z)
            {
                currentZ += currentZ < to.Z ? 1 : -1;
                var next = new GridCoord3(currentX, currentY, currentZ);
                if (blockedCells.Contains(next))
                {
                    path = steps;
                    return false;
                }

                steps.Add(next);
            }

            path = steps;
            return true;
        }
    }
}
