#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    internal static class SkillTargetResolver
    {
        public static bool TryResolveTarget(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            SkillExecutionContext pContext,
            out ResolvedSkillTarget pResolvedTarget,
            out BattleActionResult pFailure)
        {
            BattleActionResult lBaseValidation = ValidateBaseInputs(pActor, pSkill, pTarget, pContext);
            if (!lBaseValidation.IsSuccess)
            {
                pResolvedTarget = null;
                pFailure = lBaseValidation;
                return false;
            }

            if (!TryResolvePrimaryTarget(pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out pFailure))
            {
                pResolvedTarget = null;
                return false;
            }

            if (!ValidateTargetGeometry(pActor, pSkill, lResolvedTarget, pContext, out pFailure))
            {
                pResolvedTarget = null;
                return false;
            }

            PopulateAffectedCells(pActor, pSkill, lResolvedTarget.TargetCell, pContext, lResolvedTarget.AffectedCells);
            PopulateAffectedUnits(pActor, pSkill, pContext, lResolvedTarget.AffectedCells, lResolvedTarget.AffectedUnits);
            PopulateUsageTargetKeys(lResolvedTarget);

            if (!ValidateEffectSpecificTargeting(pActor, pSkill, lResolvedTarget, pContext, out pFailure))
            {
                pResolvedTarget = null;
                return false;
            }

            if (!pActor.TryValidateSkillUsage(pSkill, lResolvedTarget.UsageTargetKeys, out string lUsageFailureReason))
            {
                pResolvedTarget = null;
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, lUsageFailureReason);
                return false;
            }

            pResolvedTarget = lResolvedTarget;
            pFailure = null;
            return true;
        }

        public static IEnumerable<UnitId> ResolveAffectedUnitIds(UnitId pActorId, ResolvedSkillTarget pResolvedTarget)
        {
            HashSet<UnitId> lAffectedIds = new HashSet<UnitId> { pActorId };

            if (pResolvedTarget != null)
            {
                for (int lIndex = 0; lIndex < pResolvedTarget.AffectedUnits.Count; lIndex++)
                {
                    UnitRuntime lUnit = pResolvedTarget.AffectedUnits[lIndex];
                    if (lUnit != null && lUnit.Id.IsValid)
                        lAffectedIds.Add(lUnit.Id);
                }
            }

            return lAffectedIds;
        }

        private static BattleActionResult ValidateBaseInputs(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            SkillExecutionContext pContext)
        {
            if (pActor == null || !pActor.IsAlive)
                return BattleActionResult.Failed(BattleActionType.Skill, "Actor is not available.");

            if (pSkill == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Skill is missing.");

            if (pTarget == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Target is missing.");

            if (pContext == null || pContext.GridService == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Skill context is missing.");

            if (pActor.RemainingActionPoints < pSkill.ActionPointCost)
                return BattleActionResult.Failed(BattleActionType.Skill, "Not enough action points.");

            return BattleActionResult.Succeeded(BattleActionType.Skill, "Skill inputs are valid.", new[] { pActor.Id });
        }

        private static bool TryResolvePrimaryTarget(
            SkillTarget pTarget,
            SkillExecutionContext pContext,
            out ResolvedSkillTarget pResolvedTarget,
            out BattleActionResult pFailure)
        {
            pResolvedTarget = new ResolvedSkillTarget();

            if (!pContext.GridService.IsInside(pTarget.Cell))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Target cell is outside the board.");
                return false;
            }

            pResolvedTarget.TargetCell = pTarget.Cell;
            pResolvedTarget.PrimaryTargetUnit = TryResolveOccupantAtCell(pContext, pTarget.Cell);
            pFailure = null;
            return true;
        }

        private static bool ValidateTargetGeometry(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext,
            out BattleActionResult pFailure)
        {
            GridCoord pTargetCell = pResolvedTarget.TargetCell;
            int lDistance = pActor.Position.ManhattanDistanceTo(pTargetCell);
            int lRangeMin = pActor.GetSkillRangeMin(pSkill);
            int lRangeMax = pActor.GetSkillRangeMax(pSkill);

            if (lDistance < lRangeMin || lDistance > lRangeMax)
            {
                pFailure = BattleActionResult.Failed(
                    BattleActionType.Skill,
                    $"Target is out of range ({lRangeMin}-{lRangeMax}).");
                return false;
            }

            if (!GridLineOfSightUtility.MatchesAlignment(pActor.Position, pTargetCell, pSkill.TargetAlignment))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, ResolveAlignmentFailureMessage(pSkill.TargetAlignment));
                return false;
            }

            if (pSkill.RequiresLineOfSight
                && pActor.Position != pTargetCell
                && !HasLineOfSight(pActor, pTargetCell, pResolvedTarget.PrimaryTargetUnit, pContext))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Target is not in line of sight.");
                return false;
            }

            pFailure = null;
            return true;
        }

        private static bool ValidateEffectSpecificTargeting(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext,
            out BattleActionResult pFailure)
        {
            bool lRequiresAffectedUnits = pSkill.PrimaryEffectType == SkillPrimaryEffectType.Damage
                || pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Push;

            if (lRequiresAffectedUnits && pResolvedTarget.AffectedUnits.Count == 0)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, "No unit is affected by this skill.");
                return false;
            }

            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.SwitchPositions:
                    if (pResolvedTarget.PrimaryTargetUnit == null || pResolvedTarget.PrimaryTargetUnit.Id == pActor.Id)
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "A valid target unit is required to switch positions.");
                        return false;
                    }

                    pFailure = null;
                    return true;

                case SkillAdditionalEffectType.Teleport:
                    if (!pContext.GridService.CanUnitOccupy(pActor.Id, pResolvedTarget.TargetCell))
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Destination cell is not valid for teleportation.");
                        return false;
                    }

                    pFailure = null;
                    return true;

                case SkillAdditionalEffectType.Summon:
                    if (pSkill.SummonUnit == null)
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Summon skill is missing a unit definition.");
                        return false;
                    }

                    if (pContext.GridService.IsOccupied(pResolvedTarget.TargetCell))
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Target cell is occupied.");
                        return false;
                    }

                    pFailure = null;
                    return true;

                case SkillAdditionalEffectType.CreateGlyph:
                    pFailure = null;
                    return true;

                default:
                    pFailure = null;
                    return true;
            }
        }

        private static void PopulateAffectedCells(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            GridCoord pOrigin,
            SkillExecutionContext pContext,
            ICollection<GridCoord> pCells)
        {
            if (pCells == null)
                return;

            pCells.Clear();

            int lSize = pSkill != null ? pSkill.AoeSize : 0;
            if (pSkill == null || pSkill.AoeShape == SkillAoeShape.Single || lSize <= 0)
            {
                if (pContext.GridService.IsInside(pOrigin))
                    pCells.Add(pOrigin);

                return;
            }

            GridCoord lDirection = pActor != null
                ? GridLineOfSightUtility.ResolveAreaDirection(pActor.Position, pOrigin)
                : new GridCoord(0, 1);

            for (int lOffsetY = -lSize; lOffsetY <= lSize; lOffsetY++)
            {
                for (int lOffsetX = -lSize; lOffsetX <= lSize; lOffsetX++)
                {
                    GridCoord lCandidate = new GridCoord(pOrigin.X + lOffsetX, pOrigin.Y + lOffsetY);
                    if (!pContext.GridService.IsInside(lCandidate))
                        continue;

                    if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lSize, lDirection))
                        continue;

                    pCells.Add(lCandidate);
                }
            }
        }

        private static void PopulateAffectedUnits(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            SkillExecutionContext pContext,
            IEnumerable<GridCoord> pCells,
            ICollection<UnitRuntime> pUnits)
        {
            if (pCells == null || pUnits == null)
                return;

            pUnits.Clear();
            HashSet<UnitId> lVisitedUnits = new HashSet<UnitId>();

            foreach (GridCoord lCell in pCells)
            {
                if (!pContext.GridService.TryGetOccupant(lCell, out UnitId lOccupantId)
                    || !pContext.TryGetUnit(lOccupantId, out UnitRuntime lUnit)
                    || lUnit == null
                    || !lUnit.IsAlive
                    || (!pSkill.CanAffectCaster && pActor != null && lUnit.Id == pActor.Id)
                    || !lVisitedUnits.Add(lUnit.Id))
                {
                    continue;
                }

                pUnits.Add(lUnit);
            }
        }

        private static void PopulateUsageTargetKeys(ResolvedSkillTarget pResolvedTarget)
        {
            pResolvedTarget.UsageTargetKeys.Clear();

            HashSet<string> lKeys = new HashSet<string>();
            for (int lIndex = 0; lIndex < pResolvedTarget.AffectedUnits.Count; lIndex++)
            {
                UnitRuntime lUnit = pResolvedTarget.AffectedUnits[lIndex];
                if (lUnit != null)
                    lKeys.Add($"unit:{lUnit.Id.Value}");
            }

            if (lKeys.Count == 0)
            {
                if (pResolvedTarget.PrimaryTargetUnit != null)
                    lKeys.Add($"unit:{pResolvedTarget.PrimaryTargetUnit.Id.Value}");
                else
                    lKeys.Add($"cell:{pResolvedTarget.TargetCell.X}:{pResolvedTarget.TargetCell.Y}");
            }

            foreach (string lKey in lKeys)
                pResolvedTarget.UsageTargetKeys.Add(lKey);
        }

        private static UnitRuntime TryResolveOccupantAtCell(SkillExecutionContext pContext, GridCoord pCell)
        {
            return pContext.GridService.TryGetOccupant(pCell, out UnitId lOccupantId)
                && pContext.TryGetUnit(lOccupantId, out UnitRuntime lOccupant)
                && lOccupant != null
                && lOccupant.IsAlive
                ? lOccupant
                : null;
        }

        private static string ResolveAlignmentFailureMessage(SkillTargetAlignment pAlignment)
        {
            return pAlignment switch
            {
                SkillTargetAlignment.Orthogonal => "Target must be aligned horizontally or vertically.",
                SkillTargetAlignment.Diagonal => "Target must be aligned diagonally.",
                _ => "Target alignment is invalid."
            };
        }

        private static bool HasLineOfSight(UnitRuntime pActor, GridCoord pTarget, UnitRuntime pTargetUnit, SkillExecutionContext pContext)
        {
            return GridLineOfSightUtility.HasLineOfSight(
                pActor.Position,
                pTarget,
                pCell =>
                {
                    if (!pContext.GridService.IsInside(pCell) || pContext.GridService.BlocksLineOfSight(pCell))
                        return true;

                    if (!pContext.GridService.TryGetOccupant(pCell, out UnitId lOccupantId))
                        return false;

                    if (lOccupantId == pActor.Id)
                        return false;

                    if (pTargetUnit != null && lOccupantId == pTargetUnit.Id)
                        return pCell != pTarget;

                    return true;
                });
        }
    }
}
