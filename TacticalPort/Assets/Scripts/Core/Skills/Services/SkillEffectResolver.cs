#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class SkillEffectResolver
    {
        public static BattleActionResult ApplyEffect(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pResolvedTarget,
            SkillExecutionContext pContext)
        {
            List<BattleActionResult> lResults = new List<BattleActionResult>();

            bool lShouldApplyPrimaryEffect = pSkill.PrimaryEffectType != SkillPrimaryEffectType.None;
            if (lShouldApplyPrimaryEffect)
            {
                switch (pSkill.PrimaryEffectType)
                {
                    case SkillPrimaryEffectType.Damage:
                        lResults.Add(SkillDamageResolver.ApplyDamage(pActor, pSkill, pResolvedTarget));
                        break;

                    case SkillPrimaryEffectType.Heal:
                        lResults.Add(SkillDamageResolver.ApplyHeal(pActor, pSkill, pResolvedTarget));
                        break;
                }
            }

            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.Push:
                    lResults.Add(SkillPushResolver.ApplyPush(pActor, pSkill, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.Teleport:
                    lResults.Add(SkillPlacementResolver.ApplyTeleport(pActor, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.SwitchPositions:
                    lResults.Add(SkillPlacementResolver.ApplySwitch(pActor, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.Summon:
                    lResults.Add(SkillPlacementResolver.ApplySummon(pActor, pSkill, pResolvedTarget, pContext));
                    break;

                case SkillAdditionalEffectType.CreateGlyph:
                    lResults.Add(SkillPlacementResolver.ApplyGlyph(pActor, pSkill, pResolvedTarget, pContext));
                    break;
            }

            if (pSkill.AppliedState != null)
                lResults.Add(SkillDamageResolver.ApplyState(pActor, pSkill, pResolvedTarget));

            return CombineEffectResults(pActor, lResults);
        }

        private static BattleActionResult CombineEffectResults(UnitRuntime pActor, List<BattleActionResult> pResults)
        {
            if (pResults == null || pResults.Count == 0)
                return BattleActionResult.Succeeded(BattleActionType.Skill, $"{pActor.Definition.DisplayName} used a skill.", new[] { pActor.Id });

            HashSet<UnitId> lAffectedIds = new HashSet<UnitId>();
            List<string> lMessages = new List<string>();
            BattleActionOutcomeFlags lOutcomeFlags = BattleActionOutcomeFlags.None;

            foreach (BattleActionResult lResult in pResults)
            {
                if (lResult == null || !lResult.IsSuccess)
                    return lResult ?? BattleActionResult.Failed(BattleActionType.Skill, "Skill execution failed.");

                if (!string.IsNullOrWhiteSpace(lResult.Message))
                    lMessages.Add(lResult.Message);

                lOutcomeFlags |= lResult.OutcomeFlags;

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

            return BattleActionResult.Succeeded(BattleActionType.Skill, lMessage, lAffectedIds, null, lOutcomeFlags);
        }
    }
}
