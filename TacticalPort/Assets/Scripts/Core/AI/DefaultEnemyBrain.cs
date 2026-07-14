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
    public sealed class DefaultEnemyBrain : IEnemyBrain
    {
        public EnemyAiAction ChooseAction(EnemyAiContext pContext)
        {
            if (pContext?.Actor == null || !pContext.Actor.IsAlive)
                return EnemyAiAction.EndTurn();

            EnemyAiProfile lProfile = EnemyAiProfile.Create(pContext);

            if (ShouldRepositionBeforeSkill(pContext, lProfile)
                && EnemyAiMovementEvaluator.TryChooseMovement(pContext, lProfile, out GridCoord lKiteDestination, out string lKiteReason))
            {
                return EnemyAiAction.Move(lKiteDestination, lKiteReason);
            }

            if (TryChooseSkill(pContext, out EnemyAiAction lSkillAction))
                return lSkillAction;

            if (pContext.Actor.RemainingMobility > 0
                && EnemyAiMovementEvaluator.TryChooseMovement(pContext, lProfile, out GridCoord lDestination, out string lMoveReason))
            {
                return EnemyAiAction.Move(lDestination, lMoveReason);
            }

            return EnemyAiAction.EndTurn($"{pContext.Actor.Definition.DisplayName} ends turn with no valid action.");
        }

        private static bool ShouldRepositionBeforeSkill(EnemyAiContext pContext, EnemyAiProfile pProfile)
        {
            UnitRuntime lActor = pContext?.Actor;
            if (lActor == null || lActor.RemainingMobility <= 0 || pProfile.MovementMode != EnemyAiMovementMode.Kite)
                return false;

            int lTurnEnergy = lActor.Definition.EnergyPerTurn + lActor.GetEnergyModifier();
            bool lHasAlreadyActed = lActor.RemainingEnergy < lTurnEnergy;
            return pContext.ForceKiteThisDecision || lHasAlreadyActed || lActor.Definition.EnemyAiMovementPolicy == EnemyAiMovementPolicy.KeepDistance;
        }

        private static bool TryChooseSkill(EnemyAiContext pContext, out EnemyAiAction pAction)
        {
            pAction = default;
            EnemyAiSkillEvaluation lBestEvaluation = default;
            HashSet<SkillDefinition> lRuledSkills = new HashSet<SkillDefinition>();
            List<UnitSkillAiOverride> lOrderedRules = EnemyAiSkillRuleUtility.BuildOrderedRules(pContext.Actor, lRuledSkills);
            for (int lIndex = 0; lIndex < lOrderedRules.Count; lIndex++)
                EvaluateSkill(pContext, lOrderedRules[lIndex].Skill, lOrderedRules[lIndex], ref lBestEvaluation);

            foreach (SkillDefinition lSkill in pContext.Actor.Skills)
            {
                if (lSkill != null && !lRuledSkills.Contains(lSkill))
                    EvaluateSkill(pContext, lSkill, null, ref lBestEvaluation);
            }

            if (!lBestEvaluation.IsValid)
                return false;

            pAction = lBestEvaluation.ToAction();
            return true;
        }

        private static void EvaluateSkill(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            UnitSkillAiOverride pRule,
            ref EnemyAiSkillEvaluation pBestEvaluation)
        {
            if (pSkill == null
                || pContext.Actor.RemainingEnergy < pSkill.EnergyCost
                || !EnemyAiSkillRuleUtility.CanUseRule(pContext.Actor, pRule))
                return;

            foreach (GridCoord lCell in EnemyAiTargeting.EnumerateTargetCells(pContext, pSkill, pRule))
            {
                SkillTarget lTarget = SkillTarget.ForCell(lCell);
                if (!pContext.TryValidate(pSkill, lTarget, out BattleActionResult lValidation))
                    continue;

                EnemyAiSkillEvaluation lEvaluation = EnemyAiSkillScorer.Evaluate(pContext, pSkill, lTarget, lValidation, pRule);
                if (lEvaluation.Score <= pBestEvaluation.Score)
                    continue;

                pBestEvaluation = lEvaluation;
            }
        }

    }
}
