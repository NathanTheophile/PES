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
        private static readonly BasicAttackActionPolicy DefaultPolicy = new(
            minRange: 1,
            maxRange: 2,
            maxLineOfSightDelta: 2,
            resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 12, baseHitChance: 80),
            damageElement: DamageElement.Physical,
            baseCriticalChance: 5);

        private readonly BasicAttackActionPolicy? _policyOverride;

        public BasicAttackAction(EntityId attackerId, EntityId targetId)
            : this(attackerId, targetId, null)
        {
        }

        public BasicAttackAction(EntityId attackerId, EntityId targetId, BasicAttackActionPolicy? policyOverride)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            _policyOverride = policyOverride;
        }

        public EntityId AttackerId { get; }
        public EntityId TargetId { get; }

        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            var policy = _policyOverride ?? DefaultPolicy;

            if (state.TryGetEntityHitPoints(AttackerId, out var attackerHp) && attackerHp <= 0)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: attacker defeated ({AttackerId})", ActionFailureReason.ActorDefeated);
            }

            if (!state.TryGetEntityHitPoints(TargetId, out var targetHp))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: missing hit points for {TargetId}", ActionFailureReason.MissingHitPoints);
            }

            if (targetHp <= 0)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: target already defeated ({TargetId})", ActionFailureReason.TargetDefeated);
            }

            var targetingService = new BasicAttackTargetingService();
            var targeting = targetingService.Evaluate(state, AttackerId, TargetId, policy.MinRange, policy.MaxRange, policy.MaxLineOfSightDelta);
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
                    BasicAttackTargetingFailure.SelfTargeting =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: self-targeting ({AttackerId})", ActionFailureReason.SelfTargeting),
                    BasicAttackTargetingFailure.InvalidPolicy =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: invalid attack policy ({AttackerId} -> {TargetId})", ActionFailureReason.InvalidPolicy),
                    _ =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: invalid targeting ({AttackerId} -> {TargetId})", ActionFailureReason.InvalidTargeting),
                };
            }

            var resolutionService = new BasicAttackResolutionService();
            var resolution = resolutionService.Resolve(rngService, -targeting.VerticalDelta, policy.ResolutionPolicy);
            if (!resolution.Hit)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Missed,
                    $"BasicAttackMissed: {AttackerId} -> {TargetId} [roll:{resolution.Roll}, hitChance:{resolution.HitChance}]",
                    ActionFailureReason.HitRollMissed,
                    new ActionResultPayload("AttackMissed", resolution.Roll, resolution.HitChance, 0));
            }

            var attackerHasRpgStats = state.TryGetEntityRpgStats(AttackerId, out var attackerStats);
            var defenderHasRpgStats = state.TryGetEntityRpgStats(TargetId, out var defenderStats);

            var finalDamage = resolution.FinalDamage;
            var criticalChance = 0;
            var criticalRoll = 0;
            var isCritical = false;

            if (attackerHasRpgStats || defenderHasRpgStats)
            {
                if (!attackerHasRpgStats)
                {
                    attackerStats = CombatantRpgStats.Empty;
                }

                if (!defenderHasRpgStats)
                {
                    defenderStats = CombatantRpgStats.Empty;
                }

                criticalChance = Clamp(policy.BaseCriticalChance + attackerStats.CriticalChance, 0, 100);
                criticalRoll = rngService.NextInt(1, 101);
                isCritical = criticalRoll <= criticalChance;

                var damageResolution = DamageFormulaCalculator.Resolve(
                    attackerStats,
                    defenderStats,
                    resolution.FinalDamage,
                    policy.BaseCriticalChance,
                    policy.DamageElement,
                    isCritical);

                finalDamage = damageResolution.FinalDamage;
            }

            finalDamage = StatusEffectDamageModifier.Apply(state, AttackerId, TargetId, finalDamage);

            var damageApplied = state.TryApplyDamage(TargetId, finalDamage);
            if (!damageApplied)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: failed to apply damage to {TargetId}", ActionFailureReason.DamageApplicationFailed);
            }

            return new ActionResolution(
                true,
                ActionResolutionCode.Succeeded,
                $"BasicAttackResolved: {AttackerId} -> {TargetId} [roll:{resolution.Roll}, hitChance:{resolution.HitChance}, hBonus:{resolution.HeightDamageBonus}, critRoll:{criticalRoll}, critChance:{criticalChance}, crit:{isCritical}, dmg:{finalDamage}]",
                ActionFailureReason.None,
                new ActionResultPayload("AttackResolved", finalDamage, criticalRoll, criticalChance));
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
