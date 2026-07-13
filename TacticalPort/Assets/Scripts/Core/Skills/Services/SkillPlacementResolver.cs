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
            UnitRuntime lSummonedUnit = pContext.TrySummonUnit(pSkill.SummonUnit, pSkill.SummonTeamRule, pActor, pResolvedTarget.TargetCell);
            if (lSummonedUnit == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "Summon failed.");

            List<UnitId> lAffectedUnitIds = new List<UnitId> { pActor.Id, lSummonedUnit.Id };
            int lSpawnStateCount = ApplySummonSpawnState(pActor, pSkill, lSummonedUnit, pContext, lAffectedUnitIds);
            string lMessage = $"{pActor.Definition.DisplayName} summoned {lSummonedUnit.Definition.DisplayName}.";
            if (lSpawnStateCount > 0)
                lMessage += $" {pSkill.SummonSpawnState.DisplayName} applied to {lSpawnStateCount} unit(s).";

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds,
                null,
                BattleActionOutcomeFlags.Summon | (lSpawnStateCount > 0 ? BattleActionOutcomeFlags.StateApplied : BattleActionOutcomeFlags.None));
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
                pContext.AddGlyph(new GridGlyphRuntime(
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
                    pSkill.GlyphAppliedStateDurationTurns));
                lGlyphCount++;
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} placed {lGlyphCount} glyph(s).",
                new[] { pActor.Id },
                null,
                lGlyphCount > 0 ? BattleActionOutcomeFlags.Glyph : BattleActionOutcomeFlags.None);
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

                if (!lCandidate.TryApplyState(pSkill.SummonSpawnState, pSkill.SummonSpawnStateStacks, pSkill.SummonSpawnStateDurationTurns))
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
