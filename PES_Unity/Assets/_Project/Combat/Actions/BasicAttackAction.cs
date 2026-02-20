// Utilité : ce script implémente une première version d'attaque de base avec validations
// de portée, ligne de vue simplifiée, bonus de hauteur et dégâts déterministes via RNG.
using PES.Combat.Resolution;
using PES.Combat.Targeting;
using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    public readonly struct BasicAttackAction : IActionCommand
    {
        private const int MinRange = 1;
        private const int MaxRange = 2;
        private const int MaxLineOfSightDelta = 2;

        private static readonly BasicAttackResolutionPolicy DefaultPolicy = new(baseDamage: 12, baseHitChance: 80);

        public BasicAttackAction(EntityId attackerId, EntityId targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        public EntityId AttackerId { get; }
        public EntityId TargetId { get; }

        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            var targetingService = new BasicAttackTargetingService();
            var targeting = targetingService.Evaluate(state, AttackerId, TargetId, MinRange, MaxRange, MaxLineOfSightDelta);
            if (!targeting.Success)
            {
                return targeting.Failure switch
                {
                    BasicAttackTargetingFailure.MissingPositions =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: missing positions ({AttackerId} -> {TargetId})", ActionFailureReason.MissingPositions),
                    BasicAttackTargetingFailure.TooClose =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: target too close ({AttackerId} -> {TargetId}, range:{targeting.HorizontalDistance})", ActionFailureReason.TooClose),
                    BasicAttackTargetingFailure.OutOfRange =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: out of range ({AttackerId} -> {TargetId}, range:{targeting.HorizontalDistance})", ActionFailureReason.OutOfRange),
                    BasicAttackTargetingFailure.LineOfSightBlocked =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: line of sight blocked ({AttackerId} -> {TargetId}, z:{targeting.VerticalDelta})", ActionFailureReason.LineOfSightBlocked),
                    _ =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: invalid targeting ({AttackerId} -> {TargetId})", ActionFailureReason.InvalidTargeting),
                };
            }

            if (!state.TryGetEntityHitPoints(TargetId, out _))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: missing hit points for {TargetId}", ActionFailureReason.MissingHitPoints);
            }

            var resolutionService = new BasicAttackResolutionService();
            var resolution = resolutionService.Resolve(rngService, -targeting.VerticalDelta, DefaultPolicy);
            if (!resolution.Hit)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Missed,
                    $"BasicAttackMissed: {AttackerId} -> {TargetId} [roll:{resolution.Roll}, hitChance:{resolution.HitChance}]",
                    ActionFailureReason.HitRollMissed);
            }

            var damageApplied = state.TryApplyDamage(TargetId, resolution.FinalDamage);
            if (!damageApplied)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: failed to apply damage to {TargetId}", ActionFailureReason.DamageApplicationFailed);
            }

            return new ActionResolution(
                true,
                ActionResolutionCode.Succeeded,
                $"BasicAttackResolved: {AttackerId} -> {TargetId} [roll:{resolution.Roll}, hitChance:{resolution.HitChance}, dmg:{resolution.FinalDamage}, hBonus:{resolution.HeightDamageBonus}]");
        }
    }
}
