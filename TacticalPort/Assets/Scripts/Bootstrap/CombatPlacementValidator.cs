#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Bootstrap
#endregion

using TacticalPort.Combat;
using TacticalPort.Core;
using TacticalPort.Shared;
using TacticalPort.View;

namespace TacticalPort.Bootstrap
{
    internal static class CombatPlacementValidator
    {
        #region _____________________________| VALIDATE

        public static bool TryValidatePlacementCommand(
            IBattleService pBattleService,
            BoardView pBoardView,
            ICombatCommandSink pCommandSink,
            UnitId pUnitId,
            GridCoord pDestination,
            bool pRequireLocalControl,
            out BattleActionResult pFailure)
        {
            pFailure = null;

            if (pBattleService == null || pBattleService.Phase != BattlePhase.Placement)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Placement phase is not active.");
                return false;
            }

            if (!pBattleService.TryGetUnit(pUnitId, out UnitRuntime lUnit) || lUnit == null || !lUnit.IsAlive)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Selected unit is not available.");
                return false;
            }

            if (lUnit.Team == Team.Neutral)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Neutral units cannot be placed.");
                return false;
            }

            if (pCommandSink == null)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Placement command sink is not ready.");
                return false;
            }

            if (pRequireLocalControl && !pCommandSink.CanLocallyControlTeam(lUnit.Team))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "This unit is not controlled locally.");
                return false;
            }

            if (!CanPlaceUnitOnSpawnCell(pBoardView, pCommandSink, lUnit, pDestination))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Choose an empty spawn cell.");
                return false;
            }

            if (IsPlacementCellOccupied(pBattleService, pUnitId, pDestination))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "This spawn cell is already occupied.");
                return false;
            }

            return true;
        }

        private static bool CanPlaceUnitOnSpawnCell(BoardView pBoardView, ICombatCommandSink pCommandSink, UnitRuntime pUnit, GridCoord pDestination)
        {
            if (pBoardView == null || pCommandSink == null || pUnit == null)
                return false;

            return pCommandSink.TryGetSlotForTeam(pUnit.Team, out MatchPlayerSlot lSlot)
                && pBoardView.IsSpawnerCell(pDestination, lSlot);
        }

        private static bool IsPlacementCellOccupied(IBattleService pBattleService, UnitId pMovingUnitId, GridCoord pDestination)
        {
            if (pBattleService?.Units == null)
                return false;

            foreach (UnitRuntime lRuntimeUnit in pBattleService.Units)
            {
                if (lRuntimeUnit == null || !lRuntimeUnit.IsAlive || lRuntimeUnit.Id == pMovingUnitId)
                    continue;

                foreach (GridCoord lOccupiedCell in lRuntimeUnit.EnumerateOccupiedCells())
                {
                    if (lOccupiedCell == pDestination)
                        return true;
                }
            }

            return false;
        }

        #endregion
    }
}
