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
    internal static class SkillDamageResolver
    {
        public static BattleActionResult ApplyDamage(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedUnitCount = 0;
            int lTotalValue = 0;
            List<UnitId> lAffectedUnitIds = new List<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive)
                    continue;

                int lBaseDamage = Math.Max(0, pSkill.Power);
                int lFalloffDamage = ResolveAoeFalloffDamage(lBaseDamage, pSkill, lTarget, pResolvedTarget.TargetCell);
                DamageRangeType lDamageRange = pSkill.CategoryDamageRange != DamageRangeType.None
                    ? pSkill.CategoryDamageRange
                    : pActor.ResolveDamageRangeTo(lTarget);
                int lResolvedDamage = pActor.ResolveOutgoingDamage(lFalloffDamage, lDamageRange);
                lTotalValue += lTarget.ApplyDamage(lResolvedDamage, lDamageRange);
                lAffectedUnitCount++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                ResolveEffectMessage(pActor, pResolvedTarget, lAffectedUnitCount, lTotalValue, "dealt", "damage"),
                lAffectedUnitIds,
                null,
                lAffectedUnitCount > 0 ? BattleActionOutcomeFlags.PrimaryDamage : BattleActionOutcomeFlags.None);
        }

        public static BattleActionResult ApplyHeal(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedUnitCount = 0;
            int lTotalValue = 0;
            List<UnitId> lAffectedUnitIds = new List<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive)
                    continue;

                lTotalValue += lTarget.RestoreHealth(pSkill.Power);
                lAffectedUnitCount++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                ResolveEffectMessage(pActor, pResolvedTarget, lAffectedUnitCount, lTotalValue, "restored", "health"),
                lAffectedUnitIds,
                null,
                lTotalValue > 0 ? BattleActionOutcomeFlags.Heal : BattleActionOutcomeFlags.None);
        }

        public static BattleActionResult ApplyState(UnitRuntime pActor, SkillDefinition pSkill, ResolvedSkillTarget pResolvedTarget)
        {
            int lAffectedCount = 0;
            HashSet<UnitId> lAffectedUnitIds = new HashSet<UnitId> { pActor.Id };

            foreach (UnitRuntime lTarget in pResolvedTarget.AffectedUnits)
            {
                if (lTarget == null || !lTarget.IsAlive)
                    continue;

                if (!lTarget.TryApplyState(pSkill.AppliedState, pSkill.AppliedStateStacks, pSkill.AppliedStateDurationTurns))
                    continue;

                lAffectedCount++;
                lAffectedUnitIds.Add(lTarget.Id);
            }

            string lMessage = lAffectedCount > 0
                ? $"{pActor.Definition.DisplayName} applied {pSkill.AppliedState.DisplayName} to {lAffectedCount} unit(s)."
                : string.Empty;

            return BattleActionResult.Succeeded(
                BattleActionType.Skill,
                lMessage,
                lAffectedUnitIds,
                null,
                lAffectedCount > 0 ? BattleActionOutcomeFlags.StateApplied : BattleActionOutcomeFlags.None);
        }

        private static int ResolveAoeFalloffDamage(int pBaseDamage, SkillDefinition pSkill, UnitRuntime pTarget, GridCoord pCenterCell)
        {
            if (pBaseDamage <= 0 || pSkill == null || pSkill.AoeDamageFalloffPercentPerCell <= 0)
                return Math.Max(0, pBaseDamage);

            int lDistanceFromCenter = ResolveClosestOccupiedCellDistance(pTarget, pCenterCell);
            if (lDistanceFromCenter <= 0)
                return pBaseDamage;

            int lRemainingPercent = Math.Max(0, 100 - lDistanceFromCenter * pSkill.AoeDamageFalloffPercentPerCell);
            return pBaseDamage * lRemainingPercent / 100;
        }

        private static string ResolveEffectMessage(
            UnitRuntime pActor,
            ResolvedSkillTarget pResolvedTarget,
            int pAffectedUnitCount,
            int pTotalValue,
            string pVerb,
            string pResourceLabel)
        {
            if (pAffectedUnitCount == 1 && pResolvedTarget.AffectedUnits.Count > 0 && pResolvedTarget.AffectedUnits[0] != null)
                return $"{pActor.Definition.DisplayName} {pVerb} {pTotalValue} {pResourceLabel} to {pResolvedTarget.AffectedUnits[0].Definition.DisplayName}.";

            return $"{pActor.Definition.DisplayName} {pVerb} {pTotalValue} {pResourceLabel} across {pAffectedUnitCount} unit(s).";
        }

        private static int ResolveClosestOccupiedCellDistance(UnitRuntime pTarget, GridCoord pCell)
        {
            if (pTarget == null)
                return 0;

            int lBestDistance = int.MaxValue;
            foreach (GridCoord lOccupiedCell in pTarget.EnumerateOccupiedCells())
                lBestDistance = Math.Min(lBestDistance, lOccupiedCell.ManhattanDistanceTo(pCell));

            return lBestDistance == int.MaxValue ? 0 : lBestDistance;
        }

    }
}
