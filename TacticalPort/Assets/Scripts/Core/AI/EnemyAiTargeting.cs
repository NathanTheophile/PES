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
    public static class EnemyAiTargeting
    {
        public static List<UnitRuntime> GetPriorityUnits(EnemyAiContext pContext, Team pTeam)
        {
            List<UnitRuntime> lUnits = new List<UnitRuntime>();
            if (pContext?.BattleService?.Units == null || pContext.Actor == null)
                return lUnits;

            foreach (UnitRuntime lUnit in pContext.BattleService.Units)
            {
                if (lUnit != null && lUnit.IsAlive && lUnit.Team == pTeam)
                    lUnits.Add(lUnit);
            }

            lUnits.Sort((pLeft, pRight) => CompareByPriority(pContext.Actor, pLeft, pRight));
            return lUnits;
        }

        public static IEnumerable<GridCoord> EnumerateTargetCells(EnemyAiContext pContext, SkillDefinition pSkill, UnitSkillAiOverride pRule = null)
        {
            HashSet<GridCoord> lCells = new HashSet<GridCoord>();
            UnitRuntime lActor = pContext?.Actor;
            if (lActor == null || pSkill == null)
                yield break;

            if (pRule != null && pRule.TargetTeam == EnemyAiTargetTeam.Self)
            {
                lCells.Add(lActor.Position);
            }
            else
            {
                Team lPrimaryTeam = ResolveTargetTeam(lActor, pSkill, pRule);
                foreach (UnitRuntime lUnit in GetPriorityUnits(pContext, lPrimaryTeam, pRule != null ? pRule.TargetMode : EnemyAiTargetMode.BestScore))
                    AddUnitTargetCells(lCells, lActor, lUnit, pSkill);
            }

            if (ShouldEnumerateWholeSkillRange(pSkill)
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Summon
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Teleport
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.CreateGlyph)
            {
                foreach (GridCoord lCell in EnumerateSkillRangeCells(lActor, pSkill))
                    lCells.Add(lCell);
            }

            if (pRule == null || pRule.AllowSelfCast || pSkill.CanAffectCaster)
                lCells.Add(lActor.Position);

            List<GridCoord> lOrderedCells = new List<GridCoord>(lCells);
            lOrderedCells.Sort((pLeft, pRight) =>
            {
                int lDistanceResult = lActor.Position.ManhattanDistanceTo(pLeft).CompareTo(lActor.Position.ManhattanDistanceTo(pRight));
                if (lDistanceResult != 0)
                    return lDistanceResult;

                int lYResult = pLeft.Y.CompareTo(pRight.Y);
                return lYResult != 0 ? lYResult : pLeft.X.CompareTo(pRight.X);
            });

            foreach (GridCoord lCell in lOrderedCells)
                yield return lCell;
        }

        public static IEnumerable<GridCoord> EnumerateMovementCandidates(EnemyAiContext pContext, bool pIncludeActorCell)
        {
            UnitRuntime lActor = pContext?.Actor;
            if (lActor == null)
                yield break;

            HashSet<GridCoord> lVisitedCells = new HashSet<GridCoord>();
            if (pIncludeActorCell && lVisitedCells.Add(lActor.Position))
                yield return lActor.Position;

            if (pContext.ReachableCells == null)
                yield break;

            foreach (GridCoord lCell in pContext.ReachableCells)
            {
                if (lVisitedCells.Add(lCell))
                    yield return lCell;
            }
        }

        public static bool TryGetApproachCell(EnemyAiContext pContext, out GridCoord pDestination)
        {
            pDestination = default;
            UnitRuntime lActor = pContext?.Actor;
            if (lActor == null)
                return false;

            int lBestScore = int.MinValue;
            foreach (GridCoord lCell in pContext.ReachableCells)
            {
                if (lCell == lActor.Position)
                    continue;

                int lScore = ScoreApproachCell(pContext, lCell);
                if (lScore <= lBestScore)
                    continue;

                lBestScore = lScore;
                pDestination = lCell;
            }

            return lBestScore > ScoreApproachCell(pContext, lActor.Position);
        }

        public static List<UnitRuntime> GetPriorityUnits(EnemyAiContext pContext, Team pTeam, EnemyAiTargetMode pTargetMode)
        {
            List<UnitRuntime> lUnits = GetPriorityUnits(pContext, pTeam);
            if (pTargetMode == EnemyAiTargetMode.BestScore || pContext?.Actor == null)
                return lUnits;

            lUnits.Sort((pLeft, pRight) => CompareByTargetMode(pContext.Actor, pLeft, pRight, pTargetMode));
            return lUnits;
        }

        private static void AddUnitTargetCells(ISet<GridCoord> pCells, UnitRuntime pActor, UnitRuntime pUnit, SkillDefinition pSkill)
        {
            if (pCells == null || pUnit == null || pSkill == null)
                return;

            int lAoeSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : Math.Max(0, pSkill.AoeSize);
            foreach (GridCoord lOccupiedCell in pUnit.EnumerateOccupiedCells())
            {
                pCells.Add(lOccupiedCell);
                GridCoord lDirection = pActor != null
                    ? GridLineOfSightUtility.ResolveAreaDirection(pActor.Position, lOccupiedCell)
                    : new GridCoord(0, 1);

                for (int lOffsetY = -lAoeSize; lOffsetY <= lAoeSize; lOffsetY++)
                {
                    for (int lOffsetX = -lAoeSize; lOffsetX <= lAoeSize; lOffsetX++)
                    {
                        if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lAoeSize, lDirection))
                            continue;

                        pCells.Add(new GridCoord(lOccupiedCell.X - lOffsetX, lOccupiedCell.Y - lOffsetY));
                    }
                }
            }
        }

        private static int CompareByPriority(UnitRuntime pActor, UnitRuntime pLeft, UnitRuntime pRight)
        {
            if (pLeft == pRight)
                return 0;
            if (pLeft == null)
                return 1;
            if (pRight == null)
                return -1;

            EnemyAiTargetPriority lTargetPriority = EnemyAiProfileSettings.FromActor(pActor).TargetPriority;
            int lResult = lTargetPriority switch
            {
                EnemyAiTargetPriority.StrongFirst => pRight.CurrentHealth.CompareTo(pLeft.CurrentHealth),
                EnemyAiTargetPriority.CloseFirst => pActor.Position.ManhattanDistanceTo(pLeft.Position)
                    .CompareTo(pActor.Position.ManhattanDistanceTo(pRight.Position)),
                _ => pLeft.CurrentHealth.CompareTo(pRight.CurrentHealth)
            };

            return lResult != 0 ? lResult : pLeft.Id.Value.CompareTo(pRight.Id.Value);
        }

        private static int CompareByTargetMode(UnitRuntime pActor, UnitRuntime pLeft, UnitRuntime pRight, EnemyAiTargetMode pTargetMode)
        {
            if (pLeft == pRight)
                return 0;
            if (pLeft == null)
                return 1;
            if (pRight == null)
                return -1;

            int lResult = pTargetMode switch
            {
                EnemyAiTargetMode.StrongFirst => pRight.CurrentHealth.CompareTo(pLeft.CurrentHealth),
                EnemyAiTargetMode.CloseFirst => pActor.Position.ManhattanDistanceTo(pLeft.Position)
                    .CompareTo(pActor.Position.ManhattanDistanceTo(pRight.Position)),
                EnemyAiTargetMode.MostMissingHealth => ResolveMissingHealth(pRight).CompareTo(ResolveMissingHealth(pLeft)),
                _ => pLeft.CurrentHealth.CompareTo(pRight.CurrentHealth)
            };

            return lResult != 0 ? lResult : pLeft.Id.Value.CompareTo(pRight.Id.Value);
        }

        private static Team ResolveTargetTeam(UnitRuntime pActor, SkillDefinition pSkill, UnitSkillAiOverride pRule)
        {
            if (pRule != null)
            {
                switch (pRule.TargetTeam)
                {
                    case EnemyAiTargetTeam.Enemy:
                        return pActor.Team == Team.TeamA ? Team.TeamB : Team.TeamA;

                    case EnemyAiTargetTeam.Ally:
                        return pActor.Team;
                }
            }

            return pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal
                ? pActor.Team
                : pActor.Team == Team.TeamA ? Team.TeamB : Team.TeamA;
        }

        private static int ResolveMissingHealth(UnitRuntime pUnit) =>
            pUnit != null ? Math.Max(0, pUnit.Definition.MaxHealth - pUnit.CurrentHealth) : 0;

        private static bool ShouldEnumerateWholeSkillRange(SkillDefinition pSkill)
        {
            if (pSkill == null || pSkill.AoeShape == SkillAoeShape.Single || pSkill.AoeSize <= 0)
                return false;

            return pSkill.PrimaryEffectType == SkillPrimaryEffectType.Damage
                || pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal
                || pSkill.AdditionalEffectType == SkillAdditionalEffectType.Push
                || pSkill.AppliedState != null;
        }

        private static IEnumerable<GridCoord> EnumerateSkillRangeCells(UnitRuntime pActor, SkillDefinition pSkill)
        {
            if (pActor == null || pSkill == null)
                yield break;

            int lRangeMin = pActor.GetSkillRangeMin(pSkill);
            int lRangeMax = pActor.GetSkillRangeMax(pSkill);
            for (int lOffsetY = -lRangeMax; lOffsetY <= lRangeMax; lOffsetY++)
            {
                for (int lOffsetX = -lRangeMax; lOffsetX <= lRangeMax; lOffsetX++)
                {
                    GridCoord lCell = new GridCoord(pActor.Position.X + lOffsetX, pActor.Position.Y + lOffsetY);
                    int lDistance = pActor.Position.ManhattanDistanceTo(lCell);
                    if (lDistance < lRangeMin || lDistance > lRangeMax)
                        continue;

                    if (!GridLineOfSightUtility.MatchesAlignment(pActor.Position, lCell, pSkill.TargetAlignment))
                        continue;

                    yield return lCell;
                }
            }
        }

        private static int ScoreApproachCell(EnemyAiContext pContext, GridCoord pCell)
        {
            UnitRuntime lActor = pContext.Actor;
            int lBestScore = int.MinValue;
            List<UnitRuntime> lTargets = GetPriorityUnits(pContext, Team.TeamA);

            foreach (SkillDefinition lSkill in lActor.Skills)
            {
                if (lSkill == null || lActor.RemainingActionPoints < lSkill.ActionPointCost)
                    continue;

                foreach (UnitRuntime lTarget in lTargets)
                {
                    int lDistance = pCell.ManhattanDistanceTo(lTarget.Position);
                    int lRangeMin = lActor.GetSkillRangeMin(lSkill);
                    int lRangeMax = lActor.GetSkillRangeMax(lSkill);
                    int lRangePenalty = lDistance < lRangeMin ? lRangeMin - lDistance : Math.Max(0, lDistance - lRangeMax);
                    int lScore = 100 - lRangePenalty * 20;

                    if (!GridLineOfSightUtility.MatchesAlignment(pCell, lTarget.Position, lSkill.TargetAlignment))
                        lScore -= 25;

                    lScore += Math.Max(0, lSkill.Power);
                    lBestScore = Math.Max(lBestScore, lScore);
                }
            }

            if (lBestScore != int.MinValue)
                return lBestScore;

            List<UnitRuntime> lFallbackTargets = GetPriorityUnits(pContext, Team.TeamA);
            if (lFallbackTargets.Count == 0)
                return int.MinValue;

            return -pCell.ManhattanDistanceTo(lFallbackTargets[0].Position);
        }
    }
}
