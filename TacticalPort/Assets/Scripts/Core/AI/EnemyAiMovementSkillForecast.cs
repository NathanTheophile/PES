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
    internal static class EnemyAiMovementSkillForecast
    {
        public static EnemyAiSkillEvaluation EvaluateBestSkillOpportunityFromCell(EnemyAiContext pContext, GridCoord pCell)
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
            {
                return;
            }

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
                if (lEvaluation.Score > pBestEvaluation.Score)
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
            int lAoeSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : System.Math.Max(0, pSkill.AoeSize);
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

            int lAoeSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : System.Math.Max(0, pSkill.AoeSize);
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
