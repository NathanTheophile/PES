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
    internal static class EnemyAiImpactAnalyzer
    {
        public static EnemyAiImpactSummary Analyze(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            BattleActionResult pValidation)
        {
            EnemyAiImpactSummary lImpact = new EnemyAiImpactSummary();
            if (pContext?.BattleService?.Units == null || pContext.Actor == null || pSkill == null || pTarget == null)
                return lImpact;

            List<GridCoord> lCells = BuildAffectedCells(pContext.Actor, pSkill, pTarget.Cell);
            lImpact.Cells.AddRange(lCells);

            HashSet<UnitId> lVisitedUnits = new HashSet<UnitId>();
            foreach (UnitRuntime lUnit in pContext.BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive || !DoesAreaTouchUnit(lCells, lUnit))
                    continue;

                if (lUnit.Id == pContext.Actor.Id && !CanAffectActor(pContext.Actor, pSkill, pTarget))
                    continue;

                if (!lVisitedUnits.Add(lUnit.Id))
                    continue;

                AddUnitToImpact(pContext, pSkill, lImpact, lUnit);
            }

            if (lImpact.Units.Count == 0 && pValidation?.AffectedUnitIds != null)
                AddValidatedUnitsToImpact(pContext, pSkill, pValidation, lImpact, lVisitedUnits);

            return lImpact;
        }

        public static List<GridCoord> BuildAffectedCells(UnitRuntime pActor, SkillDefinition pSkill, GridCoord pOrigin)
        {
            List<GridCoord> lCells = new List<GridCoord>();
            int lSize = pSkill != null ? pSkill.AoeSize : 0;
            if (pSkill == null || pSkill.AoeShape == SkillAoeShape.Single || lSize <= 0)
            {
                lCells.Add(pOrigin);
                return lCells;
            }

            GridCoord lDirection = pActor != null
                ? GridLineOfSightUtility.ResolveAreaDirection(pActor.Position, pOrigin)
                : new GridCoord(0, 1);

            for (int lOffsetY = -lSize; lOffsetY <= lSize; lOffsetY++)
            {
                for (int lOffsetX = -lSize; lOffsetX <= lSize; lOffsetX++)
                {
                    if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lSize, lDirection))
                        continue;

                    lCells.Add(new GridCoord(pOrigin.X + lOffsetX, pOrigin.Y + lOffsetY));
                }
            }

            return lCells;
        }

        public static int DistanceToClosestCell(UnitRuntime pUnit, IReadOnlyList<GridCoord> pCells)
        {
            if (pUnit == null || pCells == null || pCells.Count == 0)
                return int.MaxValue;

            int lBestDistance = int.MaxValue;
            foreach (GridCoord lOccupiedCell in pUnit.EnumerateOccupiedCells())
            {
                for (int lIndex = 0; lIndex < pCells.Count; lIndex++)
                    lBestDistance = Math.Min(lBestDistance, lOccupiedCell.ManhattanDistanceTo(pCells[lIndex]));
            }

            return lBestDistance;
        }

        public static int DistanceToNearestHostile(EnemyAiContext pContext, GridCoord pCell)
        {
            UnitRuntime lActor = pContext.Actor;
            Team lHostileTeam = lActor.Team == Team.Player ? Team.Enemy : Team.Player;
            int lBestDistance = int.MaxValue;
            foreach (UnitRuntime lUnit in EnemyAiTargeting.GetPriorityUnits(pContext, lHostileTeam))
                lBestDistance = Math.Min(lBestDistance, DistanceToUnit(lUnit, pCell));

            return lBestDistance;
        }

        public static int DistanceToNearestAlly(EnemyAiContext pContext, GridCoord pCell, bool pIncludeActor)
        {
            int lBestDistance = int.MaxValue;
            foreach (UnitRuntime lUnit in EnemyAiTargeting.GetPriorityUnits(pContext, pContext.Actor.Team))
            {
                if (!pIncludeActor && lUnit.Id == pContext.Actor.Id)
                    continue;

                lBestDistance = Math.Min(lBestDistance, DistanceToUnit(lUnit, pCell));
            }

            return lBestDistance == int.MaxValue ? 0 : lBestDistance;
        }

        public static int DistanceToUnit(UnitRuntime pUnit, GridCoord pCell)
        {
            if (pUnit == null)
                return int.MaxValue;

            int lBestDistance = int.MaxValue;
            foreach (GridCoord lOccupiedCell in pUnit.EnumerateOccupiedCells())
                lBestDistance = Math.Min(lBestDistance, lOccupiedCell.ManhattanDistanceTo(pCell));

            return lBestDistance;
        }

        public static int ResolveAoeFalloffPower(int pBasePower, SkillDefinition pSkill, UnitRuntime pTarget, GridCoord pCenterCell)
        {
            if (pBasePower <= 0 || pSkill == null || pSkill.AoeDamageFalloffPercentPerCell <= 0)
                return Math.Max(0, pBasePower);

            int lDistanceFromCenter = DistanceToUnit(pTarget, pCenterCell);
            if (lDistanceFromCenter <= 0 || lDistanceFromCenter == int.MaxValue)
                return pBasePower;

            int lRemainingPercent = Math.Max(0, 100 - lDistanceFromCenter * pSkill.AoeDamageFalloffPercentPerCell);
            return pBasePower * lRemainingPercent / 100;
        }

        public static GridCoord ResolvePushDirection(GridCoord pOrigin, GridCoord pTarget)
        {
            int lDeltaX = pTarget.X - pOrigin.X;
            int lDeltaY = pTarget.Y - pOrigin.Y;
            int lAbsX = Math.Abs(lDeltaX);
            int lAbsY = Math.Abs(lDeltaY);

            if (lDeltaX == 0 && lDeltaY == 0)
                return new GridCoord(0, 0);

            if (lAbsX == lAbsY)
                return new GridCoord(Math.Sign(lDeltaX), Math.Sign(lDeltaY));

            if (lAbsX > lAbsY)
                return new GridCoord(Math.Sign(lDeltaX), 0);

            return new GridCoord(0, Math.Sign(lDeltaY));
        }

        private static void AddValidatedUnitsToImpact(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            BattleActionResult pValidation,
            EnemyAiImpactSummary pImpact,
            ISet<UnitId> pVisitedUnits)
        {
            for (int lIndex = 0; lIndex < pValidation.AffectedUnitIds.Count; lIndex++)
            {
                UnitId lUnitId = pValidation.AffectedUnitIds[lIndex];
                if (lUnitId == pContext.Actor.Id
                    || !pVisitedUnits.Add(lUnitId)
                    || !pContext.BattleService.TryGetUnit(lUnitId, out UnitRuntime lUnit)
                    || lUnit == null
                    || !lUnit.IsAlive)
                {
                    continue;
                }

                AddUnitToImpact(pContext, pSkill, pImpact, lUnit);
            }
        }

        private static void AddUnitToImpact(EnemyAiContext pContext, SkillDefinition pSkill, EnemyAiImpactSummary pImpact, UnitRuntime pUnit)
        {
            pImpact.Units.Add(pUnit);
            bool lIsAlly = pUnit.Team == pContext.Actor.Team;
            if (lIsAlly)
                pImpact.AllyCount++;
            else
                pImpact.HostileCount++;

            if (pUnit.Id == pContext.Actor.Id)
                pImpact.HitsActor = true;

            if (pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal && lIsAlly)
                pImpact.EffectiveHealTotal += Math.Min(
                    Math.Max(0, pUnit.Definition.MaxHealth - pUnit.CurrentHealth),
                    pSkill.Power);
        }

        private static bool DoesAreaTouchUnit(IReadOnlyList<GridCoord> pCells, UnitRuntime pUnit)
        {
            if (pCells == null || pUnit == null)
                return false;

            foreach (GridCoord lOccupiedCell in pUnit.EnumerateOccupiedCells())
            {
                for (int lIndex = 0; lIndex < pCells.Count; lIndex++)
                {
                    if (pCells[lIndex] == lOccupiedCell)
                        return true;
                }
            }

            return false;
        }

        private static bool CanAffectActor(UnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget)
        {
            if (pActor == null || pSkill == null || pTarget == null || !pSkill.CanAffectCaster)
                return false;

            int lAoeSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : Math.Max(0, pSkill.AoeSize);
            GridCoord lDirection = GridLineOfSightUtility.ResolveAreaDirection(pActor.Position, pTarget.Cell);
            foreach (GridCoord lCell in pActor.EnumerateOccupiedCells())
            {
                int lOffsetX = lCell.X - pTarget.Cell.X;
                int lOffsetY = lCell.Y - pTarget.Cell.Y;
                if (GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lAoeSize, lDirection))
                    return true;
            }

            return false;
        }
    }

    internal sealed class EnemyAiImpactSummary
    {
        public readonly List<GridCoord> Cells = new List<GridCoord>();
        public readonly List<UnitRuntime> Units = new List<UnitRuntime>();
        public int HostileCount;
        public int AllyCount;
        public int EffectiveHealTotal;
        public bool HitsActor;
    }
}
