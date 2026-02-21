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
        public void Resolve_CastSkillAction_WithTargetDebuffEffect_AppliesConfiguredStatusEffect()
        {
            var state = new BattleState();
            var caster = new EntityId(350);
            var target = new EntityId(351);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 10);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 270,
                minRange: 1,
                maxRange: 3,
                baseDamage: 5,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                targetStatusEffectType: StatusEffectType.Weakened,
                targetStatusPotency: 3,
                targetStatusDurationTurns: 2,
                targetStatusTickMoment: StatusEffectTickMoment.TurnEnd);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Weakened), Is.EqualTo(2));
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Poison), Is.EqualTo(0));

            var endTickDamage = state.TickStatusEffects(target, StatusEffectTickMoment.TurnEnd);
            Assert.That(endTickDamage, Is.EqualTo(0));
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Weakened), Is.EqualTo(1));
        }

        [Test]
        public void Resolve_CastSkillAction_WithCasterBuffAndPoison_AppliesBothEffects()
        {
            var state = new BattleState();
            var caster = new EntityId(360);
            var target = new EntityId(361);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(caster, 10);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 271,
                minRange: 1,
                maxRange: 3,
                baseDamage: 4,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                periodicDamage: 2,
                periodicDurationTurns: 3,
                periodicTickMoment: StatusEffectTickMoment.TurnStart,
                casterStatusEffectType: StatusEffectType.Fortified,
                casterStatusPotency: 1,
                casterStatusDurationTurns: 2,
                casterStatusTickMoment: StatusEffectTickMoment.TurnEnd);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.True);
            Assert.That(state.GetStatusEffectRemaining(target, StatusEffectType.Poison), Is.EqualTo(3));
            Assert.That(state.GetStatusEffectRemaining(caster, StatusEffectType.Fortified), Is.EqualTo(2));

            var poisonDamage = state.TickStatusEffects(target, StatusEffectTickMoment.TurnStart);
            Assert.That(poisonDamage, Is.EqualTo(2));
        }


        [Test]
        public void Resolve_CastSkillAction_WithStatusTypeButZeroDuration_IsRejectedAsInvalidPolicy()
        {
            var state = new BattleState();
            var caster = new EntityId(370);
            var target = new EntityId(371);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 272,
                minRange: 1,
                maxRange: 3,
                baseDamage: 4,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                targetStatusEffectType: StatusEffectType.Marked,
                targetStatusDurationTurns: 0,
                targetStatusPotency: 1);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.InvalidPolicy));
        }


        [Test]
        public void Resolve_CastSkillAction_RejectionPayload_UsesStableSkillRejectedContract()
        {
            var state = new BattleState();
            var caster = new EntityId(380);
            var target = new EntityId(381);

            state.SetEntityPosition(caster, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(8, 0, 0));
            state.SetEntityHitPoints(caster, 20);
            state.SetEntityHitPoints(target, 20);

            var resolver = new ActionResolver(new SeededRngService(42));
            var policy = new SkillActionPolicy(
                skillId: 333,
                minRange: 1,
                maxRange: 3,
                baseDamage: 4,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1);

            var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.OutOfRange));
            Assert.That(result.Payload.HasValue, Is.True);
            Assert.That(result.Payload!.Value.Kind, Is.EqualTo("SkillRejected"));
            Assert.That(result.Payload!.Value.Value1, Is.EqualTo(333));
            Assert.That(result.Payload!.Value.Value2, Is.EqualTo((int)ActionFailureReason.OutOfRange));
            Assert.That(result.Payload!.Value.SchemaVersion, Is.EqualTo(ActionResultPayload.CurrentSchemaVersion));
        }

        [Test]
        public void Resolve_CastSkillAction_DifferentSeeds_ProduceControlledDamageDivergence()
        {
            var caster = new EntityId(390);
            var targetA = new EntityId(391);
            var targetB = new EntityId(392);

            var policy = new SkillActionPolicy(
                skillId: 334,
                minRange: 1,
                maxRange: 3,
                baseDamage: 9,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1);

            var stateA = new BattleState();
            stateA.SetEntityPosition(caster, new Position3(0, 0, 0));
            stateA.SetEntityPosition(targetA, new Position3(1, 0, 0));
            stateA.SetEntityHitPoints(caster, 40);
            stateA.SetEntityHitPoints(targetA, 40);

            var stateB = new BattleState();
            stateB.SetEntityPosition(caster, new Position3(0, 0, 0));
            stateB.SetEntityPosition(targetB, new Position3(1, 0, 0));
            stateB.SetEntityHitPoints(caster, 40);
            stateB.SetEntityHitPoints(targetB, 40);

            var resultA = new ActionResolver(new SeededRngService(5)).Resolve(stateA, new CastSkillAction(caster, targetA, policy));
            var resultB = new ActionResolver(new SeededRngService(17)).Resolve(stateB, new CastSkillAction(caster, targetB, policy));

            Assert.That(resultA.Success, Is.True);
            Assert.That(resultB.Success, Is.True);
            Assert.That(resultA.Payload.HasValue, Is.True);
            Assert.That(resultB.Payload.HasValue, Is.True);
            Assert.That(resultA.Payload!.Value.Kind, Is.EqualTo("SkillResolved"));
            Assert.That(resultB.Payload!.Value.Kind, Is.EqualTo("SkillResolved"));
            Assert.That(resultA.Payload!.Value.Value2, Is.Not.EqualTo(resultB.Payload!.Value.Value2));
        }


        [Test]
        public void Resolve_CastSkillAction_WithFortifiedTarget_ReducesResolvedDamage()
        {
            var caster = new EntityId(395);
            var targetA = new EntityId(396);
            var targetB = new EntityId(397);

            var policy = new SkillActionPolicy(
                skillId: 335,
                minRange: 1,
                maxRange: 3,
                baseDamage: 10,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1);

            var baselineState = new BattleState();
            baselineState.SetEntityPosition(caster, new Position3(0, 0, 0));
            baselineState.SetEntityPosition(targetA, new Position3(1, 0, 0));
            baselineState.SetEntityHitPoints(caster, 40);
            baselineState.SetEntityHitPoints(targetA, 40);

            var fortifiedState = new BattleState();
            fortifiedState.SetEntityPosition(caster, new Position3(0, 0, 0));
            fortifiedState.SetEntityPosition(targetB, new Position3(1, 0, 0));
            fortifiedState.SetEntityHitPoints(caster, 40);
            fortifiedState.SetEntityHitPoints(targetB, 40);
            fortifiedState.SetStatusEffect(targetB, StatusEffectType.Fortified, remainingTurns: 2, potency: 3);

            var baselineResult = new ActionResolver(new SeededRngService(21)).Resolve(baselineState, new CastSkillAction(caster, targetA, policy));
            var fortifiedResult = new ActionResolver(new SeededRngService(21)).Resolve(fortifiedState, new CastSkillAction(caster, targetB, policy));

            Assert.That(baselineResult.Success, Is.True);
            Assert.That(fortifiedResult.Success, Is.True);
            Assert.That(baselineResult.Payload.HasValue, Is.True);
            Assert.That(fortifiedResult.Payload.HasValue, Is.True);
            Assert.That(fortifiedResult.Payload!.Value.Value2, Is.LessThan(baselineResult.Payload!.Value.Value2));
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
