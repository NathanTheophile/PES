// Utilité : ce script implémente une première action de skill ciblée
// (validation, ciblage, résolution RNG et application des dégâts).
using System;
using PES.Combat.Resolution;
using PES.Combat.Targeting;
using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    public readonly struct CastSkillAction : IActionCommand
    {
        private static readonly SkillActionPolicy DefaultPolicy = new(
            skillId: 0,
            minRange: 1,
            maxRange: 3,
            baseDamage: 8,
            baseHitChance: 85,
            elevationPerRangeBonus: 2,
            rangeBonusPerElevationStep: 1,
            damageElement: DamageElement.Elemental,
            baseCriticalChance: 5);

        private readonly SkillActionPolicy? _policyOverride;

        public CastSkillAction(EntityId casterId, EntityId targetId)
            : this(casterId, targetId, null)
        {
        }

        public CastSkillAction(EntityId casterId, EntityId targetId, SkillActionPolicy? policyOverride)
        {
            CasterId = casterId;
            TargetId = targetId;
            _policyOverride = policyOverride;
        }

        public EntityId CasterId { get; }
        public EntityId TargetId { get; }

        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            var policy = _policyOverride ?? DefaultPolicy;
            if (!policy.IsValid)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: invalid skill policy for {CasterId}", ActionFailureReason.InvalidPolicy);
            }

            if (state.TryGetEntityHitPoints(CasterId, out var casterHp) && casterHp <= 0)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: caster defeated ({CasterId})", ActionFailureReason.ActorDefeated);
            }

            if (!state.TryGetEntityHitPoints(TargetId, out var targetHp))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: missing hit points for {TargetId}", ActionFailureReason.MissingHitPoints);
            }

            if (targetHp <= 0)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: target already defeated ({TargetId})", ActionFailureReason.TargetDefeated);
            }

            var remainingCooldown = state.GetSkillCooldown(CasterId, policy.SkillId);
            if (remainingCooldown > 0)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Rejected,
                    $"CastSkillRejected: skill on cooldown ({CasterId}, skill:{policy.SkillId}, remaining:{remainingCooldown})",
                    ActionFailureReason.SkillOnCooldown,
                    new ActionResultPayload("SkillOnCooldown", policy.SkillId, remainingCooldown, 0));
            }

            var availableResource = state.TryGetEntitySkillResource(CasterId, out var skillResource) ? skillResource : 0;
            if (availableResource < policy.ResourceCost)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Rejected,
                    $"CastSkillRejected: insufficient skill resource ({CasterId}, skill:{policy.SkillId}, available:{availableResource}, required:{policy.ResourceCost})",
                    ActionFailureReason.SkillResourceInsufficient,
                    new ActionResultPayload("SkillResourceInsufficient", policy.SkillId, availableResource, policy.ResourceCost));
            }

            var targetingService = new SkillTargetingService();
            var targeting = targetingService.Evaluate(state, CasterId, TargetId, policy);
            if (!targeting.Success)
            {
                return targeting.Failure switch
                {
                    SkillTargetingFailure.MissingPositions =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: missing positions ({CasterId} -> {TargetId})", ActionFailureReason.MissingPositions),
                    SkillTargetingFailure.TooClose =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: target too close ({CasterId} -> {TargetId}, distXZ:{targeting.DistanceXZ})", ActionFailureReason.TooClose),
                    SkillTargetingFailure.OutOfRange =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: out of range ({CasterId} -> {TargetId}, distXZ:{targeting.DistanceXZ}, max:{targeting.EffectiveMaxRange})", ActionFailureReason.OutOfRange),
                    SkillTargetingFailure.LineOfSightBlocked =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: line of sight blocked ({CasterId} -> {TargetId})", ActionFailureReason.LineOfSightBlocked),
                    SkillTargetingFailure.SelfTargeting =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: self-targeting ({CasterId})", ActionFailureReason.SelfTargeting),
                    SkillTargetingFailure.InvalidPolicy =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: invalid policy ({CasterId} -> {TargetId})", ActionFailureReason.InvalidPolicy),
                    _ =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: invalid targeting ({CasterId} -> {TargetId})", ActionFailureReason.InvalidTargeting),
                };
            }

            var resolutionService = new SkillResolutionService();
            var resolution = resolutionService.Resolve(rngService, policy.BaseDamage, policy.BaseHitChance);
            if (!resolution.Hit)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Missed,
                    $"CastSkillMissed: {CasterId} -> {TargetId} [skill:{policy.SkillId}, roll:{resolution.Roll}, hitChance:{resolution.HitChance}]",
                    ActionFailureReason.HitRollMissed,
                    new ActionResultPayload("SkillMissed", policy.SkillId, resolution.Roll, resolution.HitChance));
            }

            var attackerStats = state.GetEntityRpgStatsOrEmpty(CasterId);
            var defenderStats = state.GetEntityRpgStatsOrEmpty(TargetId);
            var criticalChance = Clamp(policy.BaseCriticalChance + attackerStats.CriticalChance, 0, 100);
            var criticalRoll = rngService.NextInt(1, 101);
            var isCritical = criticalRoll <= criticalChance;

            var damageResolution = DamageFormulaCalculator.Resolve(
                attackerStats,
                defenderStats,
                policy.BaseDamage,
                policy.BaseCriticalChance,
                policy.DamageElement,
                isCritical);

            if (!state.TryApplyDamage(TargetId, damageResolution.FinalDamage))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: failed to apply damage to {TargetId}", ActionFailureReason.DamageApplicationFailed);
            }

            var splashTargetsHit = ApplySplashDamage(state, policy, damageResolution.FinalDamage);

            if (!state.TryConsumeEntitySkillResource(CasterId, policy.ResourceCost))
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Rejected,
                    $"CastSkillRejected: failed to consume skill resource ({CasterId}, skill:{policy.SkillId}, required:{policy.ResourceCost})",
                    ActionFailureReason.SkillResourceInsufficient,
                    new ActionResultPayload("SkillResourceInsufficient", policy.SkillId, availableResource, policy.ResourceCost));
            }

            state.SetSkillCooldown(CasterId, policy.SkillId, policy.CooldownTurns);

            if (policy.PeriodicDamage > 0 && policy.PeriodicDurationTurns > 0)
            {
                state.SetStatusEffect(TargetId, StatusEffectType.Poison, policy.PeriodicDurationTurns, policy.PeriodicDamage, policy.PeriodicTickMoment);
            }

            ApplyStatusEffectFromPolicy(
                state,
                TargetId,
                policy.TargetStatusEffectType,
                policy.TargetStatusDurationTurns,
                policy.TargetStatusPotency,
                policy.TargetStatusTickMoment);

            ApplyStatusEffectFromPolicy(
                state,
                CasterId,
                policy.CasterStatusEffectType,
                policy.CasterStatusDurationTurns,
                policy.CasterStatusPotency,
                policy.CasterStatusTickMoment);

            return new ActionResolution(
                true,
                ActionResolutionCode.Succeeded,
                $"CastSkillResolved: {CasterId} -> {TargetId} [skill:{policy.SkillId}, roll:{resolution.Roll}, hitChance:{resolution.HitChance}, critRoll:{criticalRoll}, critChance:{criticalChance}, crit:{isCritical}, dmg:{damageResolution.FinalDamage}, splashHits:{splashTargetsHit}, distXZ:{targeting.DistanceXZ}, max:{targeting.EffectiveMaxRange}]",
                ActionFailureReason.None,
                new ActionResultPayload("SkillResolved", policy.SkillId, damageResolution.FinalDamage, splashTargetsHit));
        }

        private static void ApplyStatusEffectFromPolicy(
            BattleState state,
            EntityId target,
            StatusEffectType effectType,
            int durationTurns,
            int potency,
            StatusEffectTickMoment tickMoment)
        {
            if (effectType == StatusEffectType.None || durationTurns <= 0)
            {
                return;
            }

            state.SetStatusEffect(target, effectType, durationTurns, potency, tickMoment);
        }

        private int ApplySplashDamage(BattleState state, SkillActionPolicy policy, int primaryDamage)
        {
            if (policy.SplashRadiusXZ <= 0 || policy.SplashDamagePercent <= 0)
            {
                return 0;
            }

            var splashDamage = (primaryDamage * policy.SplashDamagePercent) / 100;
            if (splashDamage <= 0)
            {
                return 0;
            }

            if (!state.TryGetEntityPosition(TargetId, out var primaryTargetPosition))
            {
                return 0;
            }

            var hits = 0;
            foreach (var pair in state.GetEntityPositions())
            {
                if (pair.Key.Equals(CasterId) || pair.Key.Equals(TargetId))
                {
                    continue;
                }

                if (!state.TryGetEntityHitPoints(pair.Key, out var hp) || hp <= 0)
                {
                    continue;
                }

                var distanceXZ = Math.Abs(pair.Value.X - primaryTargetPosition.X) + Math.Abs(pair.Value.Z - primaryTargetPosition.Z);
                if (distanceXZ > policy.SplashRadiusXZ)
                {
                    continue;
                }

                if (state.TryApplyDamage(pair.Key, splashDamage))
                {
                    hits++;
                }
            }

            return hits;
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
