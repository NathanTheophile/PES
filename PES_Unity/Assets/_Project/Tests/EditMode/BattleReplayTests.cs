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
            if (a.Tick != b.Tick || a.EntityPositions.Count != b.EntityPositions.Count || a.EntityHitPoints.Count != b.EntityHitPoints.Count)
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
