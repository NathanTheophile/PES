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
        private sealed class ResolvedSkillTarget
        {
            public GridCoord TargetCell;
            public BattleUnitRuntime PrimaryTargetUnit;
            public List<GridCoord> AffectedCells = new List<GridCoord>();
            public List<BattleUnitRuntime> AffectedUnits = new List<BattleUnitRuntime>();
            public List<string> UsageTargetKeys = new List<string>();
        }

        #region _____________________________| SKILLS

        public BattleActionResult Validate(BattleUnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget, SkillExecutionContext pContext)
        {
            return TryResolveTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation)
                ? BattleActionResult.Succeeded(BattleActionType.Skill, "Skill can be used.", ResolveAffectedUnitIds(pActor, lResolvedTarget))
                : lValidation;
        }

        public BattleActionResult Execute(BattleUnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget, SkillExecutionContext pContext)
        {
            if (!TryResolveTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation))
                return lValidation;

            if (pSkill.TargetType != SkillTargetType.Self)
                pActor.FaceTowards(lResolvedTarget.TargetCell);

            BattleActionResult lResult = ApplyEffect(pActor, pSkill, lResolvedTarget, pContext);
            if (lResult.IsSuccess)
                pActor.RegisterSkillUse(pSkill, lResolvedTarget.UsageTargetKeys);

            return lResult;
        }

        #endregion

        #region _____________________________| HELPERS

        private static bool TryResolveTarget(
            BattleUnitRuntime pActor,
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

            if (!ValidateTargetGeometry(pActor, pSkill, lResolvedTarget.TargetCell, pContext, out pFailure))
            {
                pResolvedTarget = null;
                return false;
            }

            PopulateAffectedCells(pSkill, lResolvedTarget.TargetCell, pContext, lResolvedTarget.AffectedCells);
            PopulateAffectedUnits(pActor, pSkill, pContext, lResolvedTarget.AffectedCells, lResolvedTarget.AffectedUnits);
            PopulateUsageTargetKeys(pActor, pTarget, lResolvedTarget);

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
            BattleUnitRuntime pActor,
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

            if (pTarget.TargetType != pSkill.TargetType)
                return BattleActionResult.Failed(BattleActionType.Skill, "Target type is incompatible with the skill.");

            return BattleActionResult.Succeeded(BattleActionType.Skill, "Skill inputs are valid.", new[] { pActor.Id });
        }

        private static bool TryResolvePrimaryTarget(
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            SkillExecutionContext pContext,
            out ResolvedSkillTarget pResolvedTarget,
            out BattleActionResult pFailure)
        {
            pResolvedTarget = new ResolvedSkillTarget();

            switch (pSkill.TargetType)
            {
                case SkillTargetType.Self:
                    pResolvedTarget.PrimaryTargetUnit = pActor;
                    pResolvedTarget.TargetCell = pActor.Position;
                    pFailure = null;
                    return true;

                case SkillTargetType.Unit:
                    if (!pContext.TryGetUnit(pTarget.UnitId, out BattleUnitRuntime lTargetUnit) || !lTargetUnit.IsAlive)
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Target unit is invalid.");
                        return false;
                    }

                    pResolvedTarget.PrimaryTargetUnit = lTargetUnit;
                    pResolvedTarget.TargetCell = lTargetUnit.Position;
                    pFailure = null;
                    return true;

                case SkillTargetType.Cell:
                    if (!pContext.GridService.IsInside(pTarget.Cell))
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Target cell is outside the board.");
                        return false;
                    }

                    pResolvedTarget.TargetCell = pTarget.Cell;
                    pResolvedTarget.PrimaryTargetUnit = TryResolveOccupantAtCell(pContext, pTarget.Cell);
                    pFailure = null;
                    return true;

                default:
                    pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Unsupported skill target type.");
                    return false;
            }
        }

        private static bool ValidateTargetGeometry(
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            GridCoord pTargetCell,
            SkillExecutionContext pContext,
            out BattleActionResult pFailure)
        {
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

            if (!MatchesAlignment(pActor.Position, pTargetCell, pSkill.TargetAlignment))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, ResolveAlignmentFailureMessage(pSkill.TargetAlignment));
                return false;
            }

            if (pSkill.RequiresLineOfSight && pActor.Position != pTargetCell && !HasLineOfSight(pActor.Position, pTargetCell, pContext))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Target is not in line of sight.");
                return false;
            }

            pFailure = null;
            return true;
        }

        private static bool ValidateEffectSpecificTargeting(
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext,
            out BattleActionResult pFailure)
        {
            switch (pSkill.EffectType)
            {
                case SkillEffectType.Damage:
                case SkillEffectType.Heal:
                case SkillEffectType.Push:
                    if (pResolvedTarget.AffectedUnits.Count == 0)
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "No unit is affected by this skill.");
                        return false;
                    }

                    pFailure = null;
                    return true;

                case SkillEffectType.SwitchPositions:
                    if (pResolvedTarget.PrimaryTargetUnit == null || pResolvedTarget.PrimaryTargetUnit.Id == pActor.Id)
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "A valid target unit is required to switch positions.");
                        return false;
                    }

                    pFailure = null;
                    return true;

                case SkillEffectType.Teleport:
                    if (!pContext.GridService.CanUnitOccupy(pActor.Id, pResolvedTarget.TargetCell))
                    {
                        pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Destination cell is not valid for teleportation.");
                        return false;
                    }

                    pFailure = null;
                    return true;

                case SkillEffectType.Summon:
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

                case SkillEffectType.CreateGlyph:
                    pFailure = null;
                    return true;

                default:
                    pFailure = BattleActionResult.Failed(BattleActionType.Skill, "Unsupported skill effect.");
                    return false;
            }
        }

        private static BattleActionResult ApplyEffect(
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            switch (pSkill.EffectType)
            {
                case SkillEffectType.Damage:
                    return ApplyDamage(pActor, pSkill, pResolvedTarget);

                case SkillEffectType.Heal:
                    return ApplyHeal(pActor, pSkill, pResolvedTarget);

                case SkillEffectType.Push:
                    return ApplyPush(pActor, pSkill, pResolvedTarget, pContext);

                case SkillEffectType.Teleport:
                    return ApplyTeleport(pActor, pResolvedTarget, pContext);

                case SkillEffectType.SwitchPositions:
                    return ApplySwitch(pActor, pResolvedTarget, pContext);

                case SkillEffectType.Summon:
                    return ApplySummon(pActor, pSkill, pResolvedTarget, pContext);

                case SkillEffectType.CreateGlyph:
                    return ApplyGlyph(pActor, pSkill, pResolvedTarget, pContext);

                default:
                    return BattleActionResult.Failed(BattleActionType.Skill, "Unsupported skill effect.");
            }
        }

        private static BattleActionResult ApplyDamage(BattleUnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedUnitCount = 0;
            int lTotalValue = 0;
            List<BattleUnitId> lAffectedUnitIds = new List<BattleUnitId> { pActor.Id };

            foreach (BattleUnitRuntime lTarget in pResolvedTarget.AffectedUnits)
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

        private static BattleActionResult ApplyHeal(BattleUnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedUnitCount = 0;
            int lTotalValue = 0;
            List<BattleUnitId> lAffectedUnitIds = new List<BattleUnitId> { pActor.Id };

            foreach (BattleUnitRuntime lTarget in pResolvedTarget.AffectedUnits)
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
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            int lMovedUnits = 0;
            int lDamagedUnits = 0;
            int lTotalCollisionDamage = 0;
            List<BattleUnitId> lAffectedUnitIds = new List<BattleUnitId> { pActor.Id };

            foreach (BattleUnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive || lTarget.Id == pActor.Id)
                    continue;

                GridCoord lPushDirection = ResolvePushDirection(pActor.Position, lTarget.Position);
                if (lPushDirection.X == 0 && lPushDirection.Y == 0)
                    continue;

                PushResolution lResolution = ResolvePushDestination(lTarget, lPushDirection, pSkill.PushDistance, pContext);
                int lCollisionDamage = lResolution.BlockedSteps > 0
                    ? lResolution.BlockedSteps + (pActor != null && pActor.Definition != null ? pActor.Definition.PushDamageBonus : 0)
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
                }

                if (lResolution.Destination == lTarget.Position)
                    continue;

                if (!pContext.TryRelocateUnit(lTarget, lResolution.Destination))
                    continue;

                lMovedUnits++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            if (lMovedUnits == 0 && lDamagedUnits == 0)
                return BattleActionResult.Failed(BattleActionType.Skill, "Push could not move any target.");

            string lMessage = $"{pActor.Definition.DisplayName} pushed {lMovedUnits} unit(s).";
            if (lDamagedUnits > 0)
                lMessage += $" Collision dealt {lTotalCollisionDamage} damage across {lDamagedUnits} unit(s).";

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds);
        }

        private static BattleActionResult ApplyTeleport(
            BattleUnitRuntime pActor,
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
            BattleUnitRuntime pActor,
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
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            BattleUnitRuntime lSummonedUnit = pContext.TrySummonUnit(pSkill.SummonUnit, pSkill.SummonTeamRule, pActor, pResolvedTarget.TargetCell);
            if (lSummonedUnit == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Summon failed.");

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} summoned {lSummonedUnit.Definition.DisplayName}.",
                new[] { pActor.Id, lSummonedUnit.Id });
        }

        private static BattleActionResult ApplyGlyph(
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
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
                    lGlyphSkillId));
                lGlyphCount++;
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} placed {lGlyphCount} glyph(s).",
                new[] { pActor.Id });
        }

        private static string ResolveEffectMessage(
            BattleUnitRuntime pActor,
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

        private static IEnumerable<BattleUnitId> ResolveAffectedUnitIds(BattleUnitRuntime pActor, ResolvedSkillTarget pResolvedTarget)
        {
            HashSet<BattleUnitId> lAffectedIds = new HashSet<BattleUnitId> { pActor.Id };

            if (pResolvedTarget != null)
            {
                for (int lIndex = 0; lIndex < pResolvedTarget.AffectedUnits.Count; lIndex++)
                {
                    BattleUnitRuntime lUnit = pResolvedTarget.AffectedUnits[lIndex];
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

                    if (!IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lSize))
                        continue;

                    pCells.Add(lCandidate);
                }
            }
        }

        private static void PopulateAffectedUnits(
            BattleUnitRuntime pActor,
            SkillDefinition pSkill,
            SkillExecutionContext pContext,
            IEnumerable<GridCoord> pCells,
            ICollection<BattleUnitRuntime> pUnits)
        {
            if (pCells == null || pUnits == null)
                return;

            pUnits.Clear();
            HashSet<BattleUnitId> lVisitedUnits = new HashSet<BattleUnitId>();

            foreach (GridCoord lCell in pCells)
            {
                if (!pContext.GridService.TryGetOccupant(lCell, out BattleUnitId lOccupantId)
                    || !pContext.TryGetUnit(lOccupantId, out BattleUnitRuntime lUnit)
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

        private static void PopulateUsageTargetKeys(BattleUnitRuntime pActor, SkillTarget pTarget, ResolvedSkillTarget pResolvedTarget)
        {
            pResolvedTarget.UsageTargetKeys.Clear();

            HashSet<string> lKeys = new HashSet<string>();
            for (int lIndex = 0; lIndex < pResolvedTarget.AffectedUnits.Count; lIndex++)
            {
                BattleUnitRuntime lUnit = pResolvedTarget.AffectedUnits[lIndex];
                if (lUnit != null)
                    lKeys.Add($"unit:{lUnit.Id.Value}");
            }

            if (lKeys.Count == 0)
            {
                switch (pTarget.TargetType)
                {
                    case SkillTargetType.Self:
                        lKeys.Add($"unit:{pActor.Id.Value}");
                        break;

                    case SkillTargetType.Unit:
                        lKeys.Add($"unit:{pTarget.UnitId.Value}");
                        break;

                    case SkillTargetType.Cell:
                        lKeys.Add($"cell:{pTarget.Cell.X}:{pTarget.Cell.Y}");
                        break;
                }
            }

            foreach (string lKey in lKeys)
                pResolvedTarget.UsageTargetKeys.Add(lKey);
        }

        private static BattleUnitRuntime TryResolveOccupantAtCell(SkillExecutionContext pContext, GridCoord pCell)
        {
            return pContext.GridService.TryGetOccupant(pCell, out BattleUnitId lOccupantId)
                && pContext.TryGetUnit(lOccupantId, out BattleUnitRuntime lOccupant)
                && lOccupant != null
                && lOccupant.IsAlive
                ? lOccupant
                : null;
        }

        private sealed class PushResolution
        {
            public GridCoord Destination;
            public int BlockedSteps;
        }

        private static PushResolution ResolvePushDestination(BattleUnitRuntime pTarget, GridCoord pDirection, int pDistance, SkillExecutionContext pContext)
        {
            GridCoord lCurrent = pTarget.Position;
            int lMaxDistance = Math.Max(0, pDistance);
            int lBlockedSteps = 0;

            for (int lStep = 0; lStep < lMaxDistance; lStep++)
            {
                GridCoord lCandidate = new GridCoord(lCurrent.X + pDirection.X, lCurrent.Y + pDirection.Y);
                if (!pContext.GridService.CanUnitOccupy(pTarget.Id, lCandidate))
                {
                    lBlockedSteps = lMaxDistance - lStep;
                    break;
                }

                lCurrent = lCandidate;
            }

            return new PushResolution
            {
                Destination = lCurrent,
                BlockedSteps = lBlockedSteps
            };
        }

        private static int ResolveDirectionalDamageModifier(BattleUnitRuntime pActor, BattleUnitRuntime pTarget, SkillDefinition pSkill)
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

            if (lDeltaX == 0 && lDeltaY == 0)
                return new GridCoord(0, 0);

            return new GridCoord(Math.Sign(lDeltaX), Math.Sign(lDeltaY));
        }

        private static bool MatchesAlignment(GridCoord pOrigin, GridCoord pTarget, SkillTargetAlignment pAlignment)
        {
            if (pOrigin == pTarget || pAlignment == SkillTargetAlignment.Any)
                return true;

            int lDeltaX = Math.Abs(pTarget.X - pOrigin.X);
            int lDeltaY = Math.Abs(pTarget.Y - pOrigin.Y);

            return pAlignment switch
            {
                SkillTargetAlignment.Orthogonal => pOrigin.X == pTarget.X || pOrigin.Y == pTarget.Y,
                SkillTargetAlignment.Diagonal => lDeltaX == lDeltaY,
                _ => true
            };
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

        private static bool HasLineOfSight(GridCoord pOrigin, GridCoord pTarget, SkillExecutionContext pContext)
        {
            int lX0 = pOrigin.X;
            int lY0 = pOrigin.Y;
            int lX1 = pTarget.X;
            int lY1 = pTarget.Y;
            int lDeltaX = Math.Abs(lX1 - lX0);
            int lDeltaY = Math.Abs(lY1 - lY0);
            int lStepX = lX0 < lX1 ? 1 : -1;
            int lStepY = lY0 < lY1 ? 1 : -1;
            int lError = lDeltaX - lDeltaY;
            int lX = lX0;
            int lY = lY0;

            while (lX != lX1 || lY != lY1)
            {
                int lDoubleError = lError * 2;

                if (lDoubleError > -lDeltaY)
                {
                    lError -= lDeltaY;
                    lX += lStepX;
                }

                if (lDoubleError < lDeltaX)
                {
                    lError += lDeltaX;
                    lY += lStepY;
                }

                if (lX == lX1 && lY == lY1)
                    break;

                GridCoord lCell = new GridCoord(lX, lY);
                if (!pContext.GridService.IsInside(lCell) || !pContext.GridService.IsWalkable(lCell) || pContext.GridService.IsOccupied(lCell))
                    return false;
            }

            return true;
        }

        private static bool IsInsideAreaShape(SkillAoeShape pShape, int pOffsetX, int pOffsetY, int pSize)
        {
            switch (pShape)
            {
                case SkillAoeShape.Circle:
                    return Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize;

                case SkillAoeShape.Cross:
                    return (pOffsetX == 0 || pOffsetY == 0) && Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize;

                default:
                    return pOffsetX == 0 && pOffsetY == 0;
            }
        }

        #endregion
    }
}
