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
    [Flags]
    public enum BattleActionOutcomeFlags
    {
        None = 0,
        PrimaryDamage = 1 << 0,
        Heal = 1 << 1,
        CollisionDamage = 1 << 2,
        StateApplied = 1 << 3,
        Summon = 1 << 4,
        Glyph = 1 << 5
    }

    public sealed class BattleActionResult
    {
        #region _____________________________| FACTORIES

        public static BattleActionResult Failed(BattleActionType pActionType, string pMessage) =>
            new BattleActionResult(false, pActionType, pMessage, Array.Empty<UnitId>(), Array.Empty<GridCoord>(), BattleActionOutcomeFlags.None);

        public static BattleActionResult Succeeded(
            BattleActionType pActionType,
            string pMessage,
            IEnumerable<UnitId> pAffectedUnitIds = null,
            IEnumerable<GridCoord> pTraversedPath = null,
            BattleActionOutcomeFlags pOutcomeFlags = BattleActionOutcomeFlags.None) =>
            new BattleActionResult(
                true,
                pActionType,
                pMessage,
                pAffectedUnitIds?.ToArray() ?? Array.Empty<UnitId>(),
                pTraversedPath?.ToArray() ?? Array.Empty<GridCoord>(),
                pOutcomeFlags);

        private BattleActionResult(
            bool pIsSuccess,
            BattleActionType pActionType,
            string pMessage,
            IReadOnlyList<UnitId> pAffectedUnitIds,
            IReadOnlyList<GridCoord> pTraversedPath,
            BattleActionOutcomeFlags pOutcomeFlags)
        {
            IsSuccess = pIsSuccess;
            ActionType = pActionType;
            Message = pMessage ?? string.Empty;
            AffectedUnitIds = pAffectedUnitIds;
            TraversedPath = pTraversedPath;
            OutcomeFlags = pOutcomeFlags;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsSuccess { get; }
        public BattleActionType ActionType { get; }
        public string Message { get; }
        public IReadOnlyList<UnitId> AffectedUnitIds { get; }
        public IReadOnlyList<GridCoord> TraversedPath { get; }
        public BattleActionOutcomeFlags OutcomeFlags { get; }

        #endregion
    }
}
