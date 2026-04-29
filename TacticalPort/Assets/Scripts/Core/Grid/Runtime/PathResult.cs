#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class PathResult
    {
        #region _____________________________| FACTORIES

        public static PathResult Failed(string pFailureReason) => new PathResult(false, pFailureReason, Array.Empty<GridCoord>(), 0);

        public static PathResult Succeeded(IEnumerable<GridCoord> pSteps, int pTotalCost) =>
            new PathResult(true, string.Empty, pSteps?.ToArray() ?? Array.Empty<GridCoord>(), pTotalCost);

        private PathResult(bool pIsSuccess, string pFailureReason, IReadOnlyList<GridCoord> pSteps, int pTotalCost)
        {
            IsSuccess = pIsSuccess;
            FailureReason = pFailureReason ?? string.Empty;
            Steps = pSteps;
            TotalCost = pTotalCost;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsSuccess { get; }
        public string FailureReason { get; }
        public IReadOnlyList<GridCoord> Steps { get; }
        public int TotalCost { get; }

        #endregion
    }
}
