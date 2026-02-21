using System;
using System.Collections.Generic;
using PES.Grid.Grid3D;

namespace PES.Grid.Pathfinding
{
    /// <summary>
    /// Service de cheminement orienté grille.
    /// - Mode legacy : navigation 3D (6 voisins) avec cellules bloquées.
    /// - Mode height-map : 1 tile par index X/Y, hauteur portée par Z, 4 voisins planaires + contrainte de marche verticale.
    /// </summary>
    public sealed class PathfindingService
    {
        private static readonly GridCoord3[] LegacyNeighborOffsets =
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
            new(0, 0, 1),
            new(0, 0, -1),
        };

        private static readonly GridCoord3[] PlanarNeighborOffsets =
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
        };

        public IReadOnlyList<GridCoord3> ComputePath(GridCoord3 from, GridCoord3 to)
        {
            TryComputePath(from, to, new HashSet<GridCoord3>(), out var path);
            return path;
        }

        public bool TryComputePath(GridCoord3 from, GridCoord3 to, ISet<GridCoord3> blockedCells, out IReadOnlyList<GridCoord3> path)
        {
            return TryComputePath(from, to, blockedCells, out path, null, int.MaxValue);
        }

        public bool TryComputePath(
            GridCoord3 from,
            GridCoord3 to,
            ISet<GridCoord3> blockedCells,
            out IReadOnlyList<GridCoord3> path,
            ISet<GridCoord3> walkableCells,
            int maxVerticalStepPerTile)
        {
            if (blockedCells.Contains(to))
            {
                path = new List<GridCoord3> { from };
                return false;
            }

            if (walkableCells == null || walkableCells.Count == 0)
            {
                return TryComputeLegacyPath(from, to, blockedCells, out path);
            }

            return TryComputeHeightMapPath(from, to, blockedCells, walkableCells, maxVerticalStepPerTile, out path);
        }

        private static bool TryComputeLegacyPath(GridCoord3 from, GridCoord3 to, ISet<GridCoord3> blockedCells, out IReadOnlyList<GridCoord3> path)
        {
            var maxSearchRadius = ManhattanDistance(from, to) + blockedCells.Count + 6;
            var frontier = new Queue<GridCoord3>();
            var cameFrom = new Dictionary<GridCoord3, GridCoord3>();
            var visited = new HashSet<GridCoord3> { from };
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current.Equals(to))
                {
                    path = RebuildPath(from, to, cameFrom);
                    return true;
                }

                for (var i = 0; i < LegacyNeighborOffsets.Length; i++)
                {
                    var offset = LegacyNeighborOffsets[i];
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

        private static bool TryComputeHeightMapPath(
            GridCoord3 from,
            GridCoord3 to,
            ISet<GridCoord3> blockedCells,
            ISet<GridCoord3> walkableCells,
            int maxVerticalStepPerTile,
            out IReadOnlyList<GridCoord3> path)
        {
            var safeMaxVerticalStep = maxVerticalStepPerTile < 0 ? 0 : maxVerticalStepPerTile;
            var heightsByPlanarCell = BuildHeightLookup(walkableCells);
            var fromKey = (from.X, from.Y);
            var toKey = (to.X, to.Y);

            if (!heightsByPlanarCell.TryGetValue(fromKey, out var fromHeight) || !heightsByPlanarCell.TryGetValue(toKey, out var toHeight))
            {
                path = new List<GridCoord3> { from };
                return false;
            }

            var normalizedFrom = new GridCoord3(from.X, from.Y, fromHeight);
            var normalizedTo = new GridCoord3(to.X, to.Y, toHeight);

            if (blockedCells.Contains(normalizedTo))
            {
                path = new List<GridCoord3> { normalizedFrom };
                return false;
            }

            var frontier = new Queue<GridCoord3>();
            var cameFrom = new Dictionary<GridCoord3, GridCoord3>();
            var visited = new HashSet<GridCoord3> { normalizedFrom };
            frontier.Enqueue(normalizedFrom);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current.Equals(normalizedTo))
                {
                    path = RebuildPath(normalizedFrom, normalizedTo, cameFrom);
                    return true;
                }

                for (var i = 0; i < PlanarNeighborOffsets.Length; i++)
                {
                    var offset = PlanarNeighborOffsets[i];
                    var planarX = current.X + offset.X;
                    var planarY = current.Y + offset.Y;
                    var planarKey = (planarX, planarY);
                    if (!heightsByPlanarCell.TryGetValue(planarKey, out var neighborHeight))
                    {
                        continue;
                    }

                    if (Math.Abs(neighborHeight - current.Z) > safeMaxVerticalStep)
                    {
                        continue;
                    }

                    var next = new GridCoord3(planarX, planarY, neighborHeight);
                    if (visited.Contains(next) || blockedCells.Contains(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            path = new List<GridCoord3> { normalizedFrom };
            return false;
        }

        private static Dictionary<(int X, int Y), int> BuildHeightLookup(ISet<GridCoord3> walkableCells)
        {
            var lookup = new Dictionary<(int X, int Y), int>(walkableCells.Count);
            foreach (var cell in walkableCells)
            {
                lookup[(cell.X, cell.Y)] = cell.Z;
            }

            return lookup;
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
