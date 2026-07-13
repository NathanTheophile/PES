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
    internal static class EnemyAiEffectScorer
    {
        public static int ScoreAffectedUnits(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            BattleActionResult pValidation,
            EnemyAiImpactSummary pImpact)
        {
            int lScore = 0;
            IReadOnlyList<UnitRuntime> lUnits = pImpact.Units;
            if ((lUnits == null || lUnits.Count == 0) && pValidation.AffectedUnitIds == null)
                return lScore;

            if (lUnits != null && lUnits.Count > 0)
            {
                for (int lIndex = 0; lIndex < lUnits.Count; lIndex++)
                {
                    UnitRuntime lUnit = lUnits[lIndex];
                    if (lUnit == null || !lUnit.IsAlive)
                        continue;

                    bool lIsAlly = lUnit.Team == pContext.Actor.Team;
                    lScore += ScorePrimaryEffectOnUnit(pContext, pSkill, pTarget.Cell, lUnit, lIsAlly);
                    lScore += ScoreAdditionalEffectOnUnit(pContext, pSkill, pTarget, lUnit, lIsAlly);
                    lScore += ScoreStateOnUnit(pSkill, lUnit, lIsAlly);
                }

                return lScore;
            }

            for (int lIndex = 0; lIndex < pValidation.AffectedUnitIds.Count; lIndex++)
            {
                UnitId lUnitId = pValidation.AffectedUnitIds[lIndex];
                if (!pContext.BattleService.TryGetUnit(lUnitId, out UnitRuntime lUnit)
                    || lUnit == null
                    || !lUnit.IsAlive
                    || lUnit.Id == pContext.Actor.Id)
                {
                    continue;
                }

                bool lIsAlly = lUnit.Team == pContext.Actor.Team;
                lScore += ScorePrimaryEffectOnUnit(pContext, pSkill, pTarget.Cell, lUnit, lIsAlly);
                lScore += ScoreAdditionalEffectOnUnit(pContext, pSkill, pTarget, lUnit, lIsAlly);
                lScore += ScoreStateOnUnit(pSkill, lUnit, lIsAlly);
            }

            return lScore;
        }

        public static int ScoreImpactShape(EnemyAiContext pContext, SkillDefinition pSkill, EnemyAiImpactSummary pImpact)
        {
            if (pImpact == null)
                return 0;

            EnemyAiProfileSettings lSettings = EnemyAiProfileSettings.FromActor(pContext.Actor);
            int lScore = 0;
            bool lOffensive = EnemyAiSkillScorer.IsOffensiveSkill(pSkill);
            if (lOffensive)
            {
                lScore += WeightScore(pImpact.HostileCount * 14, lSettings.AoeWeight);
                lScore -= WeightScore(pImpact.AllyCount * 22, lSettings.SafetyWeight);
                if (pImpact.HitsActor)
                    lScore -= WeightScore(45, lSettings.SafetyWeight);
            }

            if (pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal)
            {
                if (pImpact.AllyCount == 0 || pImpact.EffectiveHealTotal <= 0)
                    lScore -= 80;

                lScore += WeightScore(Math.Min(36, pImpact.EffectiveHealTotal * 2), lSettings.HealWeight);
                lScore -= WeightScore(pImpact.HostileCount * 35, lSettings.SafetyWeight);
            }

            if (pSkill.AoeShape != SkillAoeShape.Single && pSkill.AoeSize > 0)
            {
                lScore += WeightScore(Math.Max(0, pImpact.HostileCount - 1) * 18, lSettings.AoeWeight);
                lScore -= WeightScore(Math.Max(0, pImpact.AllyCount - 1) * 20, lSettings.SafetyWeight);
            }

            return lScore;
        }

        public static int ScoreBoardEffect(EnemyAiContext pContext, SkillDefinition pSkill, SkillTarget pTarget, EnemyAiImpactSummary pImpact)
        {
            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.Summon:
                    return ScoreSummon(pContext, pSkill, pTarget.Cell);

                case SkillAdditionalEffectType.CreateGlyph:
                    return WeightScore(ScoreGlyph(pContext, pSkill, pTarget.Cell, pImpact), EnemyAiProfileSettings.FromActor(pContext.Actor).AoeWeight);

                case SkillAdditionalEffectType.Teleport:
                    return ScoreTeleport(pContext, pTarget.Cell);

                default:
                    return 0;
            }
        }

        private static int ScorePrimaryEffectOnUnit(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            GridCoord pCenterCell,
            UnitRuntime pUnit,
            bool pIsAlly)
        {
            UnitRuntime lActor = pContext.Actor;
            EnemyAiProfileSettings lSettings = EnemyAiProfileSettings.FromActor(lActor);
            switch (pSkill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    int lFalloffPower = EnemyAiImpactAnalyzer.ResolveAoeFalloffPower(pSkill.Power, pSkill, pUnit, pCenterCell);
                    DamageRangeType lDamageRange = pSkill.CategoryDamageRange != DamageRangeType.None
                        ? pSkill.CategoryDamageRange
                        : lActor.ResolveDamageRangeTo(pUnit);
                    int lResolvedDamage = pUnit.ResolveIncomingDamage(lActor.ResolveOutgoingDamage(lFalloffPower, lDamageRange), lDamageRange);
                    int lEffectiveDamage = Math.Min(pUnit.CurrentHealth, lResolvedDamage);
                    int lOverkillPenalty = Math.Max(0, lResolvedDamage - pUnit.CurrentHealth);
                    if (pIsAlly)
                        return -WeightScore(35 + lEffectiveDamage * 9, lSettings.SafetyWeight);

                    int lKillBonus = lEffectiveDamage >= pUnit.CurrentHealth ? 45 : 0;
                    return WeightScore(lEffectiveDamage * 8, lSettings.DamageWeight)
                        + WeightScore(lKillBonus, lSettings.KillConfirmWeight)
                        - lOverkillPenalty;

                case SkillPrimaryEffectType.Heal:
                    int lMissingHealth = Math.Max(0, pUnit.Definition.MaxHealth - pUnit.CurrentHealth);
                    int lEffectiveHeal = Math.Min(lMissingHealth, pSkill.Power);
                    if (pIsAlly)
                    {
                        if (lEffectiveHeal <= 0)
                            return -12;

                        int lEmergencyBonus = pUnit.CurrentHealth * 100 <= pUnit.Definition.MaxHealth * 35 ? 18 : 0;
                        return WeightScore(lEffectiveHeal * 7 + lEmergencyBonus, lSettings.HealWeight);
                    }

                    return -WeightScore(25 + lEffectiveHeal * 8, lSettings.SafetyWeight);

                default:
                    return 0;
            }
        }

        private static int ScoreAdditionalEffectOnUnit(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            UnitRuntime pUnit,
            bool pIsAlly)
        {
            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.Push:
                    return ScorePushOnUnit(pContext, pSkill, pTarget, pUnit, pIsAlly);

                case SkillAdditionalEffectType.SwitchPositions:
                    return pIsAlly ? -8 : 10;

                default:
                    return 0;
            }
        }

        private static int ScoreStateOnUnit(SkillDefinition pSkill, UnitRuntime pUnit, bool pIsAlly)
        {
            if (pSkill.AppliedState == null)
                return 0;

            int lExistingStacks = pUnit.GetStateStacks(pSkill.AppliedState);
            int lStackValue = lExistingStacks > 0 ? 5 : 16;

            if (pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal)
                return pIsAlly ? lStackValue : -lStackValue;

            return pIsAlly ? -lStackValue : lStackValue;
        }

        private static int ScorePushOnUnit(
            EnemyAiContext pContext,
            SkillDefinition pSkill,
            SkillTarget pTarget,
            UnitRuntime pUnit,
            bool pIsAlly)
        {
            if (pIsAlly)
                return -24 - Math.Max(0, pSkill.PushDistance) * 3;

            UnitRuntime lActor = pContext.Actor;
            GridCoord lDirection = EnemyAiImpactAnalyzer.ResolvePushDirection(lActor.Position, pUnit.Position);
            GridCoord lEstimatedDestination = new GridCoord(
                pUnit.Position.X + lDirection.X * Math.Max(0, pSkill.PushDistance),
                pUnit.Position.Y + lDirection.Y * Math.Max(0, pSkill.PushDistance));

            int lStartDistance = EnemyAiImpactAnalyzer.DistanceToNearestAlly(pContext, pUnit.Position, true);
            int lEndDistance = EnemyAiImpactAnalyzer.DistanceToNearestAlly(pContext, lEstimatedDestination, true);
            int lDistanceDelta = lEndDistance - lStartDistance;
            int lScore = 18 + Math.Max(0, pSkill.PushDistance) * 3;

            if (pSkill.PrimaryEffectType != SkillPrimaryEffectType.Damage && pSkill.AppliedState == null && lDistanceDelta > 0)
                lScore -= lDistanceDelta * 12;
            else if (lDistanceDelta < 0)
                lScore += Math.Min(16, -lDistanceDelta * 6);

            if (pTarget != null && pUnit.Position == pTarget.Cell)
                lScore += 4;

            return lScore;
        }

        private static int ScoreSummon(EnemyAiContext pContext, SkillDefinition pSkill, GridCoord pCell)
        {
            int lScore = 18 + ScoreCellNearHostiles(pContext, pCell, 10);
            int lAllyProximity = ScoreCellNearAllies(pContext, pCell, 6);
            lScore += Math.Min(8, lAllyProximity);

            int lHostileDistance = EnemyAiImpactAnalyzer.DistanceToNearestHostile(pContext, pCell);
            if (lHostileDistance <= 2)
                lScore += 14;
            else if (lHostileDistance >= 6)
                lScore -= 12;

            return pSkill.SummonUnit != null ? lScore : -100;
        }

        private static int ScoreGlyph(EnemyAiContext pContext, SkillDefinition pSkill, GridCoord pCell, EnemyAiImpactSummary pImpact)
        {
            int lScore = 8;
            IReadOnlyList<GridCoord> lCells = pImpact != null && pImpact.Cells.Count > 0
                ? pImpact.Cells
                : EnemyAiImpactAnalyzer.BuildAffectedCells(pContext.Actor, pSkill, pCell);

            foreach (UnitRuntime lUnit in pContext.BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive)
                    continue;

                int lDistance = EnemyAiImpactAnalyzer.DistanceToClosestCell(lUnit, lCells);
                int lProximityScore = Math.Max(0, 12 - lDistance * 4);
                if (lUnit.Team == pContext.Actor.Team)
                    lScore -= lProximityScore + (lDistance == 0 ? 20 : 0);
                else
                    lScore += lProximityScore + (lDistance == 0 ? 18 : 0);
            }

            return lScore;
        }

        private static int ScoreTeleport(EnemyAiContext pContext, GridCoord pCell)
        {
            UnitRuntime lActor = pContext.Actor;
            int lCurrentPressure = ScoreCellNearHostiles(pContext, lActor.Position, 4);
            int lDestinationPressure = ScoreCellNearHostiles(pContext, pCell, 4);

            return lDestinationPressure - lCurrentPressure;
        }

        private static int ScoreCellNearHostiles(EnemyAiContext pContext, GridCoord pCell, int pMaxBonus)
        {
            UnitRuntime lActor = pContext.Actor;
            Team lHostileTeam = lActor.Team == Team.TeamA ? Team.TeamB : Team.TeamA;
            return ScoreCellNearTeam(pContext, pCell, lHostileTeam, pMaxBonus);
        }

        private static int ScoreCellNearAllies(EnemyAiContext pContext, GridCoord pCell, int pMaxBonus) =>
            ScoreCellNearTeam(pContext, pCell, pContext.Actor.Team, pMaxBonus);

        private static int ScoreCellNearTeam(EnemyAiContext pContext, GridCoord pCell, Team pTeam, int pMaxBonus)
        {
            int lBestScore = 0;
            foreach (UnitRuntime lUnit in EnemyAiTargeting.GetPriorityUnits(pContext, pTeam))
            {
                int lDistance = pCell.ManhattanDistanceTo(lUnit.Position);
                lBestScore = Math.Max(lBestScore, Math.Max(0, pMaxBonus - lDistance * 2));
            }

            return lBestScore;
        }

        private static int WeightScore(int pScore, int pWeight) =>
            pScore * Math.Max(0, pWeight) / 100;
    }
}
