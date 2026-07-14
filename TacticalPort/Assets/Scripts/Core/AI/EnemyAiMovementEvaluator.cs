#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class EnemyAiMovementEvaluator
    {
        private const int MIN_MOVE_SCORE_GAIN = 8;
        private const int MIN_SKILL_MOVE_SCORE_GAIN = 5;

        public static bool TryChooseMovement(
            EnemyAiContext pContext,
            EnemyAiProfile pProfile,
            out GridCoord pDestination,
            out string pReason)
        {
            pDestination = default;
            pReason = string.Empty;

            UnitRuntime lActor = pContext?.Actor;
            if (lActor == null)
                return false;

            EnemyAiSkillEvaluation lCurrentSkillEvaluation = EnemyAiMovementSkillForecast.EvaluateBestSkillOpportunityFromCell(pContext, lActor.Position);
            int lCurrentScore = ScoreCell(pContext, pProfile, lActor.Position, lCurrentSkillEvaluation);
            int lBestScore = lCurrentScore;
            GridCoord lBestCell = lActor.Position;
            EnemyAiSkillEvaluation lBestSkillEvaluation = lCurrentSkillEvaluation;

            foreach (GridCoord lCell in EnemyAiTargeting.EnumerateMovementCandidates(pContext, true))
            {
                EnemyAiSkillEvaluation lSkillEvaluation = EnemyAiMovementSkillForecast.EvaluateBestSkillOpportunityFromCell(pContext, lCell);
                int lScore = ScoreCell(pContext, pProfile, lCell, lSkillEvaluation);
                lScore -= lActor.Position.ManhattanDistanceTo(lCell);

                if (lScore <= lBestScore)
                    continue;

                lBestScore = lScore;
                lBestCell = lCell;
                lBestSkillEvaluation = lSkillEvaluation;
            }

            if (lBestCell == lActor.Position)
                return false;

            bool lEnablesBetterSkill = lBestSkillEvaluation.IsValid
                && lBestSkillEvaluation.Score >= lCurrentSkillEvaluation.Score + MIN_SKILL_MOVE_SCORE_GAIN;
            if (!lEnablesBetterSkill && lBestScore < lCurrentScore + MIN_MOVE_SCORE_GAIN)
                return false;

            pDestination = lBestCell;
            pReason = lEnablesBetterSkill
                ? $"AI moved to set up {lBestSkillEvaluation.Skill.DisplayName} on {lBestSkillEvaluation.Target.Cell}."
                : pProfile.MovementMode == EnemyAiMovementMode.Kite
                ? "AI kited to a safer firing lane."
                : "AI repositioned to pressure a target.";
            return true;
        }

        private static int ScoreCell(
            EnemyAiContext pContext,
            EnemyAiProfile pProfile,
            GridCoord pCell,
            EnemyAiSkillEvaluation pSkillEvaluation)
        {
            UnitRuntime lActor = pContext.Actor;
            List<UnitRuntime> lHostiles = EnemyAiTargeting.GetPriorityUnits(pContext, pProfile.HostileTeam);
            if (lHostiles.Count == 0)
                return 0;

            int lThreatScore = ScoreThreatDistance(pCell, lHostiles, pProfile);
            int lPressureScore = ScoreOffensivePressureFromCell(lActor, pCell, lHostiles);
            int lSupportScore = ScoreSupportPressureFromCell(lActor, pCell, pContext);
            int lSkillScore = pSkillEvaluation.IsValid ? pSkillEvaluation.Score : 0;
            int lActionSetupScore = pSkillEvaluation.IsValid ? 80 : 0;

            if (!pSkillEvaluation.IsValid && lActor.RemainingEnergy > 0)
                lPressureScore /= 3;

            return pProfile.MovementMode == EnemyAiMovementMode.Kite
                ? lPressureScore * 2 + lThreatScore * 3 + lSupportScore + lActionSetupScore + lSkillScore * 2
                : lPressureScore * 2 + lThreatScore + lSupportScore + lActionSetupScore + lSkillScore * 4;
        }

        private static int ScoreThreatDistance(GridCoord pCell, List<UnitRuntime> pHostiles, EnemyAiProfile pProfile)
        {
            int lNearestDistance = int.MaxValue;
            for (int lIndex = 0; lIndex < pHostiles.Count; lIndex++)
            {
                UnitRuntime lHostile = pHostiles[lIndex];
                if (lHostile != null)
                    lNearestDistance = Math.Min(lNearestDistance, pCell.ManhattanDistanceTo(lHostile.Position));
            }

            if (lNearestDistance == int.MaxValue)
                return 0;

            if (pProfile.MovementMode == EnemyAiMovementMode.Kite)
            {
                int lDistanceError = Math.Abs(lNearestDistance - pProfile.PreferredThreatDistance);
                int lClosePenalty = lNearestDistance <= 1 ? 35 : lNearestDistance == 2 ? 12 : 0;
                return 50 - lDistanceError * 8 - lClosePenalty;
            }

            return -Math.Max(0, lNearestDistance - 1) * 2;
        }

        private static int ScoreOffensivePressureFromCell(UnitRuntime pActor, GridCoord pCell, List<UnitRuntime> pTargets)
        {
            int lBestScore = int.MinValue;
            IReadOnlyList<SkillDefinition> lSkills = pActor.Skills;

            for (int lSkillIndex = 0; lSkillIndex < lSkills.Count; lSkillIndex++)
            {
                SkillDefinition lSkill = lSkills[lSkillIndex];
                if (!EnemyAiSkillScorer.IsOffensiveSkill(lSkill) || pActor.RemainingEnergy < lSkill.EnergyCost)
                    continue;

                int lRangeMin = pActor.GetSkillRangeMin(lSkill);
                int lRangeMax = pActor.GetSkillRangeMax(lSkill);
                for (int lTargetIndex = 0; lTargetIndex < pTargets.Count; lTargetIndex++)
                {
                    UnitRuntime lTarget = pTargets[lTargetIndex];
                    if (lTarget == null)
                        continue;

                    int lDistance = pCell.ManhattanDistanceTo(lTarget.Position);
                    int lRangePenalty = lDistance < lRangeMin ? lRangeMin - lDistance : Math.Max(0, lDistance - lRangeMax);
                    int lScore = 80 - lRangePenalty * 20 + Math.Max(0, lSkill.Power);

                    if (!GridLineOfSightUtility.MatchesAlignment(pCell, lTarget.Position, lSkill.TargetAlignment))
                        lScore -= 25;

                    lBestScore = Math.Max(lBestScore, lScore);
                }
            }

            return lBestScore != int.MinValue ? lBestScore : -pCell.ManhattanDistanceTo(pTargets[0].Position) * 10;
        }

        private static int ScoreSupportPressureFromCell(UnitRuntime pActor, GridCoord pCell, EnemyAiContext pContext)
        {
            int lBestScore = 0;
            IReadOnlyList<SkillDefinition> lSkills = pActor.Skills;
            List<UnitRuntime> lAllies = EnemyAiTargeting.GetPriorityUnits(pContext, pActor.Team);

            for (int lSkillIndex = 0; lSkillIndex < lSkills.Count; lSkillIndex++)
            {
                SkillDefinition lSkill = lSkills[lSkillIndex];
                if (lSkill == null || lSkill.PrimaryEffectType != SkillPrimaryEffectType.Heal)
                    continue;

                int lRangeMin = pActor.GetSkillRangeMin(lSkill);
                int lRangeMax = pActor.GetSkillRangeMax(lSkill);
                for (int lAllyIndex = 0; lAllyIndex < lAllies.Count; lAllyIndex++)
                {
                    UnitRuntime lAlly = lAllies[lAllyIndex];
                    if (lAlly == null)
                        continue;

                    int lMissingHealth = Math.Max(0, lAlly.CurrentMaxHealth - lAlly.CurrentHealth);
                    if (lMissingHealth <= 0)
                        continue;

                    int lDistance = pCell.ManhattanDistanceTo(lAlly.Position);
                    int lRangePenalty = lDistance < lRangeMin ? lRangeMin - lDistance : Math.Max(0, lDistance - lRangeMax);
                    lBestScore = Math.Max(lBestScore, Math.Min(lMissingHealth, lSkill.Power) * 4 - lRangePenalty * 15);
                }
            }

            return lBestScore;
        }
    }
}
