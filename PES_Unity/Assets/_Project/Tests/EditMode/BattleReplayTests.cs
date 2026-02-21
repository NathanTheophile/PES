using NUnit.Framework;
using PES.Combat.Actions;
using PES.Combat.Resolution;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Infrastructure.Replay;

namespace PES.Tests.EditMode
{
    public class BattleReplayTests
    {
        [Test]
        public void ReplayRecord_WithSameSeed_ReproducesSnapshotsAndEventLog()
        {
            // Arrange : état initial + séquence d'actions déterministe.
            var state = CreateInitialState();
            var recorder = new BattleReplayRecorder(seed: 11);
            recorder.CaptureInitialState(state);

            var resolver = new ActionResolver(new SeededRngService(11));
            var move = new MoveAction(UnitA, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1));
            var attackA = new BasicAttackAction(UnitA, UnitB);
            var attackB = new BasicAttackAction(UnitB, UnitA);

            resolver.Resolve(state, move);
            recorder.RecordAction(RecordedActionCommand.Move(UnitA, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1)), state);

            resolver.Resolve(state, attackA);
            recorder.RecordAction(RecordedActionCommand.BasicAttack(UnitA, UnitB), state);

            resolver.Resolve(state, attackB);
            recorder.RecordAction(RecordedActionCommand.BasicAttack(UnitB, UnitA), state);

            var originalRecord = recorder.Build();

            // Act : replay du record.
            var replayRunner = new BattleReplayRunner();
            var replay = replayRunner.Run(originalRecord);

            // Assert : snapshots finales identiques.
            AssertSnapshotsEqual(originalRecord.Snapshots[originalRecord.Snapshots.Count - 1], replay.FinalSnapshot);
            Assert.That(replay.Events.Count, Is.EqualTo(state.StructuredEventLog.Count));
            for (var i = 0; i < replay.Events.Count; i++)
            {
                Assert.That(replay.Events[i].Code, Is.EqualTo(state.StructuredEventLog[i].Code));
                Assert.That(replay.Events[i].Description, Is.EqualTo(state.StructuredEventLog[i].Description));
            }
        }


        [Test]
        public void ReplayRecord_WithSkillStatusesAndResources_ReproducesAllSnapshotsAndResults()
        {
            var state = new BattleState();
            state.SetEntityPosition(UnitA, new Position3(0, 0, 0));
            state.SetEntityPosition(UnitB, new Position3(1, 0, 0));
            state.SetEntityHitPoints(UnitA, 50);
            state.SetEntityHitPoints(UnitB, 50);
            state.SetEntityMovementPoints(UnitA, maxMovementPoints: 6, currentMovementPoints: 5);
            state.SetEntityMovementPoints(UnitB, maxMovementPoints: 6, currentMovementPoints: 6);
            state.SetEntitySkillResource(UnitA, 10);
            state.SetEntitySkillResource(UnitB, 5);

            state.SetEntityRpgStats(UnitA, new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 6,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 50,
                assiduity: 0,
                rapidity: 10,
                criticalChance: 0,
                criticalDamage: 0,
                criticalResistance: 0,
                attack: new DamageElementValues(10, 0, 0, 0, 0, 0),
                power: new DamageElementValues(100, 100, 100, 100, 100, 100),
                defense: default,
                resistance: default));

            state.SetEntityRpgStats(UnitB, new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 6,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 50,
                assiduity: 0,
                rapidity: 5,
                criticalChance: 0,
                criticalDamage: 0,
                criticalResistance: 0,
                attack: default,
                power: new DamageElementValues(100, 100, 100, 100, 100, 100),
                defense: new DamageElementValues(2, 0, 0, 0, 0, 0),
                resistance: default));

            var recorder = new BattleReplayRecorder(seed: 31);
            recorder.CaptureInitialState(state);

            var resolver = new ActionResolver(new SeededRngService(31));
            var originalResults = new System.Collections.Generic.List<ActionResolution>();

            var policy = new SkillActionPolicy(
                skillId: 420,
                minRange: 1,
                maxRange: 3,
                baseDamage: 6,
                baseHitChance: 100,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                resourceCost: 3,
                cooldownTurns: 2,
                periodicDamage: 2,
                periodicDurationTurns: 2,
                periodicTickMoment: StatusEffectTickMoment.TurnStart,
                targetStatusEffectType: StatusEffectType.Weakened,
                targetStatusPotency: 1,
                targetStatusDurationTurns: 2,
                targetStatusTickMoment: StatusEffectTickMoment.TurnEnd,
                casterStatusEffectType: StatusEffectType.Fortified,
                casterStatusPotency: 1,
                casterStatusDurationTurns: 1,
                casterStatusTickMoment: StatusEffectTickMoment.TurnEnd,
                damageElement: DamageElement.Blunt,
                baseCriticalChance: 0);

            var cast = resolver.Resolve(state, new CastSkillAction(UnitA, UnitB, policy));
            originalResults.Add(cast);
            recorder.RecordAction(RecordedActionCommand.CastSkill(UnitA, UnitB, policy), state);

            var attack = resolver.Resolve(state, new BasicAttackAction(UnitB, UnitA));
            originalResults.Add(attack);
            recorder.RecordAction(RecordedActionCommand.BasicAttack(UnitB, UnitA), state);

            var record = recorder.Build();
            var replay = new BattleReplayRunner().Run(record);

            Assert.That(replay.Snapshots.Count, Is.EqualTo(record.Snapshots.Count));
            for (var i = 0; i < record.Snapshots.Count; i++)
            {
                AssertSnapshotsEqual(record.Snapshots[i], replay.Snapshots[i]);
            }

            AssertSnapshotsEqual(record.Snapshots[record.Snapshots.Count - 1], replay.FinalSnapshot);
            Assert.That(replay.ActionResults.Count, Is.EqualTo(originalResults.Count));
            for (var i = 0; i < originalResults.Count; i++)
            {
                Assert.That(replay.ActionResults[i].Code, Is.EqualTo(originalResults[i].Code));
                Assert.That(replay.ActionResults[i].FailureReason, Is.EqualTo(originalResults[i].FailureReason));
                Assert.That(replay.ActionResults[i].Description, Is.EqualTo(originalResults[i].Description));
                Assert.That(replay.ActionResults[i].Payload, Is.EqualTo(originalResults[i].Payload));
            }

            Assert.That(replay.FinalSnapshot.SkillCooldowns.Count, Is.GreaterThan(0));
            Assert.That(replay.FinalSnapshot.StatusEffects.Count, Is.GreaterThan(0));
            Assert.That(replay.FinalSnapshot.EntitySkillResources.Count, Is.GreaterThan(0));
            Assert.That(replay.FinalSnapshot.EntityRpgStats.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ReplayRecord_WithDifferentSeed_DivergesFromOriginalOutcome()
        {
            // Arrange : même séquence d'actions, mais seed de replay volontairement différente.
            var state = CreateInitialState();
            var recorder = new BattleReplayRecorder(seed: 11);
            recorder.CaptureInitialState(state);

            var resolver = new ActionResolver(new SeededRngService(11));
            resolver.Resolve(state, new MoveAction(UnitA, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1)));
            recorder.RecordAction(RecordedActionCommand.Move(UnitA, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1)), state);

            var deterministicAttack = new BasicAttackActionPolicy(
                minRange: 1,
                maxRange: 2,
                maxLineOfSightDelta: 2,
                resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 10, baseHitChance: 100));

            resolver.Resolve(state, new BasicAttackAction(UnitA, UnitB, deterministicAttack));
            recorder.RecordAction(RecordedActionCommand.BasicAttack(UnitA, UnitB), state);
            resolver.Resolve(state, new BasicAttackAction(UnitB, UnitA, deterministicAttack));
            recorder.RecordAction(RecordedActionCommand.BasicAttack(UnitB, UnitA), state);
            resolver.Resolve(state, new BasicAttackAction(UnitA, UnitB, deterministicAttack));
            recorder.RecordAction(RecordedActionCommand.BasicAttack(UnitA, UnitB), state);

            var record = recorder.Build();

            // Seed altérée pour prouver la divergence potentielle.
            var altered = new BattleReplayRecord(99, record.InitialSnapshot, record.Actions, record.Snapshots);
            var replay = new BattleReplayRunner().Run(altered);

            // Assert : non équivalence stricte sur le snapshot final (dégâts divergents via variance RNG).
            var originalFinal = record.Snapshots[record.Snapshots.Count - 1];
            var same = SnapshotsAreEqual(originalFinal, replay.FinalSnapshot);
            Assert.That(same, Is.False);
        }

        private static BattleState CreateInitialState()
        {
            var state = new BattleState();
            state.SetEntityPosition(UnitA, new Position3(0, 0, 0));
            state.SetEntityPosition(UnitB, new Position3(2, 0, 1));
            state.SetEntityHitPoints(UnitA, 40);
            state.SetEntityHitPoints(UnitB, 40);
            return state;
        }

        private static bool SnapshotsAreEqual(BattleStateSnapshot a, BattleStateSnapshot b)
        {
            if (a.Tick != b.Tick ||
                a.EntityPositions.Count != b.EntityPositions.Count ||
                a.EntityHitPoints.Count != b.EntityHitPoints.Count ||
                a.EntityMovementPoints.Count != b.EntityMovementPoints.Count ||
                a.EntitySkillResources.Count != b.EntitySkillResources.Count ||
                a.SkillCooldowns.Count != b.SkillCooldowns.Count ||
                a.StatusEffects.Count != b.StatusEffects.Count ||
                a.EntityRpgStats.Count != b.EntityRpgStats.Count)
            {
                return false;
            }

            for (var i = 0; i < a.EntityPositions.Count; i++)
            {
                if (!a.EntityPositions[i].EntityId.Equals(b.EntityPositions[i].EntityId) ||
                    !a.EntityPositions[i].Position.Equals(b.EntityPositions[i].Position))
                {
                    return false;
                }
            }

            for (var i = 0; i < a.EntityHitPoints.Count; i++)
            {
                if (!a.EntityHitPoints[i].EntityId.Equals(b.EntityHitPoints[i].EntityId) ||
                    a.EntityHitPoints[i].HitPoints != b.EntityHitPoints[i].HitPoints)
                {
                    return false;
                }
            }

            for (var i = 0; i < a.EntityMovementPoints.Count; i++)
            {
                if (!a.EntityMovementPoints[i].EntityId.Equals(b.EntityMovementPoints[i].EntityId) ||
                    a.EntityMovementPoints[i].MovementPoints != b.EntityMovementPoints[i].MovementPoints ||
                    a.EntityMovementPoints[i].MaxMovementPoints != b.EntityMovementPoints[i].MaxMovementPoints)
                {
                    return false;
                }
            }

            for (var i = 0; i < a.EntitySkillResources.Count; i++)
            {
                if (!a.EntitySkillResources[i].EntityId.Equals(b.EntitySkillResources[i].EntityId) ||
                    a.EntitySkillResources[i].Amount != b.EntitySkillResources[i].Amount)
                {
                    return false;
                }
            }

            for (var i = 0; i < a.SkillCooldowns.Count; i++)
            {
                if (!a.SkillCooldowns[i].EntityId.Equals(b.SkillCooldowns[i].EntityId) ||
                    a.SkillCooldowns[i].SkillId != b.SkillCooldowns[i].SkillId ||
                    a.SkillCooldowns[i].RemainingTurns != b.SkillCooldowns[i].RemainingTurns)
                {
                    return false;
                }
            }

            for (var i = 0; i < a.StatusEffects.Count; i++)
            {
                if (!a.StatusEffects[i].EntityId.Equals(b.StatusEffects[i].EntityId) ||
                    a.StatusEffects[i].EffectType != b.StatusEffects[i].EffectType ||
                    a.StatusEffects[i].RemainingTurns != b.StatusEffects[i].RemainingTurns ||
                    a.StatusEffects[i].Potency != b.StatusEffects[i].Potency ||
                    a.StatusEffects[i].TickMoment != b.StatusEffects[i].TickMoment)
                {
                    return false;
                }
            }

            for (var i = 0; i < a.EntityRpgStats.Count; i++)
            {
                if (!a.EntityRpgStats[i].EntityId.Equals(b.EntityRpgStats[i].EntityId) ||
                    !a.EntityRpgStats[i].Stats.Equals(b.EntityRpgStats[i].Stats))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertSnapshotsEqual(BattleStateSnapshot expected, BattleStateSnapshot actual)
        {
            Assert.That(SnapshotsAreEqual(expected, actual), Is.True);
        }

        private static readonly EntityId UnitA = new(100);
        private static readonly EntityId UnitB = new(101);
    }
}
