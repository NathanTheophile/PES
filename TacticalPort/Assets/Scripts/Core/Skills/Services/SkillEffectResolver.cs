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

                case SkillAdditionalEffectType.Pull:
                    lResults.Add(SkillPullResolver.ApplyPull(pActor, pSkill, pResolvedTarget, pContext));
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

                case SkillAdditionalEffectType.AdvanceActivePassiveProgression:
                    lResults.Add(ApplyActivePassiveProgression(pActor, pSkill));
                    break;

                case SkillAdditionalEffectType.RepairAndToggleStates:
                    lResults.Add(ApplyRepairAndToggle(pActor, pSkill, pResolvedTarget));
                    break;

                case SkillAdditionalEffectType.ReduceStateDurations:
                    lResults.Add(ApplyStateDurationReduction(pActor, pSkill, pResolvedTarget));
                    break;
            }

            BattleActionResult lAppliedStateResult = null;
            if (pSkill.AppliedState != null)
            {
                lAppliedStateResult = SkillDamageResolver.ApplyState(pActor, pSkill, pResolvedTarget);
                lResults.Add(lAppliedStateResult);
            }

            bool lCanApplyCasterState = pSkill.AppliedState == null
                || (lAppliedStateResult != null
                    && (lAppliedStateResult.OutcomeFlags & BattleActionOutcomeFlags.StateApplied) != 0);
            if (pSkill.CasterAppliedState != null && lCanApplyCasterState)
            {
                int lCasterStacks = pSkill.CasterStateStacksPerAffectedTarget
                    ? CountSuccessfullyAffectedUnits(lAppliedStateResult, pActor.Id)
                    : -1;
                if (lCasterStacks != 0)
                    lResults.Add(SkillDamageResolver.ApplyStateToCaster(pActor, pSkill, lCasterStacks));
            }

            return CombineEffectResults(pActor, lResults);
        }

        private static BattleActionResult ApplyActivePassiveProgression(UnitRuntime pActor, SkillDefinition pSkill)
        {
            StateProgressionDefinition lProgression = pActor?.ActivePassive?.StateProgression;
            bool lAdvanced = pActor != null
                && lProgression != null
                && pActor.AdvanceStateProgression(lProgression, pSkill.PassiveProgressionSteps);

            string lMessage = lAdvanced
                ? $"{pActor.Definition.DisplayName} advanced {lProgression.name}."
                : string.Empty;
            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                pActor != null ? new[] { pActor.Id } : null,
                null,
                lAdvanced ? BattleActionOutcomeFlags.StateApplied : BattleActionOutcomeFlags.None);
        }

        private static BattleActionResult ApplyRepairAndToggle(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pTarget)
        {
            UnitRuntime lTarget = pTarget?.PrimaryTargetUnit;
            if (lTarget == null || !lTarget.IsAlive)
                return BattleActionResult.Failed(BattleActionType.Skill, "A living target is required.");

            int lRestored = lTarget.RestoreDirectHealth(pSkill.RepairAmount);
            if (!lTarget.TryTogglePersistentStates(pSkill.ToggleStateA, pSkill.ToggleStateB))
                return BattleActionResult.Failed(BattleActionType.Skill, "The target has no toggleable state.");

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                $"{lTarget.Definition.DisplayName} was repaired for {lRestored} and changed mode.",
                new[] { pActor.Id, lTarget.Id },
                null,
                lRestored > 0 ? BattleActionOutcomeFlags.Heal : BattleActionOutcomeFlags.StateApplied);
        }

        private static BattleActionResult ApplyStateDurationReduction(
            UnitRuntime pActor,
            SkillDefinition pSkill,
            ResolvedSkillTarget pTarget)
        {
            List<UnitRuntime> lTargets = new List<UnitRuntime>();
            if (pTarget?.AffectedUnits != null)
            {
                for (int lIndex = 0; lIndex < pTarget.AffectedUnits.Count; lIndex++)
                {
                    UnitRuntime lTarget = pTarget.AffectedUnits[lIndex];
                    if (lTarget != null && lTarget.IsAlive)
                        lTargets.Add(lTarget);
                }
            }

            lTargets.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            int lReducedStateCount = 0;
            List<UnitId> lAffectedIds = new List<UnitId> { pActor.Id };
            for (int lIndex = 0; lIndex < lTargets.Count; lIndex++)
            {
                UnitRuntime lTarget = lTargets[lIndex];
                int lReduced = lTarget.ReduceTemporaryStateDurations(pSkill.StateDurationReduction);
                lReducedStateCount += lReduced;
                if (lReduced > 0 && lTarget.Id != pActor.Id)
                    lAffectedIds.Add(lTarget.Id);
            }

            string lMessage = lReducedStateCount > 0
                ? $"Reduced the duration of {lReducedStateCount} state instance(s)."
                : string.Empty;
            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedIds,
                null,
                lReducedStateCount > 0 ? BattleActionOutcomeFlags.StateDurationReduced : BattleActionOutcomeFlags.None);
        }

        private static int CountSuccessfullyAffectedUnits(BattleActionResult pResult, UnitId pActorId)
        {
            if (pResult?.AffectedUnitIds == null)
                return 0;

            int lCount = 0;
            foreach (UnitId lUnitId in pResult.AffectedUnitIds)
            {
                if (lUnitId.IsValid && lUnitId != pActorId)
                    lCount++;
            }

            return lCount;
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

            List<UnitId> lOrderedAffectedIds = new List<UnitId>(lAffectedIds);
            lOrderedAffectedIds.Sort((pLeft, pRight) => pLeft.Value.CompareTo(pRight.Value));
            return BattleActionResult.Succeeded(BattleActionType.Skill, lMessage, lOrderedAffectedIds, null, lOutcomeFlags);
        }
    }
}
