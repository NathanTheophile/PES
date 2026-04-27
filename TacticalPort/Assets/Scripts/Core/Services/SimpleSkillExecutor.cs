using System;
using System.Collections.Generic;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    public sealed class SimpleSkillExecutor : ISkillExecutor
    {
        private const int PushCollisionDamagePerBlockedStep = 5;

        private sealed class ResolvedSkillTarget
        {
            public GridCoord TargetCell;
            public UnitRuntime PrimaryTargetUnit;
            public List<GridCoord> AffectedCells = new List<GridCoord>();
            public List<UnitRuntime> AffectedUnits = new List<UnitRuntime>();
            public List<string> UsageTargetKeys = new List<string>();
        }

        #region _____________________________| SKILLS

        public BattleActionResult Validate(UnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget, SkillExecutionContext pContext)
        {
            return TryResolveTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation)
                ? BattleActionResult.Succeeded(BattleActionType.Skill, "Skill can be used.", ResolveAffectedUnitIds(pActor, lResolvedTarget))
                : lValidation;
        }

        public BattleActionResult Execute(UnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget, SkillExecutionContext pContext)
        {
            if (!TryResolveTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation))
                return lValidation;

            if (pActor.Position != lResolvedTarget.TargetCell)
                pActor.FaceTowards(lResolvedTarget.TargetCell);

            BattleActionResult lResult = ApplyEffect(pActor, pSkill, lResolvedTarget, pContext);
            if (lResult.IsSuccess)
                pActor.RegisterSkillUse(pSkill, lResolvedTarget.UsageTargetKeys);

            return lResult;
        }

        #endregion

        #region _____________________________| HELPERS

        private static bool TryResolveTarget(
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

            if (!TryResolvePrimaryTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out pFailure))
            {
                pResolvedTarget = null;
                return false;
            }

            if (!ValidateTargetGeometry(pActor, pSkill, lResolvedTarget, pContext, out pFailure))
            {
                pResolvedTarget = null;
                return false;
            }

            PopulateAffectedCells(pSkill, lResolvedTarget.TargetCell, pContext, lResolvedTarget.AffectedCells);
            PopulateAffectedUnits(pActor, pSkill, pContext, lResolvedTarget.AffectedCells, lResolvedTarget.AffectedUnits);
            PopulateUsageTargetKeys(pActor, lResolvedTarget);

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
            UnitRuntime pActor,
            SkillDefinition pSkill,
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

        private static BattleActionResult ApplyEffect(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            List<BattleActionResult> lResults = new List<BattleActionResult>();

            switch (pSkill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    lResults.Add(ApplyDamage(pActor, pSkill, pResolvedTarget));
                    break;

                case SkillPrimaryEffectType.Heal:
                    lResults.Add(ApplyHeal(pActor, pSkill, pResolvedTarget));
                    break;
            }

            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.Push:
                    lResults.Add(ApplyPush(pActor, pSkill, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.Teleport:
                    lResults.Add(ApplyTeleport(pActor, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.SwitchPositions:
                    lResults.Add(ApplySwitch(pActor, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.Summon:
                    lResults.Add(ApplySummon(pActor, pSkill, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.CreateGlyph:
                    lResults.Add(ApplyGlyph(pActor, pSkill, pResolvedTarget, pContext));
                    break;
            }

            if (pSkill.AppliedState != null)
                lResults.Add(ApplyState(pActor, pSkill, pResolvedTarget));

            return CombineEffectResults(pActor, lResults);
        }

        private static BattleActionResult ApplyDamage(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedUnitCount = 0;
            int lTotalValue = 0;
            List<UnitId> lAffectedUnitIds = new List<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive)
                    continue;

                int lBaseDamage = Math.Max(0, pSkill.Power + ResolveDirectionalDamageModifier(pActor, lTarget, pSkill));
                int lResolvedDamage = pActor.ResolveOutgoingDamage(lBaseDamage);
                lTotalValue += lTarget.ApplyDamage(lResolvedDamage);
                lAffectedUnitCount++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                ResolveEffectMessage(pActor, pResolvedTarget, lAffectedUnitCount, lTotalValue, "dealt", "damage"),
                lAffectedUnitIds);
        }

        private static BattleActionResult ApplyHeal(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedUnitCount = 0;
            int lTotalValue = 0;
            List<UnitId> lAffectedUnitIds = new List<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive)
                    continue;

                lTotalValue += lTarget.RestoreHealth(pSkill.Power);
                lAffectedUnitCount++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                ResolveEffectMessage(pActor, pResolvedTarget, lAffectedUnitCount, lTotalValue, "restored", "health"),
                lAffectedUnitIds);
        }

        private static BattleActionResult ApplyPush(
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
                return BattleActionResult.Failed(BattleActionType.Skill, "Push could not move any target.");

            string lMessage = lMovedUnits > 0
                ? $"{pActor.Definition.DisplayName} pushed {lMovedUnits} unit(s)."
                : $"{pActor.Definition.DisplayName} slammed the target(s) against an obstacle.";
            if (lDamagedUnits > 0)
                lMessage += $" Collision dealt {lTotalCollisionDamage} damage across {lDamagedUnits} unit(s).";

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds);
        }

        private static BattleActionResult ApplyTeleport(
            UnitRuntime pActor,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            if (!pContext.TryRelocateUnit(pActor, pResolvedTarget.TargetCell))
                return BattleActionResult.Failed(BattleActionType.Skill, "Teleport failed.");

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} teleported to {pResolvedTarget.TargetCell}.",
                new[] { pActor.Id });
        }

        private static BattleActionResult ApplySwitch(
            UnitRuntime pActor,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            if (!pContext.TrySwapUnits(pActor, pResolvedTarget.PrimaryTargetUnit))
                return BattleActionResult.Failed(BattleActionType.Skill, "Position switch failed.");

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} switched positions with {pResolvedTarget.PrimaryTargetUnit.Definition.DisplayName}.",
                new[] { pActor.Id, pResolvedTarget.PrimaryTargetUnit.Id });
        }

        private static BattleActionResult ApplySummon(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            UnitRuntime lSummonedUnit = pContext.TrySummonUnit(pSkill.SummonUnit, pSkill.SummonTeamRule, pActor, pResolvedTarget.TargetCell);
            if (lSummonedUnit == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Summon failed.");

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} summoned {lSummonedUnit.Definition.DisplayName}.",
                new[] { pActor.Id, lSummonedUnit.Id });
        }

        private static BattleActionResult ApplyGlyph(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            string lGlyphGroupId = $"{pActor.Id.Value}:{pSkill.Id}:{pResolvedTarget.TargetCell.X}:{pResolvedTarget.TargetCell.Y}";
            int lGlyphCount = 0;
            foreach (GridCoord lCell in pResolvedTarget.AffectedCells)
            {
                string lGlyphSkillId = $"{pSkill.Id}:{lCell.X}:{lCell.Y}";
                pContext.AddGlyph(new GridGlyphRuntime(
                    pActor.Id,
                    pActor.Team,
                    lCell,
                    pSkill.Power,
                    pSkill.GlyphDurationTurns,
                    pSkill.GlyphTargetRule,
                    lGlyphSkillId,
                    lGlyphGroupId));
                lGlyphCount++;
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} placed {lGlyphCount} glyph(s).",
                new[] { pActor.Id });
        }

        private static BattleActionResult ApplyState(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedCount = 0;
            HashSet<UnitId> lAffectedUnitIds = new HashSet<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive)
                    continue;

                if (!lTarget.TryApplyState(pSkill.AppliedState, pSkill.AppliedStateStacks, pSkill.AppliedStateDurationTurns))
                    continue;

                lAffectedCount++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            string lMessage = lAffectedCount > 0
                ? $"{pActor.Definition.DisplayName} applied {pSkill.AppliedState.DisplayName} to {lAffectedCount} unit(s)."
                : string.Empty;

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds);
        }

        private static string ResolveEffectMessage(
            UnitRuntime pActor,
            ResolvedSkillTarget pResolvedTarget,
            int pAffectedUnitCount,
            int pTotalValue,
            string pVerb,
            string pResourceLabel)
        {
            if (pAffectedUnitCount == 1 && pResolvedTarget.AffectedUnits.Count > 0 && pResolvedTarget.AffectedUnits[0] != null)
                return $"{pActor.Definition.DisplayName} {pVerb} {pTotalValue} {pResourceLabel} to {pResolvedTarget.AffectedUnits[0].Definition.DisplayName}.";

            return $"{pActor.Definition.DisplayName} {pVerb} {pTotalValue} {pResourceLabel} across {pAffectedUnitCount} unit(s).";
        }

        private static IEnumerable<UnitId> ResolveAffectedUnitIds(UnitRuntime pActor, ResolvedSkillTarget pResolvedTarget)
        {
            HashSet<UnitId> lAffectedIds = new HashSet<UnitId> { pActor.Id };

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

        private static void PopulateAffectedCells(
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

            for (int lOffsetY = -lSize; lOffsetY <= lSize; lOffsetY++)
            {
                for (int lOffsetX = -lSize; lOffsetX <= lSize; lOffsetX++)
                {
                    GridCoord lCandidate = new GridCoord(pOrigin.X + lOffsetX, pOrigin.Y + lOffsetY);
                    if (!pContext.GridService.IsInside(lCandidate))
                        continue;

                    if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lSize))
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

        private static void PopulateUsageTargetKeys(UnitRuntime pActor, ResolvedSkillTarget pResolvedTarget)
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

        private sealed class PushResolution
        {
            public GridCoord Destination;
            public int BlockedSteps;
            public List<UnitId> BlockingUnitIds = new List<UnitId>();
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

        private static int ResolveDirectionalDamageModifier(UnitRuntime pActor, UnitRuntime pTarget, SkillDefinition pSkill)
        {
            if (pActor == null || pTarget == null || pSkill == null || !pSkill.UseDirectionalModifiers)
                return 0;

            GridCoord lRelativeDirection = ResolveCardinalDirection(pTarget.Position, pActor.Position);
            GridCoord lFacing = ResolveCardinalDirection(default, pTarget.FacingDirection);
            if (lRelativeDirection.X == 0 && lRelativeDirection.Y == 0)
                return pSkill.SideDamageModifier;

            if (lRelativeDirection == lFacing)
                return pSkill.FrontDamageModifier;

            if (lRelativeDirection == new GridCoord(-lFacing.X, -lFacing.Y))
                return pSkill.BackDamageModifier;

            return pSkill.SideDamageModifier;
        }

        private static GridCoord ResolveCardinalDirection(GridCoord pOrigin, GridCoord pTarget)
        {
            int lDeltaX = pTarget.X - pOrigin.X;
            int lDeltaY = pTarget.Y - pOrigin.Y;
            int lAbsX = Math.Abs(lDeltaX);
            int lAbsY = Math.Abs(lDeltaY);

            if (lAbsX == 0 && lAbsY == 0)
                return new GridCoord(0, 0);

            if (lAbsX >= lAbsY)
                return new GridCoord(Math.Sign(lDeltaX), 0);

            return new GridCoord(0, Math.Sign(lDeltaY));
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

        private static BattleActionResult CombineEffectResults(UnitRuntime pActor, List<BattleActionResult> pResults)
        {
            if (pResults == null || pResults.Count == 0)
                return BattleActionResult.Succeeded(BattleActionType.Skill, $"{pActor.Definition.DisplayName} used a skill.", new[] { pActor.Id });

            HashSet<UnitId> lAffectedIds = new HashSet<UnitId>();
            List<string> lMessages = new List<string>();

            foreach (BattleActionResult lResult in pResults)
            {
                if (lResult == null || !lResult.IsSuccess)
                    return lResult ?? BattleActionResult.Failed(BattleActionType.Skill, "Skill execution failed.");

                if (!string.IsNullOrWhiteSpace(lResult.Message))
                    lMessages.Add(lResult.Message);

                if (lResult.AffectedUnitIds == null)
                    continue;

                foreach (UnitId lAffectedId in lResult.AffectedUnitIds)
                {
                    if (lAffectedId.IsValid)
                        lAffectedIds.Add(lAffectedId);
                }
            }

            if (lAffectedIds.Count == 0)
                lAffectedIds.Add(pActor.Id);

            string lMessage = lMessages.Count > 0
                ? string.Join(" ", lMessages)
                : $"{pActor.Definition.DisplayName} used a skill.";

            return BattleActionResult.Succeeded(BattleActionType.Skill, lMessage, lAffectedIds);
        }

        #endregion
    }
}
