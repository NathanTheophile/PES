#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.AI
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

            EnemyAiSkillEvaluation lCurrentSkillEvaluation = EvaluateBestSkillOpportunityFromCell(pContext, lActor.Position);
            int lCurrentScore = ScoreCell(pContext, pProfile, lActor.Position, lCurrentSkillEvaluation);
            int lBestScore = lCurrentScore;
            GridCoord lBestCell = lActor.Position;
            EnemyAiSkillEvaluation lBestSkillEvaluation = lCurrentSkillEvaluation;

            foreach (GridCoord lCell in EnemyAiTargeting.EnumerateMovementCandidates(pContext, true))
            {
                EnemyAiSkillEvaluation lSkillEvaluation = EvaluateBestSkillOpportunityFromCell(pContext, lCell);
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

            if (!pSkillEvaluation.IsValid && lActor.RemainingActionPoints > 0)
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
                if (!EnemyAiSkillScorer.IsOffensiveSkill(lSkill) || pActor.RemainingActionPoints < lSkill.ActionPointCost)
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

                    int lMissingHealth = Math.Max(0, lAlly.Definition.MaxHealth - lAlly.CurrentHealth);
                    if (lMissingHealth <= 0)
                        continue;

                    int lDistance = pCell.ManhattanDistanceTo(lAlly.Position);
                    int lRangePenalty = lDistance < lRangeMin ? lRangeMin - lDistance : Math.Max(0, lDistance - lRangeMax);
                    lBestScore = Math.Max(lBestScore, Math.Min(lMissingHealth, lSkill.Power) * 4 - lRangePenalty * 15);
                }
            }

            return lBestScore;
        }

        private static EnemyAiSkillEvaluation EvaluateBestSkillOpportunityFromCell(EnemyAiContext pContext, GridCoord pCell)
        {
            UnitRuntime lActor = pContext?.Actor;
            if (lActor == null)
                return default;

            EnemyAiSkillEvaluation lBestEvaluation = default;
            HashSet<SkillDefinition> lRuledSkills = new HashSet<SkillDefinition>();
            List<UnitSkillAiOverride> lOrderedRules = EnemyAiSkillRuleUtility.BuildOrderedRules(lActor, lRuledSkills);
            for (int lIndex = 0; lIndex < lOrderedRules.Count; lIndex++)
                EvaluateSkillFromCell(pContext, pCell, lOrderedRules[lIndex].Skill, lOrderedRules[lIndex], ref lBestEvaluation);

            foreach (SkillDefinition lSkill in lActor.Skills)
            {
                if (lSkill != null && !lRuledSkills.Contains(lSkill))
                    EvaluateSkillFromCell(pContext, pCell, lSkill, null, ref lBestEvaluation);
            }

            return lBestEvaluation;
        }

        private static void EvaluateSkillFromCell(
            EnemyAiContext pContext,
            GridCoord pCell,
            SkillDefinition pSkill,
            UnitSkillAiOverride pRule,
            ref EnemyAiSkillEvaluation pBestEvaluation)
        {
            UnitRuntime lActor = pContext.Actor;
            if (pSkill == null
                || lActor.RemainingActionPoints < pSkill.ActionPointCost
                || !EnemyAiSkillRuleUtility.CanUseRule(lActor, pRule))
                return;

            foreach (GridCoord lTargetCell in EnumerateTargetCellsFromCell(pContext, pCell, pSkill, pRule))
            {
                SkillTarget lTarget = SkillTarget.ForCell(lTargetCell);
                bool lIsCurrentCell = pCell == lActor.Position;
                bool lIsValid = lIsCurrentCell
                    ? pContext.TryValidate(pSkill, lTarget, out BattleActionResult lValidation)
                    : TryBuildFutureValidation(pContext, pCell, pSkill, lTarget, out lValidation);

                if (!lIsValid)
                    continue;

                EnemyAiSkillEvaluation lEvaluation = EnemyAiSkillScorer.Evaluate(pContext, pSkill, lTarget, lValidation, pRule);
                if (lEvaluation.Score <= pBestEvaluation.Score)
                    continue;

                pBestEvaluation = lEvaluation;
            }
        }

        private static IEnumerable<GridCoord> EnumerateTargetCellsFromCell(
            EnemyAiContext pContext,
            GridCoord pCell,
            SkillDefinition pSkill,
            UnitSkillAiOverride pRule)
        {
            HashSet<GridCoord> lCells = new HashSet<GridCoord>();
            UnitRuntime lActor = pContext.Actor;

            if (pRule != null && pRule.TargetTeam == EnemyAiTargetTeam.Self)
            {
                lCells.Add(pCell);
            }
            else
            {
                Team lTargetTeam = ResolveTargetTeam(lActor, pSkill, pRule);
                foreach (UnitRuntime lUnit in EnemyAiTargeting.GetPriorityUnits(pContext, lTargetTeam, pRule != null ? pRule.TargetMode : EnemyAiTargetMode.BestScore))
                    AddUnitTargetCells(lCells, pCell, lUnit, pSkill);
            }

            if (pSkill.AdditionalEffectType == SkillAdditionalEffectType.Summon
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Teleport
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.CreateGlyph)
            {
                AddRangeCells(lCells, lActor, pCell, pSkill);
            }

            if (pRule == null || pRule.AllowSelfCast || pSkill.CanAffectCaster)
                lCells.Add(pCell);

            List<GridCoord> lOrderedCells = new List<GridCoord>(lCells);
            lOrderedCells.Sort((pLeft, pRight) =>
            {
                int lDistanceResult = pCell.ManhattanDistanceTo(pLeft).CompareTo(pCell.ManhattanDistanceTo(pRight));
                if (lDistanceResult != 0)
                    return lDistanceResult;

                int lYResult = pLeft.Y.CompareTo(pRight.Y);
                return lYResult != 0 ? lYResult : pLeft.X.CompareTo(pRight.X);
            });

            for (int lIndex = 0; lIndex < lOrderedCells.Count; lIndex++)
                yield return lOrderedCells[lIndex];
        }

        private static bool TryBuildFutureValidation(
            EnemyAiContext pContext,
            GridCoord pSourceCell,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            out BattleActionResult pValidation)
        {
            pValidation = null;
            UnitRuntime lActor = pContext.Actor;
            int lDistance = pSourceCell.ManhattanDistanceTo(pTarget.Cell);
            int lRangeMin = lActor.GetSkillRangeMin(pSkill);
            int lRangeMax = lActor.GetSkillRangeMax(pSkill);

            if (lDistance < lRangeMin || lDistance > lRangeMax)
                return false;

            if (!GridLineOfSightUtility.MatchesAlignment(pSourceCell, pTarget.Cell, pSkill.TargetAlignment))
                return false;

            UnitRuntime lPrimaryTarget = TryResolveOccupant(pContext, pTarget.Cell);
            if (pSkill.RequiresLineOfSight && pSourceCell != pTarget.Cell && !HasApproximateLineOfSight(pContext, pSourceCell, pTarget.Cell, lPrimaryTarget))
                return false;

            List<UnitId> lAffectedUnitIds = ResolveAffectedUnitIds(pContext, pSourceCell, pSkill, pTarget.Cell);
            if (RequiresAffectedUnit(pSkill) && lAffectedUnitIds.Count == 0)
                return false;

            if (pSkill.AdditionalEffectType == SkillAdditionalEffectType.SwitchPositions
                && (lPrimaryTarget == null || lPrimaryTarget.Id == lActor.Id))
            {
                return false;
            }

            if ((pSkill.AdditionalEffectType == SkillAdditionalEffectType.Summon
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Teleport)
                && IsOccupiedByOtherUnit(pContext, pTarget.Cell, lActor.Id))
            {
                return false;
            }

            pValidation = BattleActionResult.Succeeded(BattleActionType.Skill, "Future skill opportunity.", lAffectedUnitIds);
            return true;
        }

        private static List<UnitId> ResolveAffectedUnitIds(
            EnemyAiContext pContext,
            GridCoord pSourceCell,
            SkillDefinition pSkill,
            GridCoord pTargetCell)
        {
            List<UnitId> lAffectedUnitIds = new List<UnitId>();
            if (pContext?.BattleService?.Units == null)
                return lAffectedUnitIds;

            GridCoord lDirection = GridLineOfSightUtility.ResolveAreaDirection(pSourceCell, pTargetCell);
            int lAoeSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : Math.Max(0, pSkill.AoeSize);
            foreach (UnitRuntime lUnit in pContext.BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive)
                    continue;

                bool lIsAffected = false;
                foreach (GridCoord lOccupiedCell in EnumerateOccupiedCellsFromVirtualSource(pContext.Actor, lUnit, pSourceCell))
                {
                    int lOffsetX = lOccupiedCell.X - pTargetCell.X;
                    int lOffsetY = lOccupiedCell.Y - pTargetCell.Y;
                    if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lAoeSize, lDirection))
                        continue;

                    lIsAffected = true;
                    break;
                }

                if (lIsAffected)
                    lAffectedUnitIds.Add(lUnit.Id);
            }

            return lAffectedUnitIds;
        }

        private static IEnumerable<GridCoord> EnumerateOccupiedCellsFromVirtualSource(UnitRuntime pActor, UnitRuntime pUnit, GridCoord pSourceCell)
        {
            if (pUnit == pActor)
            {
                IReadOnlyList<GridCoord> lOffsets = pActor.OccupiedCellOffsets;
                for (int lIndex = 0; lIndex < lOffsets.Count; lIndex++)
                    yield return new GridCoord(pSourceCell.X + lOffsets[lIndex].X, pSourceCell.Y + lOffsets[lIndex].Y);

                yield break;
            }

            foreach (GridCoord lCell in pUnit.EnumerateOccupiedCells())
                yield return lCell;
        }

        private static bool HasApproximateLineOfSight(
            EnemyAiContext pContext,
            GridCoord pSourceCell,
            GridCoord pTargetCell,
            UnitRuntime pTargetUnit)
        {
            return GridLineOfSightUtility.HasLineOfSight(
                pSourceCell,
                pTargetCell,
                pCell =>
                {
                    if (!pContext.BattleService.IsInside(pCell) || pContext.BattleService.BlocksLineOfSight(pCell))
                        return true;

                    UnitRuntime lOccupant = TryResolveOccupant(pContext, pCell);
                    if (lOccupant == null || lOccupant.Id == pContext.Actor.Id)
                        return false;

                    if (pTargetUnit != null && lOccupant.Id == pTargetUnit.Id)
                        return pCell != pTargetCell;

                    return true;
                });
        }

        private static void AddUnitTargetCells(ISet<GridCoord> pCells, GridCoord pSourceCell, UnitRuntime pUnit, SkillDefinition pSkill)
        {
            if (pCells == null || pUnit == null || pSkill == null)
                return;

            int lAoeSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : Math.Max(0, pSkill.AoeSize);
            foreach (GridCoord lOccupiedCell in pUnit.EnumerateOccupiedCells())
            {
                pCells.Add(lOccupiedCell);
                GridCoord lDirection = GridLineOfSightUtility.ResolveAreaDirection(pSourceCell, lOccupiedCell);

                for (int lOffsetY = -lAoeSize; lOffsetY <= lAoeSize; lOffsetY++)
                {
                    for (int lOffsetX = -lAoeSize; lOffsetX <= lAoeSize; lOffsetX++)
                    {
                        if (GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lAoeSize, lDirection))
                            pCells.Add(new GridCoord(lOccupiedCell.X - lOffsetX, lOccupiedCell.Y - lOffsetY));
                    }
                }
            }
        }

        private static void AddRangeCells(ISet<GridCoord> pCells, UnitRuntime pActor, GridCoord pSourceCell, SkillDefinition pSkill)
        {
            int lRangeMin = pActor.GetSkillRangeMin(pSkill);
            int lRangeMax = pActor.GetSkillRangeMax(pSkill);
            for (int lOffsetY = -lRangeMax; lOffsetY <= lRangeMax; lOffsetY++)
            {
                for (int lOffsetX = -lRangeMax; lOffsetX <= lRangeMax; lOffsetX++)
                {
                    GridCoord lCell = new GridCoord(pSourceCell.X + lOffsetX, pSourceCell.Y + lOffsetY);
                    int lDistance = pSourceCell.ManhattanDistanceTo(lCell);
                    if (lDistance < lRangeMin || lDistance > lRangeMax)
                        continue;

                    if (GridLineOfSightUtility.MatchesAlignment(pSourceCell, lCell, pSkill.TargetAlignment))
                        pCells.Add(lCell);
                }
            }
        }

        private static UnitRuntime TryResolveOccupant(EnemyAiContext pContext, GridCoord pCell)
        {
            if (pContext?.BattleService?.Units == null)
                return null;

            foreach (UnitRuntime lUnit in pContext.BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive)
                    continue;

                foreach (GridCoord lOccupiedCell in EnumerateOccupiedCellsFromVirtualSource(pContext.Actor, lUnit, pContext.Actor.Position))
                {
                    if (lOccupiedCell == pCell)
                        return lUnit;
                }
            }

            return null;
        }

        private static bool IsOccupiedByOtherUnit(EnemyAiContext pContext, GridCoord pCell, UnitId pIgnoredUnitId)
        {
            UnitRuntime lOccupant = TryResolveOccupant(pContext, pCell);
            return lOccupant != null && lOccupant.Id != pIgnoredUnitId;
        }

        private static bool RequiresAffectedUnit(SkillDefinition pSkill)
        {
            return pSkill.PrimaryEffectType == SkillPrimaryEffectType.Damage
                || pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Push;
        }

        private static Team ResolveTargetTeam(UnitRuntime pActor, SkillDefinition pSkill, UnitSkillAiOverride pRule)
        {
            if (pRule != null)
            {
                switch (pRule.TargetTeam)
                {
                    case EnemyAiTargetTeam.Enemy:
                        return pActor.Team == Team.Player ? Team.Enemy : Team.Player;

                    case EnemyAiTargetTeam.Ally:
                        return pActor.Team;
                }
            }

            return pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal
                ? pActor.Team
                : pActor.Team == Team.Player ? Team.Enemy : Team.Player;
        }

    }
}
