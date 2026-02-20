// Utilité : ce script implémente une première action de skill ciblée
// (validation, ciblage, résolution RNG et application des dégâts).
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
            rangeBonusPerElevationStep: 1);

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

            if (!state.TryApplyDamage(TargetId, resolution.FinalDamage))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"CastSkillRejected: failed to apply damage to {TargetId}", ActionFailureReason.DamageApplicationFailed);
            }

            return new ActionResolution(
                true,
                ActionResolutionCode.Succeeded,
                $"CastSkillResolved: {CasterId} -> {TargetId} [skill:{policy.SkillId}, roll:{resolution.Roll}, hitChance:{resolution.HitChance}, dmg:{resolution.FinalDamage}, distXZ:{targeting.DistanceXZ}, max:{targeting.EffectiveMaxRange}]",
                ActionFailureReason.None,
                new ActionResultPayload("SkillResolved", policy.SkillId, resolution.FinalDamage, targeting.DistanceXZ));
        }
    }
}
