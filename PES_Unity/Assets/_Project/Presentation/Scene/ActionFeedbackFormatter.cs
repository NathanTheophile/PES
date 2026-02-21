using PES.Combat.Actions;
using PES.Core.Simulation;

namespace PES.Presentation.Scene
{
    public static class ActionFeedbackFormatter
    {
        public static string FormatResolutionSummary(ActionResolution resolution)
        {
            if (resolution.Code == ActionResolutionCode.Succeeded)
            {
                return $"✅ {resolution.Description}";
            }

            if (resolution.Code == ActionResolutionCode.Missed)
            {
                return $"⚠️ {resolution.Description}";
            }

            var reasonLabel = FormatFailureReason(resolution.FailureReason);
            return $"❌ {reasonLabel} — {resolution.Description}";
        }

        public static string FormatFailureReason(ActionFailureReason failureReason)
        {
            return failureReason switch
            {
                ActionFailureReason.OutOfRange => "Hors de portée",
                ActionFailureReason.LineOfSightBlocked => "Ligne de vue bloquée",
                ActionFailureReason.SkillResourceInsufficient => "Ressource insuffisante",
                ActionFailureReason.SkillOnCooldown => "Compétence en cooldown",
                ActionFailureReason.MovementPointsInsufficient => "PM insuffisants",
                ActionFailureReason.DestinationOccupied => "Case occupée",
                ActionFailureReason.TurnTimedOut => "Temps du tour écoulé",
                ActionFailureReason.InvalidOrigin => "Acteur invalide",
                _ => failureReason.ToString(),
            };
        }

        public static string FormatEventRecordLine(CombatEventRecord record)
        {
            var summary = FormatResolutionSummary(new ActionResolution(
                success: record.Code == ActionResolutionCode.Succeeded,
                code: record.Code,
                description: record.Description,
                failureReason: record.FailureReason,
                payload: record.Payload));

            return $"[T{record.Tick}] {summary}";
        }

        public static string BuildSkillTooltip(SkillActionPolicy policy, int currentCooldown, int currentResource)
        {
            var stateLabel = currentCooldown > 0
                ? $"Cooldown: {currentCooldown} tour(s)"
                : currentResource < policy.ResourceCost
                    ? $"Ressource: {currentResource}/{policy.ResourceCost}"
                    : "Prête";

            return $"Skill {policy.SkillId} | Portée {policy.MinRange}-{policy.MaxRange} | Coût {policy.ResourceCost} | Dmg {policy.BaseDamage} | Hit {policy.BaseHitChance}% | {stateLabel}";
        }
    }
}
