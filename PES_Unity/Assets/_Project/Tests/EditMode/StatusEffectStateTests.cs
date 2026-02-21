using NUnit.Framework;
using PES.Core.Simulation;
using PES.Presentation.Scene;

namespace PES.Tests.EditMode
{
    public sealed class StatusEffectStateTests
    {
        [Test]
        public void TickStatusEffects_WithPoison_AppliesDamageAndTicksDuration()
        {
            var state = new BattleState();
            var actor = new EntityId(900);
            state.SetEntityHitPoints(actor, 20);
            state.SetStatusEffect(actor, StatusEffectType.Poison, remainingTurns: 3, potency: 2);

            var damage = state.TickStatusEffects(actor);

            Assert.That(damage, Is.EqualTo(2));
            Assert.That(state.TryGetEntityHitPoints(actor, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(18));
            Assert.That(state.GetStatusEffectRemaining(actor, StatusEffectType.Poison), Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_WithStatusEffects_RestoresRemainingTurnsAndPotency()
        {
            var state = new BattleState();
            var actor = new EntityId(901);
            state.SetEntityHitPoints(actor, 30);
            state.SetStatusEffect(actor, StatusEffectType.Poison, remainingTurns: 2, potency: 3);

            var snapshot = state.CreateSnapshot();

            var restored = new BattleState();
            restored.ApplySnapshot(snapshot);

            Assert.That(restored.GetStatusEffectRemaining(actor, StatusEffectType.Poison), Is.EqualTo(2));
            var damage = restored.TickStatusEffects(actor);
            Assert.That(damage, Is.EqualTo(3));
            Assert.That(restored.TryGetEntityHitPoints(actor, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(27));
        }

        [Test]
        public void VerticalSliceLoop_EndTurn_TicksStatusesForIncomingActor()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);
            loop.State.SetStatusEffect(VerticalSliceBattleLoop.UnitB, StatusEffectType.Poison, remainingTurns: 2, potency: 4);
            loop.State.SetEntityHitPoints(VerticalSliceBattleLoop.UnitB, 25);

            var passed = loop.TryPassTurn(VerticalSliceBattleLoop.UnitA, out var result);

            Assert.That(passed, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(loop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitB, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(21));
            Assert.That(loop.State.GetStatusEffectRemaining(VerticalSliceBattleLoop.UnitB, StatusEffectType.Poison), Is.EqualTo(1));
            Assert.That(loop.State.EventLog[loop.State.EventLog.Count - 2], Does.Contain("StatusTick"));
        }
    }
}
