// Utility: this script provides a placeholder pathfinding service API for future grid navigation rules.
using System.Collections.Generic;
using PES.Grid.Grid3D;

namespace PES.Grid.Pathfinding
{
    /// <summary>
    /// Temporary pathfinding stub.
    /// Later iterations will replace this with obstacle-aware, height-aware search.
    /// </summary>
    public sealed class PathfindingService
    {
        /// <summary>
        /// Computes a path between two coordinates.
        /// Current bootstrap behavior returns a minimal 2-node path [from, to].
        /// </summary>
        public IReadOnlyList<GridCoord3> ComputePath(GridCoord3 from, GridCoord3 to)
        {
            // This is intentionally simple for bootstrap compile safety.
            return new[] { from, to };
        }
    }
}
