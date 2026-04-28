#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.View
{
    internal sealed class CombatPreviewController
    {
        #region _____________________________/ VALUES

        private readonly CombatInteractionContext _Context;
        private readonly HashSet<GridCoord> _ReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _BlockedReachableCells = new HashSet<GridCoord>();

        #endregion

        #region _____________________________| INIT

        public CombatPreviewController(CombatInteractionContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| REACHABLE

        public bool IsReachable(GridCoord pCell) => _ReachableCells.Contains(pCell);

        public void SyncReachableCells(
            bool pIsPlacementPhaseActive,
            SkillSelectionController pSkillSelection,
            PlacementInteractionController pPlacementInteraction)
        {
            HashSet<GridCoord> lLatestReachableCells = new HashSet<GridCoord>();
            HashSet<GridCoord> lLatestBlockedReachableCells = new HashSet<GridCoord>();

            if (pIsPlacementPhaseActive)
            {
                pPlacementInteraction.FillReachableSpawnCells(lLatestReachableCells);
            }
            else if (pSkillSelection.HasSelectedSkill && _Context.Bootstrap.TryGetActiveUnit(out UnitRuntime lActiveUnit))
            {
                EnumerateSkillHighlightCells(
                    lActiveUnit,
                    pSkillSelection.SelectedSkill,
                    lLatestReachableCells,
                    lLatestBlockedReachableCells);
            }
            else
            {
                foreach (GridCoord lCoord in _Context.Bootstrap.GetReachableCellsForActiveUnit())
                    lLatestReachableCells.Add(lCoord);
            }

            bool lReachableUnchanged = _ReachableCells.SetEquals(lLatestReachableCells);
            bool lBlockedUnchanged = _BlockedReachableCells.SetEquals(lLatestBlockedReachableCells);
            if (lReachableUnchanged && lBlockedUnchanged)
                return;

            _ReachableCells.Clear();
            _BlockedReachableCells.Clear();

            foreach (GridCoord lCoord in lLatestReachableCells)
                _ReachableCells.Add(lCoord);

            foreach (GridCoord lCoord in lLatestBlockedReachableCells)
                _BlockedReachableCells.Add(lCoord);

            bool lHasSelectedSkill = pSkillSelection.HasSelectedSkill;
            _Context.BoardView.SetReachableCells(lHasSelectedSkill ? null : _ReachableCells);
            _Context.BoardView.SetSkillRangeCells(
                lHasSelectedSkill ? _ReachableCells : null,
                lHasSelectedSkill ? _BlockedReachableCells : null);
        }

        #endregion

        #region _____________________________| PREVIEW

        public void SyncPreviewCells(
            bool pIsPlacementPhaseActive,
            SkillSelectionController pSkillSelection,
            CombatHoverController pHoverController)
        {
            if (_Context.BoardView == null)
                return;

            UnitRuntime lActiveUnit = null;
            bool lHasActiveUnit = _Context.Bootstrap != null
                && _Context.Bootstrap.TryGetActiveUnit(out lActiveUnit);
            if (pIsPlacementPhaseActive
                || !lHasActiveUnit
                || lActiveUnit == null
                || !pSkillSelection.HasSelectedSkill
                || !pHoverController.HasHoveredCell)
            {
                _Context.BoardView.SetPreviewCells(null);
                return;
            }

            SkillTarget lTarget = SkillTarget.ForCell(pHoverController.HoveredCell);

            BattleActionResult lValidation = _Context.Bootstrap.ValidateActiveSkill(pSkillSelection.SelectedSkillId, lTarget);
            if (!lValidation.IsSuccess)
            {
                _Context.BoardView.SetPreviewCells(null);
                return;
            }

            _Context.BoardView.SetPreviewCells(BuildSkillPreviewCells(pSkillSelection.SelectedSkill, lTarget));
        }

        #endregion

        #region _____________________________| HELPERS

        private void EnumerateSkillHighlightCells(
            UnitRuntime pActiveUnit,
            SkillDefinition pSkill,
            ISet<GridCoord> pReachableCells,
            ISet<GridCoord> pBlockedReachableCells)
        {
            if (pActiveUnit == null || pSkill == null || _Context.BoardView == null || _Context.BoardView.Scenario == null)
                return;

            foreach (CellDefinition lCell in _Context.BoardView.Scenario.EnumerateCells())
            {
                GridCoord lCoord = lCell.Coordinate.ToRuntime();
                if (!TryClassifySkillCell(pActiveUnit, pSkill, lCoord, out bool lIsBlockedByLineOfSight))
                    continue;

                if (lIsBlockedByLineOfSight)
                    pBlockedReachableCells?.Add(lCoord);
                else
                    pReachableCells?.Add(lCoord);
            }
        }

        private IReadOnlyCollection<GridCoord> BuildSkillPreviewCells(SkillDefinition pSkill, SkillTarget pTarget)
        {
            List<GridCoord> lCells = new List<GridCoord>();
            if (pSkill == null || pTarget == null || _Context.BoardView == null)
                return lCells;

            GridCoord lOrigin = pTarget.Cell;
            int lSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : pSkill.AoeSize;
            GridCoord lDirection = _Context.Bootstrap.TryGetActiveUnit(out UnitRuntime lActiveUnit)
                ? GridLineOfSightUtility.ResolveAreaDirection(lActiveUnit.Position, lOrigin)
                : new GridCoord(0, 1);

            for (int lOffsetY = -lSize; lOffsetY <= lSize; lOffsetY++)
            {
                for (int lOffsetX = -lSize; lOffsetX <= lSize; lOffsetX++)
                {
                    if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lSize, lDirection))
                        continue;

                    GridCoord lCandidate = new GridCoord(lOrigin.X + lOffsetX, lOrigin.Y + lOffsetY);
                    if (_Context.BoardView.ContainsCell(lCandidate))
                        lCells.Add(lCandidate);
                }
            }

            return lCells;
        }

        private bool TryClassifySkillCell(
            UnitRuntime pActiveUnit,
            SkillDefinition pSkill,
            GridCoord pTargetCell,
            out bool pIsBlockedByLineOfSight)
        {
            pIsBlockedByLineOfSight = false;

            if (pActiveUnit == null || pSkill == null)
                return false;

            int lDistance = pActiveUnit.Position.ManhattanDistanceTo(pTargetCell);
            int lRangeMin = pActiveUnit.GetSkillRangeMin(pSkill);
            int lRangeMax = pActiveUnit.GetSkillRangeMax(pSkill);
            if (lDistance < lRangeMin || lDistance > lRangeMax)
                return false;

            if (!GridLineOfSightUtility.MatchesAlignment(pActiveUnit.Position, pTargetCell, pSkill.TargetAlignment))
                return false;

            if (!_Context.TryGetBoardCellDefinition(pTargetCell, out CellDefinition lCellDefinition) || !lCellDefinition.IsWalkable)
                return false;

            if (!pSkill.RequiresLineOfSight || pActiveUnit.Position == pTargetCell)
                return true;

            _Context.TryResolveAliveUnitAtCell(pTargetCell, out UnitRuntime lTargetUnit);
            if (HasLineOfSight(pActiveUnit, pTargetCell, lTargetUnit))
                return true;

            pIsBlockedByLineOfSight = true;
            return true;
        }

        private bool HasLineOfSight(UnitRuntime pSourceUnit, GridCoord pTarget, UnitRuntime pTargetUnit = null)
        {
            if (_Context.BoardView == null || pSourceUnit == null)
                return false;

            return GridLineOfSightUtility.HasLineOfSight(
                pSourceUnit.Position,
                pTarget,
                pCell =>
                {
                    if (!_Context.TryGetBoardCellDefinition(pCell, out CellDefinition lCell))
                        return true;

                    if (lCell.BlocksLineOfSight)
                        return true;

                    if (!_Context.TryResolveAliveUnitAtCell(pCell, out UnitRuntime lBlockingUnit))
                        return false;

                    if (lBlockingUnit.Id == pSourceUnit.Id)
                        return false;

                    if (pTargetUnit != null && lBlockingUnit.Id == pTargetUnit.Id)
                        return pCell != pTarget;

                    return true;
                });
        }

        #endregion
    }
}
