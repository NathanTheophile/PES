using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class BattlePassiveResolver
    {
        private const string PASSIVE_MARKER_PREFIX = "passive-marker::";
        private const string PASSIVE_EFFECT_PREFIX = "passive-effect::";

        public static void Resolve(
            UnitRuntime pActor,
            SkillResolutionReport pReport,
            IEnumerable<UnitRuntime> pUnits)
        {
            if (pActor == null || pReport == null || pUnits == null)
                return;

            List<UnitRuntime> lUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit != null)
                    lUnits.Add(lUnit);
            }

            lUnits.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            for (int lIndex = 0; lIndex < lUnits.Count; lIndex++)
                ResolveUnitPassive(pActor, lUnits[lIndex], pReport, lUnits);

            ResolveObservedHealthLoss(
                new HealthLossResolutionReport(
                    pActor.Id,
                    pActor.Team,
                    HealthLossResolutionKind.Skill,
                    pReport.HealthLosses),
                lUnits);
        }

        public static void ResolveBeginTurn(UnitRuntime pOwner, IEnumerable<UnitRuntime> pUnits)
        {
            PassiveDefinition lPassive = pOwner?.ActivePassive;
            if (lPassive == null || pUnits == null)
                return;

            List<UnitRuntime> lUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit != null && lUnit.IsAlive)
                    lUnits.Add(lUnit);
            }

            lUnits.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            CleanupOwnedEffects(pOwner, lUnits);
            SelectAutomaticTarget(pOwner, lPassive, lUnits);

            if (lPassive.Trigger != PassiveTrigger.OwnerBeginTurn)
                return;

            switch (lPassive.BeginTurnEffect)
            {
                case PassiveBeginTurnEffect.RestoreOwnedUnits:
                    RestoreOwnedUnits(pOwner, lPassive, lUnits);
                    break;

                case PassiveBeginTurnEffect.ApplyOwnedUnitAuras:
                    ApplyOwnedUnitAuras(pOwner, lPassive, lUnits);
                    break;
            }
        }

        public static void ResolveObservedHealthLoss(
            HealthLossResolutionReport pReport,
            IEnumerable<UnitRuntime> pUnits)
        {
            if (pReport == null || pUnits == null || pReport.HealthLosses.Count == 0)
                return;

            List<UnitRuntime> lUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit != null)
                    lUnits.Add(lUnit);
            }

            lUnits.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            for (int lOwnerIndex = 0; lOwnerIndex < lUnits.Count; lOwnerIndex++)
            {
                UnitRuntime lOwner = lUnits[lOwnerIndex];
                PassiveDefinition lPassive = lOwner.IsAlive ? lOwner.ActivePassive : null;
                if (lPassive?.AutomaticTargetMarker == null
                    || lPassive.HealthLossReactionTarget == PassiveHealthLossReactionTarget.None
                    || lPassive.HealthLossReactionState == null)
                {
                    continue;
                }

                UnitRuntime lMarkedUnit = FindMarkedUnit(lOwner, lPassive, lUnits);
                if (lMarkedUnit == null
                    || pReport.GetHealthLost(lMarkedUnit.Id) <= 0
                    || !CanReactToSource(lPassive.HealthLossSourceRule, lMarkedUnit.Team, pReport.SourceTeam))
                {
                    continue;
                }

                ApplyHealthLossReaction(lOwner, lMarkedUnit, lPassive, lUnits);
            }
        }

        public static void CleanupOwnedEffects(UnitRuntime pOwner, IEnumerable<UnitRuntime> pUnits)
        {
            if (pOwner == null || pUnits == null)
                return;

            string lMarkerPrefix = $"{PASSIVE_MARKER_PREFIX}{pOwner.Id.Value}::";
            string lEffectPrefix = $"{PASSIVE_EFFECT_PREFIX}{pOwner.Id.Value}::";
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit == null)
                    continue;

                List<string> lKeysToRemove = new List<string>();
                foreach (BattleStateRuntime lState in lUnit.ActiveStates)
                {
                    if (lState == null)
                        continue;

                    if (lState.Key.StartsWith(lMarkerPrefix, System.StringComparison.Ordinal)
                        || lState.Key.StartsWith(lEffectPrefix, System.StringComparison.Ordinal))
                    {
                        lKeysToRemove.Add(lState.Key);
                    }
                }

                lKeysToRemove.Sort(System.StringComparer.Ordinal);
                for (int lIndex = 0; lIndex < lKeysToRemove.Count; lIndex++)
                    lUnit.RemoveStateByKey(lKeysToRemove[lIndex]);
            }
        }

        private static void SelectAutomaticTarget(
            UnitRuntime pOwner,
            PassiveDefinition pPassive,
            IReadOnlyList<UnitRuntime> pUnits)
        {
            if (pPassive.AutomaticTargetSelection == PassiveAutomaticTargetSelection.None
                || pPassive.AutomaticTargetMarker == null)
            {
                return;
            }

            UnitRuntime lSelectedUnit = null;
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitRuntime lCandidate = pUnits[lIndex];
                if (lCandidate == null
                    || !lCandidate.IsAlive
                    || lCandidate.Id == pOwner.Id
                    || lCandidate.Team != pOwner.Team
                    || !MatchesUnitType(lCandidate, pPassive.AutomaticTargetUnitType))
                {
                    continue;
                }

                if (lSelectedUnit == null || CompareHealthPercentage(lCandidate, lSelectedUnit) < 0)
                    lSelectedUnit = lCandidate;
            }

            if (lSelectedUnit == null)
                return;

            lSelectedUnit.SetPersistentState(
                ResolveMarkerKey(pOwner, pPassive),
                pPassive.AutomaticTargetMarker,
                1,
                pOwner.Id,
                pOwner.Team,
                pPassive.Id);
        }

        private static bool MatchesUnitType(UnitRuntime pUnit, UnitTargetType pTargetUnitType)
        {
            if (pUnit == null)
                return false;

            return pTargetUnitType switch
            {
                UnitTargetType.CharactersOnly => pUnit.IsCharacter,
                UnitTargetType.SummonsOnly => pUnit.IsSummon,
                _ => true
            };
        }

        private static UnitRuntime FindMarkedUnit(
            UnitRuntime pOwner,
            PassiveDefinition pPassive,
            IReadOnlyList<UnitRuntime> pUnits)
        {
            string lMarkerKey = ResolveMarkerKey(pOwner, pPassive);
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitRuntime lUnit = pUnits[lIndex];
                if (lUnit?.GetStateByKey(lMarkerKey)?.Definition == pPassive.AutomaticTargetMarker)
                    return lUnit;
            }

            return null;
        }

        private static void ApplyHealthLossReaction(
            UnitRuntime pOwner,
            UnitRuntime pMarkedUnit,
            PassiveDefinition pPassive,
            IReadOnlyList<UnitRuntime> pUnits)
        {
            if (pPassive.HealthLossReactionTarget == PassiveHealthLossReactionTarget.MarkedUnit)
            {
                ApplyReactionState(pOwner, pMarkedUnit, pPassive);
                return;
            }

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitRuntime lBeneficiary = pUnits[lIndex];
                if (lBeneficiary == null
                    || !lBeneficiary.IsAlive
                    || lBeneficiary.Id == pMarkedUnit.Id
                    || lBeneficiary.Team != pMarkedUnit.Team
                    || (!pPassive.IncludeSummonsAsReactionBeneficiaries && lBeneficiary.OwnerUnitId.IsValid))
                {
                    continue;
                }

                ApplyReactionState(pOwner, lBeneficiary, pPassive);
            }
        }

        private static void ApplyReactionState(UnitRuntime pOwner, UnitRuntime pTarget, PassiveDefinition pPassive)
        {
            if (pPassive.ClearReactionStateOnOwnerBeginTurn)
            {
                string lEffectKey = ResolveEffectKey(pOwner, pPassive);
                int lExistingStacks = pTarget.GetStateByKey(lEffectKey)?.Stacks ?? 0;
                pTarget.SetPersistentState(
                    lEffectKey,
                    pPassive.HealthLossReactionState,
                    lExistingStacks + pPassive.HealthLossReactionStateStacks,
                    pOwner.Id,
                    pOwner.Team,
                    pPassive.Id);
                return;
            }

            pTarget.TryApplyState(
                pPassive.HealthLossReactionState,
                pPassive.HealthLossReactionStateStacks,
                pPassive.HealthLossReactionState.DurationTurns,
                pOwner.Id,
                pOwner.Team,
                pPassive.Id);
        }

        private static bool CanReactToSource(
            PassiveHealthLossSourceRule pRule,
            Team pMarkedTeam,
            Team pSourceTeam)
        {
            return pRule switch
            {
                PassiveHealthLossSourceRule.EnemiesOnly => IsOpposingPlayableTeam(pMarkedTeam, pSourceTeam),
                PassiveHealthLossSourceRule.AlliesOnly => pMarkedTeam == pSourceTeam,
                _ => true
            };
        }

        private static int CompareHealthPercentage(UnitRuntime pLeft, UnitRuntime pRight)
        {
            long lLeft = (long)pLeft.CurrentHealth * System.Math.Max(1, pRight.CurrentMaxHealth);
            long lRight = (long)pRight.CurrentHealth * System.Math.Max(1, pLeft.CurrentMaxHealth);
            int lResult = lLeft.CompareTo(lRight);
            return lResult != 0 ? lResult : pLeft.Id.Value.CompareTo(pRight.Id.Value);
        }

        private static string ResolveMarkerKey(UnitRuntime pOwner, PassiveDefinition pPassive) =>
            $"{PASSIVE_MARKER_PREFIX}{pOwner.Id.Value}::{pPassive.Id}";

        private static string ResolveEffectKey(UnitRuntime pOwner, PassiveDefinition pPassive) =>
            $"{PASSIVE_EFFECT_PREFIX}{pOwner.Id.Value}::{pPassive.Id}";

        private static void RestoreOwnedUnits(UnitRuntime pOwner, PassiveDefinition pPassive, IReadOnlyList<UnitRuntime> pUnits)
        {
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitRuntime lUnit = pUnits[lIndex];
                if (lUnit.IsOwnedBy(pOwner)
                    && (pPassive.OwnedUnitRequiredState == null || lUnit.HasState(pPassive.OwnedUnitRequiredState)))
                {
                    lUnit.RestoreDirectHealth(pPassive.OwnedUnitRestoreHealth);
                }
            }
        }

        private static void ApplyOwnedUnitAuras(UnitRuntime pOwner, PassiveDefinition pPassive, IReadOnlyList<UnitRuntime> pUnits)
        {
            for (int lAuraIndex = 0; lAuraIndex < pPassive.OwnedUnitAuras.Count; lAuraIndex++)
            {
                PassiveOwnedUnitAura lAura = pPassive.OwnedUnitAuras[lAuraIndex];
                if (lAura?.AppliedState == null || lAura.SourceUnit == null)
                    continue;

                for (int lSourceIndex = 0; lSourceIndex < pUnits.Count; lSourceIndex++)
                {
                    UnitRuntime lSource = pUnits[lSourceIndex];
                    if (!lSource.IsOwnedBy(pOwner)
                        || lSource.Definition != lAura.SourceUnit
                        || (lAura.RequiredSourceState != null && !lSource.HasState(lAura.RequiredSourceState)))
                    {
                        continue;
                    }

                    for (int lTargetIndex = 0; lTargetIndex < pUnits.Count; lTargetIndex++)
                    {
                        UnitRuntime lTarget = pUnits[lTargetIndex];
                        if (lTarget.Team != pOwner.Team
                            || (lAura.ExcludedBeneficiaryState != null && lTarget.HasState(lAura.ExcludedBeneficiaryState))
                            || lSource.Position.ManhattanDistanceTo(lTarget.Position) > lAura.Range)
                        {
                            continue;
                        }

                        lTarget.TryApplyState(
                            lAura.AppliedState,
                            1,
                            lAura.AppliedState.DurationTurns,
                            pOwner.Id,
                            pOwner.Team,
                            pPassive.Id);
                    }
                }
            }
        }

        private static void ResolveUnitPassive(
            UnitRuntime pActor,
            UnitRuntime pOwner,
            SkillResolutionReport pReport,
            IReadOnlyList<UnitRuntime> pUnits)
        {
            PassiveDefinition lPassive = pOwner.ActivePassive;
            StateProgressionDefinition lProgression = lPassive?.StateProgression;
            if (lPassive == null || lProgression == null)
                return;

            bool lTriggered = lPassive.Trigger switch
            {
                PassiveTrigger.OwnerDealtSkillDamageToEnemy =>
                    pOwner.Id == pActor.Id
                    && (!pReport.UsedVariant || lPassive.TriggerOnOwnerVariantExecutions)
                    && DidDamageEnemy(pOwner, pReport, pUnits),

                PassiveTrigger.OwnerTookSkillDamageFromEnemy =>
                    pOwner.Id != pActor.Id
                    && IsOpposingPlayableTeam(pOwner.Team, pActor.Team)
                    && pReport.GetHealthLost(pOwner.Id) > 0,

                _ => false
            };

            if (lTriggered)
                pOwner.AdvanceStateProgression(lProgression, lPassive.ProgressionStepsPerTrigger);
        }

        private static bool DidDamageEnemy(
            UnitRuntime pOwner,
            SkillResolutionReport pReport,
            IReadOnlyList<UnitRuntime> pUnits)
        {
            for (int lIndex = 0; lIndex < pReport.HealthLosses.Count; lIndex++)
            {
                SkillHealthLoss lHealthLoss = pReport.HealthLosses[lIndex];
                if (lHealthLoss.Amount <= 0)
                    continue;

                for (int lUnitIndex = 0; lUnitIndex < pUnits.Count; lUnitIndex++)
                {
                    UnitRuntime lTarget = pUnits[lUnitIndex];
                    if (lTarget.Id == lHealthLoss.UnitId && IsOpposingPlayableTeam(pOwner.Team, lTarget.Team))
                        return true;
                }
            }

            return false;
        }

        private static bool IsOpposingPlayableTeam(Team pSource, Team pTarget) =>
            (pSource == Team.TeamA && pTarget == Team.TeamB)
            || (pSource == Team.TeamB && pTarget == Team.TeamA);
    }
}
