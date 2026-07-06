#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core;
using TacticalPort.Shared;
using TacticalPort.View;

namespace TacticalPort.Combat
{
    internal sealed class PlacementInteractionController
    {
        #region _____________________________/ VALUES

        private readonly CombatInteractionContext _Context;
        private UnitId _SelectedPlacementUnitId = UnitId.None;

        #endregion

        #region _____________________________| INIT

        public PlacementInteractionController(CombatInteractionContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| INTERACTION

        public bool TryHandleClick(bool pHasHoveredCell, GridCoord pHoveredCell)
        {
            if (!pHasHoveredCell)
            {
                _Context.SetStatus("Select a player unit, then choose an empty spawn cell.");
                return false;
            }

            if (_Context.TryResolveAliveUnitAtCell(pHoveredCell, out UnitRuntime lHoveredUnit)
                && _Context.Bootstrap.CanLocalPlayerControlUnit(lHoveredUnit))
            {
                _SelectedPlacementUnitId = lHoveredUnit.Id;
                _Context.SetStatus($"Selected {lHoveredUnit.Definition.DisplayName}. Choose an empty spawn cell.");
                return true;
            }

            if (!TryGetSelectedUnit(out UnitRuntime lSelectedUnit))
            {
                _Context.SetStatus("Select one of your units before choosing a placement cell.");
                return false;
            }

            if (_Context.BoardView == null
                || !_Context.Bootstrap.TryGetPlacementSlotForTeam(lSelectedUnit.Team, out MatchPlayerSlot lSlot)
                || !_Context.BoardView.IsSpawnerCell(pHoveredCell, lSlot))
            {
                _Context.SetStatus("Choose a blue spawn cell.");
                return false;
            }

            if (_Context.TryResolveAliveUnitAtCell(pHoveredCell, out _))
            {
                _Context.SetStatus("This spawn cell is already occupied.");
                return false;
            }

            BattleActionResult lResult = _Context.Bootstrap.RepositionUnitDuringPlacement(_SelectedPlacementUnitId, pHoveredCell);
            _Context.SetStatus(lResult.IsSuccess
                ? $"{lSelectedUnit.Definition.DisplayName} placed on {pHoveredCell}."
                : lResult.Message);

            return true;
        }

        public void FillReachableSpawnCells(ISet<GridCoord> pReachableCells)
        {
            if (pReachableCells == null || _Context.BoardView == null)
                return;

            if (!TryGetSelectedUnit(out UnitRuntime lSelectedUnit)
                || !_Context.Bootstrap.TryGetPlacementSlotForTeam(lSelectedUnit.Team, out MatchPlayerSlot lSlot))
                return;

            foreach (GridCoord lCoord in _Context.BoardView.GetSpawnerCells(lSlot))
            {
                if (!_Context.TryResolveAliveUnitAtCell(lCoord, out _))
                    pReachableCells.Add(lCoord);
            }
        }

        public void ClearSelection()
        {
            _SelectedPlacementUnitId = UnitId.None;
        }

        #endregion

        #region _____________________________| STATE

        public bool TryGetSelectedUnit(out UnitRuntime pUnit)
        {
            pUnit = null;
            return _SelectedPlacementUnitId.IsValid
                && _Context.Bootstrap?.BattleService != null
                && _Context.Bootstrap.BattleService.TryGetUnit(_SelectedPlacementUnitId, out pUnit)
                && pUnit != null
                && pUnit.IsAlive
                && _Context.Bootstrap.CanLocalPlayerControlUnit(pUnit);
        }

        public string ResolveModeMessage()
        {
            return TryGetSelectedUnit(out UnitRuntime lSelectedUnit)
                ? $"Mode: Placement. Selected {lSelectedUnit.Definition.DisplayName}. Click an empty blue spawn cell, then Ready."
                : "Mode: Placement. Click one of your units, place it on an empty blue spawn cell, then Ready.";
        }

        #endregion
    }
}
