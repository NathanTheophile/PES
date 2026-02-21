using NUnit.Framework;
using PES.Core.Simulation;

namespace PES.Tests.EditMode
{
    public sealed class BattleStateSnapshotDeterminismTests
    {
        [Test]
        public void CreateSnapshot_UsesStableOrderingAndApplySnapshotKeepsAllData()
        {
            var entityA = new EntityId(30);
            var entityB = new EntityId(10);
            var entityC = new EntityId(20);

            var state = new BattleState();
            state.SetEntityPosition(entityA, new Position3(3, 0, 0));
            state.SetEntityPosition(entityB, new Position3(1, 0, 0));
            state.SetEntityPosition(entityC, new Position3(2, 0, 0));
            state.SetEntityHitPoints(entityA, 31);
            state.SetEntityHitPoints(entityB, 11);
            state.SetEntityHitPoints(entityC, 21);
            state.SetEntityMovementPoints(entityA, 8, 4);
            state.SetEntityMovementPoints(entityB, 6, 3);
            state.SetEntityMovementPoints(entityC, 7, 2);
            state.SetEntitySkillResource(entityA, 5);
            state.SetEntitySkillResource(entityB, 9);
            state.SetEntitySkillResource(entityC, 1);
            state.SetSkillCooldown(entityA, 2, 3);
            state.SetSkillCooldown(entityA, 1, 4);
            state.SetSkillCooldown(entityB, 4, 2);
            state.SetStatusEffect(entityA, StatusEffectType.Poison, 3, 5, StatusEffectTickMoment.TurnEnd);
            state.SetStatusEffect(entityC, StatusEffectType.Poison, 2, 2, StatusEffectTickMoment.TurnStart);
            state.SetEntityRpgStats(entityA, new CombatantRpgStats(50, 10, 0, 5, 2, 20, 0, 0, 0, 0, 0, default, default, default, default));
            state.SetEntityRpgStats(entityB, new CombatantRpgStats(40, 12, 0, 7, 2, 25, 0, 0, 0, 0, 0, default, default, default, default));
            state.SetEntityRpgStats(entityC, new CombatantRpgStats(30, 11, 0, 4, 2, 22, 0, 0, 0, 0, 0, default, default, default, default));

            var snapshot = state.CreateSnapshot();

            Assert.That(snapshot.EntityPositions[0].EntityId.Value, Is.EqualTo(10));
            Assert.That(snapshot.EntityPositions[1].EntityId.Value, Is.EqualTo(20));
            Assert.That(snapshot.EntityPositions[2].EntityId.Value, Is.EqualTo(30));

            Assert.That(snapshot.EntityHitPoints[0].EntityId.Value, Is.EqualTo(10));
            Assert.That(snapshot.EntityMovementPoints[0].EntityId.Value, Is.EqualTo(10));
            Assert.That(snapshot.EntitySkillResources[0].EntityId.Value, Is.EqualTo(10));
            Assert.That(snapshot.SkillCooldowns[0].EntityId.Value, Is.EqualTo(10));
            Assert.That(snapshot.SkillCooldowns[1].SkillId, Is.EqualTo(1));
            Assert.That(snapshot.SkillCooldowns[2].SkillId, Is.EqualTo(2));
            Assert.That(snapshot.StatusEffects[0].EntityId.Value, Is.EqualTo(20));
            Assert.That(snapshot.EntityRpgStats[0].EntityId.Value, Is.EqualTo(10));

            var restored = new BattleState();
            restored.ApplySnapshot(snapshot);
            var restoredSnapshot = restored.CreateSnapshot();

            Assert.That(restoredSnapshot.Tick, Is.EqualTo(snapshot.Tick));
            Assert.That(restoredSnapshot.EntityPositions.Count, Is.EqualTo(snapshot.EntityPositions.Count));
            Assert.That(restoredSnapshot.EntityHitPoints.Count, Is.EqualTo(snapshot.EntityHitPoints.Count));
            Assert.That(restoredSnapshot.EntityMovementPoints.Count, Is.EqualTo(snapshot.EntityMovementPoints.Count));
            Assert.That(restoredSnapshot.EntitySkillResources.Count, Is.EqualTo(snapshot.EntitySkillResources.Count));
            Assert.That(restoredSnapshot.SkillCooldowns.Count, Is.EqualTo(snapshot.SkillCooldowns.Count));
            Assert.That(restoredSnapshot.StatusEffects.Count, Is.EqualTo(snapshot.StatusEffects.Count));
            Assert.That(restoredSnapshot.EntityRpgStats.Count, Is.EqualTo(snapshot.EntityRpgStats.Count));

            for (var i = 0; i < snapshot.EntityPositions.Count; i++)
            {
                Assert.That(restoredSnapshot.EntityPositions[i].EntityId, Is.EqualTo(snapshot.EntityPositions[i].EntityId));
                Assert.That(restoredSnapshot.EntityPositions[i].Position, Is.EqualTo(snapshot.EntityPositions[i].Position));
            }

            for (var i = 0; i < snapshot.SkillCooldowns.Count; i++)
            {
                Assert.That(restoredSnapshot.SkillCooldowns[i].EntityId, Is.EqualTo(snapshot.SkillCooldowns[i].EntityId));
                Assert.That(restoredSnapshot.SkillCooldowns[i].SkillId, Is.EqualTo(snapshot.SkillCooldowns[i].SkillId));
                Assert.That(restoredSnapshot.SkillCooldowns[i].RemainingTurns, Is.EqualTo(snapshot.SkillCooldowns[i].RemainingTurns));
            }

            for (var i = 0; i < snapshot.StatusEffects.Count; i++)
            {
                Assert.That(restoredSnapshot.StatusEffects[i].EntityId, Is.EqualTo(snapshot.StatusEffects[i].EntityId));
                Assert.That(restoredSnapshot.StatusEffects[i].EffectType, Is.EqualTo(snapshot.StatusEffects[i].EffectType));
                Assert.That(restoredSnapshot.StatusEffects[i].RemainingTurns, Is.EqualTo(snapshot.StatusEffects[i].RemainingTurns));
                Assert.That(restoredSnapshot.StatusEffects[i].Potency, Is.EqualTo(snapshot.StatusEffects[i].Potency));
                Assert.That(restoredSnapshot.StatusEffects[i].TickMoment, Is.EqualTo(snapshot.StatusEffects[i].TickMoment));
            }
        }
    }
}
