#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion




using TacticalPort.Data;
using TacticalPort.Shared;
using System.Collections.Generic;

namespace TacticalPort.Core
{
    internal static class SkillPlacementResolver
    {
        public static BattleActionResult ApplyTeleport(
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

        public static BattleActionResult ApplySwitch(
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

        public static BattleActionResult ApplySummon(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            UnitRuntime lSummonedUnit = pContext.TrySummonUnit(
                pSkill.SummonUnit,
                pSkill.SummonTeamRule,
                pActor,
                pResolvedTarget.TargetCell,
                pSkill.ReplaceOwnedSummonOfSameDefinition);
            if (lSummonedUnit == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Summon failed.");

            List<UnitId> lAffectedUnitIds = new List<UnitId> { pActor.Id, lSummonedUnit.Id };
            int lGlyphCount = ApplyAttachedGlyph(pActor, lSummonedUnit, pSkill, pContext);
            int lSpawnStateCount = ApplySummonSpawnState(pActor, pSkill, lSummonedUnit, pContext, lAffectedUnitIds);
            string lMessage = $"{pActor.Definition.DisplayName} summoned {lSummonedUnit.Definition.DisplayName}.";
            if (lGlyphCount > 0)
                lMessage += $" {lGlyphCount} glyph cell(s) created.";
            if (lSpawnStateCount > 0)
                lMessage += $" {pSkill.SummonSpawnState.DisplayName} applied to {lSpawnStateCount} unit(s).";

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds,
                null,
                BattleActionOutcomeFlags.Summon
                | (lGlyphCount > 0 ? BattleActionOutcomeFlags.Glyph : BattleActionOutcomeFlags.None)
                | (lSpawnStateCount > 0 ? BattleActionOutcomeFlags.StateApplied : BattleActionOutcomeFlags.None));
        }

        public static BattleActionResult ApplyGlyph(
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
                GridGlyphRuntime lGlyph = pSkill.GlyphDefinition != null
                    ? new GridGlyphRuntime(
                        pActor.Id,
                        pActor.Team,
                        lCell,
                        pSkill.GlyphDefinition,
                        pSkill.GlyphDurationTurns,
                        lGlyphSkillId,
                        lGlyphGroupId)
                    : new GridGlyphRuntime(
                        pActor.Id,
                        pActor.Team,
                        lCell,
                        pSkill.Power,
                        pSkill.GlyphDurationTurns,
                        pSkill.GlyphTargetRule,
                        lGlyphSkillId,
                        lGlyphGroupId,
                        pSkill.GlyphAppliedState,
                        pSkill.GlyphAppliedStateStacks,
                        pSkill.GlyphAppliedStateDurationTurns);
                pContext.AddGlyph(lGlyph);
                lGlyphCount++;
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} placed {lGlyphCount} glyph(s).",
                new[] { pActor.Id },
                null,
                lGlyphCount > 0 ? BattleActionOutcomeFlags.Glyph : BattleActionOutcomeFlags.None);
        }

        private static int ApplyAttachedGlyph(
            UnitRuntime pActor,
            UnitRuntime pSummonedUnit,
            SkillDefinition pSkill,
            SkillExecutionContext pContext)
        {
            GlyphDefinition lDefinition = pSummonedUnit?.Definition?.AttachedGlyph;
            if (pActor == null || lDefinition == null || pContext?.GridService == null)
                return 0;

            int lCount = 0;
            int lSize = lDefinition.Size;
            string lGroupId = $"unit:{pSummonedUnit.Id.Value}:glyph:{lDefinition.Id}";
            for (int lX = -lSize; lX <= lSize; lX++)
            {
                for (int lY = -lSize; lY <= lSize; lY++)
                {
                    if (!IsInsideGlyphShape(lDefinition.Shape, lX, lY, lSize))
                        continue;

                    GridCoord lCell = new GridCoord(pSummonedUnit.Position.X + lX, pSummonedUnit.Position.Y + lY);
                    if (!pContext.GridService.IsInside(lCell))
                        continue;

                    string lSourceId = $"{pSkill.Id}:{lDefinition.Id}:{pSummonedUnit.Id.Value}:{lCell.X}:{lCell.Y}";
                    pContext.AddGlyph(new GridGlyphRuntime(
                        pActor.Id,
                        pActor.Team,
                        lCell,
                        lDefinition,
                        1,
                        lSourceId,
                        lGroupId,
                        pSummonedUnit.Id));
                    lCount++;
                }
            }

            return lCount;
        }

        private static bool IsInsideGlyphShape(SkillAoeShape pShape, int pOffsetX, int pOffsetY, int pSize)
        {
            int lAbsX = System.Math.Abs(pOffsetX);
            int lAbsY = System.Math.Abs(pOffsetY);
            return pShape switch
            {
                SkillAoeShape.Single => pOffsetX == 0 && pOffsetY == 0,
                SkillAoeShape.Circle => lAbsX + lAbsY <= pSize,
                SkillAoeShape.Cross => (pOffsetX == 0 || pOffsetY == 0) && lAbsX + lAbsY <= pSize,
                SkillAoeShape.X => lAbsX == lAbsY && lAbsX <= pSize,
                SkillAoeShape.HorizontalLine => pOffsetY == 0 && lAbsX <= pSize,
                SkillAoeShape.VerticalLine => pOffsetX == 0 && lAbsY <= pSize,
                _ => lAbsX <= pSize && lAbsY <= pSize
            };
        }

        private static int ApplySummonSpawnState(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            UnitRuntime pSummonedUnit,
            SkillExecutionContext pContext,
            ICollection<UnitId> pAffectedUnitIds)
        {
            if (pActor == null || pSkill?.SummonSpawnState == null || pSummonedUnit == null || pContext == null)
                return 0;

            int lAffectedCount = 0;
            foreach (UnitRuntime lCandidate in pContext.Units)
            {
                if (lCandidate == null
                    || !lCandidate.IsAlive
                    || lCandidate.Id == pSummonedUnit.Id
                    || !CanAffectByRule(pActor.Team, lCandidate.Team, pSkill.SummonSpawnStateTargetRule)
                    || !IsWithinSummonSpawnArea(pSummonedUnit, lCandidate, pSkill.SummonSpawnStateAreaSize))
                {
                    continue;
                }

                if (!lCandidate.TryApplyState(
                        pSkill.SummonSpawnState,
                        pSkill.SummonSpawnStateStacks,
                        pSkill.SummonSpawnStateDurationTurns,
                        pActor.Id,
                        pActor.Team,
                        pSkill.Id))
                    continue;

                lAffectedCount++;
                pAffectedUnitIds?.Add(lCandidate.Id);
            }

            return lAffectedCount;
        }

        private static bool CanAffectByRule(Team pSourceTeam, Team pTargetTeam, SkillGlyphTargetRule pRule)
        {
            switch (pRule)
            {
                case SkillGlyphTargetRule.AlliesOnly:
                    return pTargetTeam == pSourceTeam;

                case SkillGlyphTargetRule.EnemiesOnly:
                    return pTargetTeam != pSourceTeam;

                default:
                    return true;
            }
        }

        private static bool IsWithinSummonSpawnArea(UnitRuntime pSource, UnitRuntime pTarget, int pAreaSize)
        {
            if (pSource == null || pTarget == null)
                return false;

            int lMaxDistance = System.Math.Max(0, pAreaSize);
            foreach (GridCoord lSourceCell in pSource.EnumerateOccupiedCells())
            {
                foreach (GridCoord lTargetCell in pTarget.EnumerateOccupiedCells())
                {
                    if (lSourceCell.ManhattanDistanceTo(lTargetCell) <= lMaxDistance)
                        return true;
                }
            }

            return false;
        }
    }
}
