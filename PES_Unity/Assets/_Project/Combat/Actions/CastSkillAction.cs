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
                return RejectSkill(policy.SkillId, ActionFailureReason.InvalidPolicy, $"CastSkillRejected: invalid skill policy for {CasterId}");
            }

            if (state.TryGetEntityHitPoints(CasterId, out var casterHp) && casterHp <= 0)
            {
                return RejectSkill(policy.SkillId, ActionFailureReason.ActorDefeated, $"CastSkillRejected: caster defeated ({CasterId})");
            }

            if (!state.TryGetEntityHitPoints(TargetId, out var targetHp))
            {
                return RejectSkill(policy.SkillId, ActionFailureReason.MissingHitPoints, $"CastSkillRejected: missing hit points for {TargetId}");
            }

            if (targetHp <= 0)
            {
                return RejectSkill(policy.SkillId, ActionFailureReason.TargetDefeated, $"CastSkillRejected: target already defeated ({TargetId})");
            }

            var remainingCooldown = state.GetSkillCooldown(CasterId, policy.SkillId);
            if (remainingCooldown > 0)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Rejected,
                    $"CastSkillRejected: skill on cooldown ({CasterId}, skill:{policy.SkillId}, remaining:{remainingCooldown})",
                    ActionFailureReason.SkillOnCooldown,
                    new ActionResultPayload("SkillRejected", policy.SkillId, (int)ActionFailureReason.SkillOnCooldown, remainingCooldown));
            }

            var availableResource = state.TryGetEntitySkillResource(CasterId, out var skillResource) ? skillResource : 0;
            if (availableResource < policy.ResourceCost)
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Rejected,
                    $"CastSkillRejected: insufficient skill resource ({CasterId}, skill:{policy.SkillId}, available:{availableResource}, required:{policy.ResourceCost})",
                    ActionFailureReason.SkillResourceInsufficient,
                    new ActionResultPayload("SkillRejected", policy.SkillId, (int)ActionFailureReason.SkillResourceInsufficient, availableResource));
            }

            var targetingService = new SkillTargetingService();
            var targeting = targetingService.Evaluate(state, CasterId, TargetId, policy);
            if (!targeting.Success)
            {
                return targeting.Failure switch
                {
                    SkillTargetingFailure.MissingPositions =>
                        RejectSkill(policy.SkillId, ActionFailureReason.MissingPositions, $"CastSkillRejected: missing positions ({CasterId} -> {TargetId})", targeting.DistanceXZ),
                    SkillTargetingFailure.TooClose =>
                        RejectSkill(policy.SkillId, ActionFailureReason.TooClose, $"CastSkillRejected: target too close ({CasterId} -> {TargetId}, distXZ:{targeting.DistanceXZ})", targeting.DistanceXZ),
                    SkillTargetingFailure.OutOfRange =>
                        RejectSkill(policy.SkillId, ActionFailureReason.OutOfRange, $"CastSkillRejected: out of range ({CasterId} -> {TargetId}, distXZ:{targeting.DistanceXZ}, max:{targeting.EffectiveMaxRange})", targeting.DistanceXZ),
                    SkillTargetingFailure.LineOfSightBlocked =>
                        RejectSkill(policy.SkillId, ActionFailureReason.LineOfSightBlocked, $"CastSkillRejected: line of sight blocked ({CasterId} -> {TargetId})", targeting.DistanceXZ),
                    SkillTargetingFailure.SelfTargeting =>
                        RejectSkill(policy.SkillId, ActionFailureReason.SelfTargeting, $"CastSkillRejected: self-targeting ({CasterId})", targeting.DistanceXZ),
                    SkillTargetingFailure.InvalidPolicy =>
                        RejectSkill(policy.SkillId, ActionFailureReason.InvalidPolicy, $"CastSkillRejected: invalid policy ({CasterId} -> {TargetId})", targeting.DistanceXZ),
                    _ =>
                        RejectSkill(policy.SkillId, ActionFailureReason.InvalidTargeting, $"CastSkillRejected: invalid targeting ({CasterId} -> {TargetId})", targeting.DistanceXZ),
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

            var finalDamage = StatusEffectDamageModifier.Apply(state, CasterId, TargetId, damageResolution.FinalDamage);
            if (!state.TryApplyDamage(TargetId, finalDamage))
            {
                return RejectSkill(policy.SkillId, ActionFailureReason.DamageApplicationFailed, $"CastSkillRejected: failed to apply damage to {TargetId}");
            }

            var splashTargetsHit = ApplySplashDamage(state, policy, finalDamage);

            if (!state.TryConsumeEntitySkillResource(CasterId, policy.ResourceCost))
            {
                return new ActionResolution(
                    false,
                    ActionResolutionCode.Rejected,
                    $"CastSkillRejected: failed to consume skill resource ({CasterId}, skill:{policy.SkillId}, required:{policy.ResourceCost})",
                    ActionFailureReason.SkillResourceInsufficient,
                    new ActionResultPayload("SkillRejected", policy.SkillId, (int)ActionFailureReason.SkillResourceInsufficient, availableResource));
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
                $"CastSkillResolved: {CasterId} -> {TargetId} [skill:{policy.SkillId}, roll:{resolution.Roll}, hitChance:{resolution.HitChance}, critRoll:{criticalRoll}, critChance:{criticalChance}, crit:{isCritical}, dmg:{finalDamage}, splashHits:{splashTargetsHit}, distXZ:{targeting.DistanceXZ}, max:{targeting.EffectiveMaxRange}]",
                ActionFailureReason.None,
                new ActionResultPayload("SkillResolved", policy.SkillId, finalDamage, splashTargetsHit));
        }

        private static ActionResolution RejectSkill(int skillId, ActionFailureReason reason, string description, int contextValue = 0)
        {
            return new ActionResolution(
                false,
                ActionResolutionCode.Rejected,
                description,
                reason,
                new ActionResultPayload("SkillRejected", skillId, (int)reason, contextValue));
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
