using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class BattlePassiveResolver
    {
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
