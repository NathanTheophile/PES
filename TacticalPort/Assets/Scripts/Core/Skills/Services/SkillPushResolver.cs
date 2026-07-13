#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class SkillPushResolver
    {
        private const int PushCollisionDamagePerBlockedStep = 5;

        public static BattleActionResult ApplyPush(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            int lProcessedTargets = 0;
            int lMovedUnits = 0;
            int lDamagedUnits = 0;
            int lTotalCollisionDamage = 0;
            HashSet<UnitId> lAffectedUnitIds = new HashSet<UnitId> { pActor.Id };
            int lPushDamageBonus = pActor?.Definition != null ? pActor.Definition.PushDamageBonus : 0;

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive || lTarget.Id == pActor.Id)
                    continue;

                lProcessedTargets++;
                GridCoord lPushDirection = ResolvePushDirection(pActor.Position, lTarget.Position);
                if (lPushDirection.X == 0 && lPushDirection.Y == 0)
                    continue;

                PushResolution lResolution = ResolvePushDestination(lTarget, lPushDirection, pSkill.PushDistance, pContext);
                int lCollisionDamage = lResolution.BlockedSteps > 0
                    ? lResolution.BlockedSteps * (PushCollisionDamagePerBlockedStep + lPushDamageBonus)
                    : 0;
                if (lCollisionDamage > 0)
                {
                    int lResolvedDamage = lTarget.ApplyDamage(lCollisionDamage);
                    if (lResolvedDamage > 0)
                    {
                        lDamagedUnits++;
                        lTotalCollisionDamage += lResolvedDamage;
                        lAffectedUnitIds.Add(lTarget.Id);
                    }

                    for (int lIndex = 0; lIndex < lResolution.BlockingUnitIds.Count; lIndex++)
                    {
                        UnitId lBlockingUnitId = lResolution.BlockingUnitIds[lIndex];
                        if (!pContext.TryGetUnit(lBlockingUnitId, out UnitRuntime lBlockingUnit) || lBlockingUnit == null || !lBlockingUnit.IsAlive)
                            continue;

                        int lBlockerDamage = lBlockingUnit.ApplyDamage(lCollisionDamage);
                        if (lBlockerDamage <= 0)
                            continue;

                        lDamagedUnits++;
                        lTotalCollisionDamage += lBlockerDamage;
                        lAffectedUnitIds.Add(lBlockingUnit.Id);
                    }
                }

                if (lResolution.Destination == lTarget.Position)
                    continue;

                if (!pContext.TryRelocateUnit(lTarget, lResolution.Destination))
                    continue;

                lMovedUnits++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            if (lProcessedTargets == 0)
                return BattleActionResult.Succeeded(BattleActionType.Skill, string.Empty, lAffectedUnitIds);

            string lMessage = lMovedUnits > 0
                ? $"{pActor.Definition.DisplayName} pushed {lMovedUnits} unit(s)."
                : $"{pActor.Definition.DisplayName} slammed the target(s) against an obstacle.";
            if (lDamagedUnits > 0)
                lMessage += $" Collision dealt {lTotalCollisionDamage} damage across {lDamagedUnits} unit(s).";

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds,
                null,
                lTotalCollisionDamage > 0 ? BattleActionOutcomeFlags.CollisionDamage : BattleActionOutcomeFlags.None);
        }

        private static PushResolution ResolvePushDestination(UnitRuntime pTarget, GridCoord pDirection, int pDistance, SkillExecutionContext pContext)
        {
            GridCoord lCurrent = pTarget.Position;
            int lMaxDistance = Math.Max(0, pDistance);
            int lBlockedSteps = 0;
            HashSet<UnitId> lBlockingUnitIds = new HashSet<UnitId>();

            for (int lStep = 0; lStep < lMaxDistance; lStep++)
            {
                GridCoord lCandidate = new GridCoord(lCurrent.X + pDirection.X, lCurrent.Y + pDirection.Y);
                if (!CanOccupyDuringPush(pContext, pTarget.Id, pTarget.OccupiedCellOffsets, lCandidate, lBlockingUnitIds))
                {
                    lBlockedSteps = lMaxDistance - lStep;
                    break;
                }

                lCurrent = lCandidate;
            }

            return new PushResolution
            {
                Destination = lCurrent,
                BlockedSteps = lBlockedSteps,
                BlockingUnitIds = new List<UnitId>(lBlockingUnitIds)
            };
        }

        private static bool CanOccupyDuringPush(
            SkillExecutionContext pContext,
            UnitId pUnitId,
            IReadOnlyList<GridCoord> pFootprintOffsets,
            GridCoord pAnchor,
            ISet<UnitId> pBlockingUnitIds)
        {
            if (pContext == null || pContext.GridService == null)
                return false;

            IReadOnlyList<GridCoord> lOffsets = pFootprintOffsets;
            if (lOffsets == null || lOffsets.Count == 0)
                lOffsets = new[] { new GridCoord(0, 0) };

            for (int lIndex = 0; lIndex < lOffsets.Count; lIndex++)
            {
                GridCoord lOffset = lOffsets[lIndex];
                GridCoord lCell = new GridCoord(pAnchor.X + lOffset.X, pAnchor.Y + lOffset.Y);

                if (!pContext.GridService.IsInside(lCell) || !pContext.GridService.IsWalkable(lCell))
                    return false;

                if (!pContext.GridService.TryGetOccupant(lCell, out UnitId lOccupantId) || lOccupantId == pUnitId)
                    continue;

                pBlockingUnitIds?.Add(lOccupantId);
                return false;
            }

            return true;
        }

        private static GridCoord ResolvePushDirection(GridCoord pOrigin, GridCoord pTarget)
        {
            int lDeltaX = pTarget.X - pOrigin.X;
            int lDeltaY = pTarget.Y - pOrigin.Y;
            int lAbsX = Math.Abs(lDeltaX);
            int lAbsY = Math.Abs(lDeltaY);

            if (lDeltaX == 0 && lDeltaY == 0)
                return new GridCoord(0, 0);

            if (lAbsX == lAbsY)
                return new GridCoord(Math.Sign(lDeltaX), Math.Sign(lDeltaY));

            if (lAbsX > lAbsY)
                return new GridCoord(Math.Sign(lDeltaX), 0);

            return new GridCoord(0, Math.Sign(lDeltaY));
        }

        private sealed class PushResolution
        {
            public GridCoord Destination;
            public int BlockedSteps;
            public List<UnitId> BlockingUnitIds = new List<UnitId>();
        }
    }
}
