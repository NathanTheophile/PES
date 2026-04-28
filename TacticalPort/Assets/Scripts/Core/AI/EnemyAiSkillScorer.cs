#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.AI
{
    internal static class EnemyAiSkillScorer
    {
        public static int Score(EnemyAiContext pContext, SkillDefinition pSkill, SkillTarget pTarget, BattleActionResult pValidation, UnitSkillAiOverride pRule = null)
        {
            return Evaluate(pContext, pSkill, pTarget, pValidation, pRule).Score;
        }

        public static EnemyAiSkillEvaluation Evaluate(EnemyAiContext pContext, SkillDefinition pSkill, SkillTarget pTarget, BattleActionResult pValidation, UnitSkillAiOverride pRule = null)
        {
            if (pContext?.Actor == null || pContext.BattleService == null || pSkill == null || pTarget == null || pValidation == null)
                return default;

            EnemyAiImpactSummary lImpact = EnemyAiImpactAnalyzer.Analyze(pContext, pSkill, pTarget, pValidation);
            int lScore = EnemyAiEffectScorer.ScoreAffectedUnits(pContext, pSkill, pTarget, pValidation, lImpact);
            lScore += EnemyAiEffectScorer.ScoreImpactShape(pContext, pSkill, lImpact);
            lScore += EnemyAiEffectScorer.ScoreBoardEffect(pContext, pSkill, pTarget, lImpact);
            lScore += ScoreTargetingIntent(pContext, pSkill, pTarget, lImpact);
            lScore += ScoreRuleIntent(pContext, pSkill, pTarget, pValidation, pRule);
            lScore -= pSkill.ActionPointCost * 3;

            return new EnemyAiSkillEvaluation(
                pSkill,
                pTarget,
                pRule,
                lScore,
                $"AI used {pSkill.DisplayName} on {pTarget.Cell} (score {lScore}).");
        }

        public static bool IsOffensiveSkill(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return false;

            return pSkill.PrimaryEffectType == SkillPrimaryEffectType.Damage
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Push
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.SwitchPositions
                || pSkill.AppliedState != null;
        }

        private static int ScoreTargetingIntent(EnemyAiContext pContext, SkillDefinition pSkill, SkillTarget pTarget, EnemyAiImpactSummary pImpact)
        {
            if (pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal)
                return 0;

            if (pImpact != null && pImpact.HostileCount > 0)
                return Math.Min(30, pImpact.HostileCount * 10);

            UnitRuntime lActor = pContext.Actor;
            int lBestBonus = 0;
            foreach (UnitRuntime lTarget in EnemyAiTargeting.GetPriorityUnits(pContext, lActor.Team == Team.Player ? Team.Enemy : Team.Player))
            {
                int lDistance = pTarget.Cell.ManhattanDistanceTo(lTarget.Position);
                int lAoeReach = pSkill.AoeShape == SkillAoeShape.Single ? 0 : Math.Max(0, pSkill.AoeSize);
                if (lDistance <= lAoeReach)
                    lBestBonus = Math.Max(lBestBonus, 10 - lDistance);
            }

            return lBestBonus;
        }

        private static int ScoreRuleIntent(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            BattleActionResult pValidation,
            UnitSkillAiOverride pRule)
        {
            if (pRule == null)
                return 0;

            int lScore = pRule.Priority * 20;
            if (pRule.PreferSelfCastWhenValid && pTarget.Cell == pContext.Actor.Position)
                lScore += 30;

            if (pRule.MinTargetMissingHealth > 0 && !HasEnoughMissingHealthTarget(pContext, pValidation, pRule.MinTargetMissingHealth))
                lScore -= 1000;

            return lScore;
        }

        private static bool HasEnoughMissingHealthTarget(EnemyAiContext pContext, BattleActionResult pValidation, int pMinimumMissingHealth)
        {
            if (pValidation?.AffectedUnitIds == null)
                return false;

            for (int lIndex = 0; lIndex < pValidation.AffectedUnitIds.Count; lIndex++)
            {
                if (!pContext.BattleService.TryGetUnit(pValidation.AffectedUnitIds[lIndex], out UnitRuntime lUnit) || lUnit == null)
                    continue;

                if (Math.Max(0, lUnit.Definition.MaxHealth - lUnit.CurrentHealth) >= pMinimumMissingHealth)
                    return true;
            }

            return false;
        }
    }
}
