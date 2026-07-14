using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class SkillPullResolver
    {
        public static BattleActionResult ApplyPull(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            int lMovedUnits = 0;
            HashSet<UnitId> lAffectedUnitIds = new HashSet<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive || lTarget.Id == pActor.Id)
                    continue;

                GridCoord lDirection = ResolveDirection(lTarget.Position, pActor.Position);
                GridCoord lDestination = ResolveDestination(lTarget, lDirection, pSkill.PushDistance, pContext);
                if (lDestination == lTarget.Position || !pContext.TryRelocateUnit(lTarget, lDestination))
                    continue;

                lMovedUnits++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            string lMessage = lMovedUnits > 0
                ? $"{pActor.Definition.DisplayName} pulled {lMovedUnits} unit(s)."
                : string.Empty;
            return BattleActionResult.Succeeded(BattleActionType.Skill, lMessage, lAffectedUnitIds);
        }

        private static GridCoord ResolveDestination(
            UnitRuntime pTarget,
            GridCoord pDirection,
            int pDistance,
            SkillExecutionContext pContext)
        {
            GridCoord lCurrent = pTarget.Position;
            int lMaxDistance = Math.Max(0, pDistance);
            for (int lStep = 0; lStep < lMaxDistance; lStep++)
            {
                GridCoord lCandidate = new GridCoord(lCurrent.X + pDirection.X, lCurrent.Y + pDirection.Y);
                if (!CanOccupy(pTarget, lCandidate, pContext))
                    break;

                lCurrent = lCandidate;
            }

            return lCurrent;
        }

        private static bool CanOccupy(UnitRuntime pTarget, GridCoord pAnchor, SkillExecutionContext pContext)
        {
            IReadOnlyList<GridCoord> lOffsets = pTarget.OccupiedCellOffsets;
            for (int lIndex = 0; lIndex < lOffsets.Count; lIndex++)
            {
                GridCoord lCell = new GridCoord(pAnchor.X + lOffsets[lIndex].X, pAnchor.Y + lOffsets[lIndex].Y);
                if (!pContext.GridService.IsInside(lCell) || !pContext.GridService.IsWalkable(lCell))
                    return false;

                if (pContext.GridService.TryGetOccupant(lCell, out UnitId lOccupantId) && lOccupantId != pTarget.Id)
                    return false;
            }

            return true;
        }

        private static GridCoord ResolveDirection(GridCoord pOrigin, GridCoord pDestination)
        {
            int lDeltaX = pDestination.X - pOrigin.X;
            int lDeltaY = pDestination.Y - pOrigin.Y;
            int lAbsX = Math.Abs(lDeltaX);
            int lAbsY = Math.Abs(lDeltaY);
            if (lDeltaX == 0 && lDeltaY == 0)
                return new GridCoord(0, 0);
            if (lAbsX == lAbsY)
                return new GridCoord(Math.Sign(lDeltaX), Math.Sign(lDeltaY));
            return lAbsX > lAbsY
                ? new GridCoord(Math.Sign(lDeltaX), 0)
                : new GridCoord(0, Math.Sign(lDeltaY));
        }
    }
}
