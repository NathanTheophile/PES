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

            List<UnitRuntime> lUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit != null)
                    lUnits.Add(lUnit);
            }

            lUnits.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            List<UnitRuntime> lDefeatedUnits = lUnits.FindAll(pUnit => !pUnit.IsAlive);

            for (int lDefeatedIndex = 0; lDefeatedIndex < lDefeatedUnits.Count; lDefeatedIndex++)
            {
                UnitRuntime lDefeatedOwner = lDefeatedUnits[lDefeatedIndex];
                for (int lUnitIndex = 0; lUnitIndex < lUnits.Count; lUnitIndex++)
                {
                    UnitRuntime lOwnedUnit = lUnits[lUnitIndex];
                    if (!lOwnedUnit.IsAlive || lOwnedUnit.OwnerUnitId != lDefeatedOwner.Id)
                        continue;

                    lOwnedUnit.ApplyDirectHealthLoss(lOwnedUnit.CurrentHealth);
                    lDefeatedUnits.Add(lOwnedUnit);
                }
            }

            lDefeatedUnits.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));

            foreach (UnitRuntime lDefeatedUnit in lDefeatedUnits)
            {
                for (int lIndex = 0; lIndex < lUnits.Count; lIndex++)
                    lUnits[lIndex].RemoveStatesBySource(lDefeatedUnit.Id);

                BattlePassiveResolver.CleanupOwnedEffects(lDefeatedUnit, lUnits);
                pGridService.RemoveGlyphsByLifetimeSource(lDefeatedUnit.Id);
                pGridService.RemoveUnit(lDefeatedUnit.Id);
                pTurnSystem.RemoveUnit(lDefeatedUnit.Id);
            }
        }

        public static void TickTemporaryStateDurationsForTurnOwner(
            UnitRuntime pTurnOwner,
            IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById)
        {
            if (pTurnOwner == null || pUnitsById == null)
                return;

            List<UnitId> lDurationSources = new List<UnitId> { pTurnOwner.Id };
            foreach (UnitRuntime lUnit in pUnitsById.Values)
            {
                if (lUnit != null
                    && lUnit.IsAlive
                    && !lUnit.Definition.ParticipatesInTurnOrder
                    && lUnit.OwnerUnitId == pTurnOwner.Id)
                {
                    lDurationSources.Add(lUnit.Id);
                }
            }

            lDurationSources.Sort((pLeft, pRight) => pLeft.Value.CompareTo(pRight.Value));
            List<UnitRuntime> lTargets = new List<UnitRuntime>(pUnitsById.Values);
            lTargets.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            for (int lTargetIndex = 0; lTargetIndex < lTargets.Count; lTargetIndex++)
            {
                UnitRuntime lTarget = lTargets[lTargetIndex];
                for (int lSourceIndex = 0; lSourceIndex < lDurationSources.Count; lSourceIndex++)
                {
                    lTarget.TickTemporaryStateDurationsForSource(
                        lDurationSources[lSourceIndex],
                        lDurationSources[lSourceIndex] == pTurnOwner.Id && lTarget.Id == pTurnOwner.Id);
                }
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
