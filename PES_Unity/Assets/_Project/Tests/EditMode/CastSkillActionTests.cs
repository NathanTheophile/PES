using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Infrastructure.Replay;

namespace PES.Tests.EditMode
{
    public class CastSkillActionTests
    {
        [Test]
        public void Resolve_CastSkillAction_Success_ProducesStructuredPayloadAndDamage()
        {
            var state = new BattleState();
            var caster = new EntityId(300);
            var target = new EntityId(301);

            state.SetEntityPosition(caster, new Position3(0, 1, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 30);

            var resolver = new ActionResolver(new SeededRngService(7));
            var policy = new SkillActionPolicy(skillId: 12, minRange: 1, maxRange: 3, baseDamage: 6, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
            Assert.That(result.Payload.HasValue, Is.True);
            Assert.That(result.Payload!.Value.Kind, Is.EqualTo("SkillResolved"));
            Assert.That(result.Payload!.Value.Value1, Is.EqualTo(12));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));

            Assert.That(state.TryGetEntityHitPoints(target, out var targetHp), Is.True);
            Assert.That(targetHp, Is.LessThan(30));
        }

        [Test]
        public void Resolve_CastSkillAction_WithOutOfRangeTarget_IsRejectedWithFailureReason()
        {
            var state = new BattleState();
            var caster = new EntityId(302);
            var target = new EntityId(303);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(6, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 30);

            var resolver = new ActionResolver(new SeededRngService(7));
            var policy = new SkillActionPolicy(skillId: 13, minRange: 1, maxRange: 3, baseDamage: 6, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.OutOfRange));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog[0].FailureReason, Is.EqualTo(ActionFailureReason.OutOfRange));
        }

        [Test]
        public void Resolve_CastSkillAction_WithBlockedRaycast_IsRejectedWithLineOfSightReason()
        {
            var state = new BattleState();
            var caster = new EntityId(306);
            var target = new EntityId(307);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(4, 0, 0));
            state.SetEntityHitPoints(caster, 25);
            state.SetEntityHitPoints(target, 25);
            state.SetEntitySkillResource(caster, 10);

            // Obstacle haut au milieu de la ligne x/z => LOS bloquée.
            state.SetBlockedPosition(new Position3(2, 2, 0), blocked: true);

            var resolver = new ActionResolver(new SeededRngService(17));
            var policy = new SkillActionPolicy(skillId: 77, minRange: 1, maxRange: 5, baseDamage: 5, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1);
            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.LineOfSightBlocked));
        }

        [Test]
        public void Resolve_CastSkillAction_ElevationBonus_ExtendsRange()
        {
            var state = new BattleState();
            var caster = new EntityId(308);
            var target = new EntityId(309);

            // Distance x/z = 5, hors maxRange=3, mais caster elevation Y=4 => +2 portée si tranche=2.
            state.SetEntityPosition(caster, new Position3(0, 4, 0));
            state.SetEntityPosition(target, new Position3(5, 0, 0));
            state.SetEntityHitPoints(caster, 25);
            state.SetEntityHitPoints(target, 25);

            var resolver = new ActionResolver(new SeededRngService(17));
            var policy = new SkillActionPolicy(skillId: 78, minRange: 1, maxRange: 3, baseDamage: 5, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1);
            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
        }

        [Test]
        public void Resolve_CastSkillAction_WithZeroCostAndNoResourcePool_Succeeds()
        {
            var state = new BattleState();
            var caster = new EntityId(316);
            var target = new EntityId(317);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(skillId: 203, minRange: 1, maxRange: 3, baseDamage: 7, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1, resourceCost: 0, cooldownTurns: 0);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
        }

        [Test]
        public void Resolve_CastSkillAction_WithInsufficientResource_IsRejected()
        {
            var state = new BattleState();
            var caster = new EntityId(310);
            var target = new EntityId(311);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 1);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(skillId: 200, minRange: 1, maxRange: 3, baseDamage: 7, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1, resourceCost: 3);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.SkillResourceInsufficient));
        }

        [Test]
        public void Resolve_CastSkillAction_OnCooldown_IsRejected()
        {
            var state = new BattleState();
            var caster = new EntityId(312);
            var target = new EntityId(313);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 10);
            state.SetSkillCooldown(caster, skillId: 201, remainingTurns: 2);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(skillId: 201, minRange: 1, maxRange: 3, baseDamage: 7, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1, resourceCost: 2);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.SkillOnCooldown));
        }

        [Test]
        public void Resolve_CastSkillAction_Success_ConsumesResourceAndSetsCooldown()
        {
            var state = new BattleState();
            var caster = new EntityId(314);
            var target = new EntityId(315);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 8);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(skillId: 202, minRange: 1, maxRange: 3, baseDamage: 7, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1, resourceCost: 3, cooldownTurns: 2);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.TryGetEntitySkillResource(caster, out var remaining), Is.True);
            Assert.That(remaining, Is.EqualTo(5));
            Assert.That(state.GetSkillCooldown(caster, 202), Is.EqualTo(2));
        }

        [Test]
        public void Resolve_CastSkillAction_WithSplashDamage_HitsSecondaryTargetsInRadius()
        {
            var state = new BattleState();
            var caster = new EntityId(330);
            var primaryTarget = new EntityId(331);
            var secondaryTarget = new EntityId(332);
            var outOfSplashTarget = new EntityId(333);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(primaryTarget, new Position3(1, 0, 0));
            state.SetEntityPosition(secondaryTarget, new Position3(2, 0, 0));
            state.SetEntityPosition(outOfSplashTarget, new Position3(4, 0, 0));

            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(primaryTarget, 30);
            state.SetEntityHitPoints(secondaryTarget, 30);
            state.SetEntityHitPoints(outOfSplashTarget, 30);
            state.SetEntitySkillResource(caster, 10);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 250,
                minRange: 1,
                maxRange: 4,
                baseDamage: 10,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                splashRadiusXZ: 1,
                splashDamagePercent: 50);

            var result = resolver.Resolve(state, new CastSkillAction(caster, primaryTarget, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.TryGetEntityHitPoints(primaryTarget, out var primaryHp), Is.True);
            Assert.That(state.TryGetEntityHitPoints(secondaryTarget, out var secondaryHp), Is.True);
            Assert.That(state.TryGetEntityHitPoints(outOfSplashTarget, out var farHp), Is.True);

            Assert.That(primaryHp, Is.LessThan(30));
            Assert.That(secondaryHp, Is.LessThan(30));
            Assert.That(farHp, Is.EqualTo(30));
            Assert.That(result.Payload.HasValue, Is.True);
            Assert.That(result.Payload.Value.Value3, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_CastSkillAction_WithPeriodicDamagePolicy_AppliesPoisonWithConfiguredTickMoment()
        {
            var state = new BattleState();
            var caster = new EntityId(340);
            var target = new EntityId(341);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 10);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 260,
                minRange: 1,
                maxRange: 3,
                baseDamage: 5,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                periodicDamage: 2,
                periodicDurationTurns: 3,
                periodicTickMoment: StatusEffectTickMoment.TurnEnd);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Poison), Is.EqualTo(3));

            var startTick = state.TickStatusEffects(target, StatusEffectTickMoment.TurnStart);
            Assert.That(startTick, Is.EqualTo(0));

            var endTick = state.TickStatusEffects(target, StatusEffectTickMoment.TurnEnd);
            Assert.That(endTick, Is.EqualTo(2));
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Poison), Is.EqualTo(2));
        }

        [Test]
        public void Resolve_CastSkillAction_WithVulnerableDebuff_AppliesStatusOnTarget()
        {
            var state = new BattleState();
            var caster = new EntityId(342);
            var target = new EntityId(343);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 10);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 261,
                minRange: 1,
                maxRange: 3,
                baseDamage: 5,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                vulnerableDurationTurns: 2);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Vulnerable), Is.EqualTo(2));

            state.SetEntityHitPoints(target, 20);
            state.TryApplyDamage(target, 10);
            Assert.That(state.TryGetEntityHitPoints(target, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(10));
        }

        [Test]
        public void Resolve_CastSkillAction_WithInvulnerableDebuff_BlocksIncomingDamage()
        {
            var state = new BattleState();
            var caster = new EntityId(350);
            var target = new EntityId(351);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 262,
                minRange: 1,
                maxRange: 3,
                baseDamage: 5,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                invulnerableDurationTurns: 2);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Invulnerable), Is.EqualTo(2));

            state.SetEntityHitPoints(target, 20);
            var applied = state.TryApplyDamage(target, 10);
            Assert.That(applied, Is.False);
            Assert.That(state.TryGetEntityHitPoints(target, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(20));
        }

        [Test]
        public void Replay_WithCastSkillAction_ReproducesFinalSnapshotWithSameSeed()
        {
            var caster = new EntityId(304);
            var target = new EntityId(305);

            var state = new BattleState();
            state.SetEntityPosition(caster, new Position3(0, 1, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 25);
            state.SetEntityHitPoints(target, 25);

            var recorder = new BattleReplayRecorder(seed: 17);
            recorder.CaptureInitialState(state);

            var resolver = new ActionResolver(new SeededRngService(17));
            var policy = new SkillActionPolicy(skillId: 99, minRange: 1, maxRange: 3, baseDamage: 5, baseHitChance: 100, elevationPerRangeBonus: 2, rangeBonusPerElevationStep: 1, resourceCost: 2, cooldownTurns: 1);
            resolver.Resolve(state, new CastSkillAction(caster, target, policy));
            recorder.RecordAction(RecordedActionCommand.CastSkill(caster, target, policy), state);

            var original = recorder.Build();
            var replay = new BattleReplayRunner().Run(original);

            var originalFinal = original.Snapshots[original.Snapshots.Count - 1];
            Assert.That(originalFinal.Tick, Is.EqualTo(replay.FinalSnapshot.Tick));
            Assert.That(originalFinal.EntityHitPoints.Count, Is.EqualTo(replay.FinalSnapshot.EntityHitPoints.Count));

            int originalTargetHp = -1;
            foreach (var row in originalFinal.EntityHitPoints)
            {
                if (row.EntityId.Value == target.Value)
                {
                    originalTargetHp = row.HitPoints;
                }
            }

            int replayTargetHp = -1;
            foreach (var row in replay.FinalSnapshot.EntityHitPoints)
            {
                if (row.EntityId.Value == target.Value)
                {
                    replayTargetHp = row.HitPoints;
                }
            }

            Assert.That(originalTargetHp, Is.GreaterThanOrEqualTo(0));
            Assert.That(replayTargetHp, Is.EqualTo(originalTargetHp));
        }
    }
}
