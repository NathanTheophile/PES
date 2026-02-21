// Utilité : ce script fournit une première implémentation de pathfinding de grille.
// Il génère un chemin discret simple avec validation d'obstacles sur la trajectoire.
using System;
using System.Collections.Generic;
using PES.Grid.Grid3D;

namespace PES.Grid.Pathfinding
{
    /// <summary>
    /// Service de cheminement minimal orienté grille.
    /// Cette version calcule un plus court chemin en largeur (BFS) avec ordre de voisins déterministe.
    /// </summary>
    public sealed class PathfindingService
    {
        private static readonly GridCoord3[] NeighborOffsets =
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
            new(0, 0, 1),
            new(0, 0, -1),
        };

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
        /// Retourne false si aucun chemin n'est trouvé dans une fenêtre de recherche bornée.
        /// </summary>
        public bool TryComputePath(GridCoord3 from, GridCoord3 to, ISet<GridCoord3> blockedCells, out IReadOnlyList<GridCoord3> path)
        {
            if (blockedCells.Contains(to))
            {
                path = new List<GridCoord3> { from };
                return false;
            }

            var maxSearchRadius = ComputeMaxSearchRadius(from, to, blockedCells.Count);
            var frontier = new Queue<GridCoord3>();
            var cameFrom = new Dictionary<GridCoord3, GridCoord3>();
            var visited = new HashSet<GridCoord3>();

            frontier.Enqueue(from);
            visited.Add(from);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current.Equals(to))
                {
                    path = RebuildPath(from, to, cameFrom);
                    return true;
                }

                for (var i = 0; i < NeighborOffsets.Length; i++)
                {
                    var offset = NeighborOffsets[i];
                    var next = new GridCoord3(current.X + offset.X, current.Y + offset.Y, current.Z + offset.Z);
                    if (visited.Contains(next) || blockedCells.Contains(next))
                    {
                        continue;
                    }

                    if (ManhattanDistance(from, next) > maxSearchRadius)
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            path = new List<GridCoord3> { from };
            return false;
        }

        private static int ComputeMaxSearchRadius(GridCoord3 from, GridCoord3 to, int blockedCount)
        {
            var directDistance = ManhattanDistance(from, to);
            var detourBudget = blockedCount + 6;
            return directDistance + detourBudget;
        }

        private static int ManhattanDistance(GridCoord3 a, GridCoord3 b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
        }

        private static IReadOnlyList<GridCoord3> RebuildPath(GridCoord3 from, GridCoord3 to, IReadOnlyDictionary<GridCoord3, GridCoord3> cameFrom)
        {
            var reversed = new List<GridCoord3> { to };
            var cursor = to;

            while (!cursor.Equals(from))
            {
                if (!cameFrom.TryGetValue(cursor, out var previous))
                {
                    return new List<GridCoord3> { from };
                }

                reversed.Add(previous);
                cursor = previous;
            }

            reversed.Reverse();
            return reversed;
        }
    }
}
