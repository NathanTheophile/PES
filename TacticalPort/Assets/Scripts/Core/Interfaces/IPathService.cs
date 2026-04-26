using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;

namespace TacticalPort.Core.Interfaces
{
    public interface IPathService
    {
        PathResult FindPath(GridCoord origin, GridCoord destination, int maxCost, BattleUnitId movingUnitId);
        IReadOnlyCollection<GridCoord> GetReachableCells(GridCoord origin, int maxCost, BattleUnitId movingUnitId);
    }
}
