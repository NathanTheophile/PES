#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;

namespace TacticalPort.Core.Interfaces
{
    public interface IPathService
    {
        PathResult FindPath(GridCoord origin, GridCoord destination, int maxCost, UnitId movingUnitId);
        IReadOnlyCollection<GridCoord> GetReachableCells(GridCoord origin, int maxCost, UnitId movingUnitId);
    }
}
