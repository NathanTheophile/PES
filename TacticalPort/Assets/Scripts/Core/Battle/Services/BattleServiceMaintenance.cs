#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class BattleServiceMaintenance
    {
        public static void RefreshAllPhaseStates(IEnumerable<UnitRuntime> pUnits) =>
            BattlePhaseStateResolver.RefreshAll(pUnits);

        public static void RefreshPhaseStates(IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById, IEnumerable<UnitId> pUnitIds) =>
            BattlePhaseStateResolver.Refresh(pUnitsById, pUnitIds);

        public static void RefreshPhaseStates(UnitRuntime pUnit) => BattlePhaseStateResolver.Refresh(pUnit);

        public static void CleanupDefeatedUnits(
            IEnumerable<UnitRuntime> pUnits,
            IGridService pGridService,
            ITurnSystem pTurnSystem)
        {
            if (pUnits == null)
                return;

            List<UnitRuntime> lDefeatedUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit != null && !lUnit.IsAlive)
                    lDefeatedUnits.Add(lUnit);
            }

            foreach (UnitRuntime lDefeatedUnit in lDefeatedUnits)
            {
                pGridService.RemoveUnit(lDefeatedUnit.Id);
                pTurnSystem.RemoveUnit(lDefeatedUnit.Id);
            }
        }

        public static void CompleteTurnIfActiveUnitIsGone(
            ITurnSystem pTurnSystem,
            IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById)
        {
            if (pTurnSystem?.CurrentTurn == null)
                return;

            UnitId lActiveUnitId = pTurnSystem.CurrentTurn.UnitId;
            if (pUnitsById != null
                && pUnitsById.TryGetValue(lActiveUnitId, out UnitRuntime lUnit)
                && lUnit != null
                && lUnit.IsAlive)
            {
                return;
            }

            pTurnSystem.CompleteCurrentTurn();
        }

        public static BattleOutcome EvaluateOutcome(
            IEnumerable<UnitRuntime> pUnits,
            ITurnSystem pTurnSystem)
        {
            BattleOutcome lOutcome = BattleOutcomeEvaluator.Evaluate(pUnits);
            if (lOutcome != BattleOutcome.None)
                pTurnSystem.CompleteCurrentTurn();

            return lOutcome;
        }
    }
}
