#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
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

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} summoned {lSummonedUnit.Definition.DisplayName}.",
                new[] { pActor.Id, lSummonedUnit.Id });
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
                    lGlyphGroupId));
                lGlyphCount++;
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{pActor.Definition.DisplayName} placed {lGlyphCount} glyph(s).",
                new[] { pActor.Id });
        }
    }
}
