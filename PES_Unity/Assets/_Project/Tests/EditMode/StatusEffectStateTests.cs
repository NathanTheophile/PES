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

            var damage = state.TickStatusEffects(actor, StatusEffectTickMoment.TurnStart);

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
            var damage = restored.TickStatusEffects(actor, StatusEffectTickMoment.TurnStart);
            Assert.That(damage, Is.EqualTo(3));
            Assert.That(restored.TryGetEntityHitPoints(actor, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(27));
        }

        [Test]
        public void VulnerableStatus_AmplifiesIncomingDamage_AndExpiresOnConfiguredTick()
        {
            var state = new BattleState();
            var actor = new EntityId(902);
            state.SetEntityHitPoints(actor, 20);
            state.SetStatusEffect(actor, StatusEffectType.Vulnerable, remainingTurns: 1, potency: 50, tickMoment: StatusEffectTickMoment.TurnStart);

            state.TryApplyDamage(actor, 10);
            Assert.That(state.TryGetEntityHitPoints(actor, out var hpAfterAmp), Is.True);
            Assert.That(hpAfterAmp, Is.EqualTo(5));

            var tickDamage = state.TickStatusEffects(actor, StatusEffectTickMoment.TurnStart);
            Assert.That(tickDamage, Is.EqualTo(0));
            Assert.That(state.GetStatusEffectRemaining(actor, StatusEffectType.Vulnerable), Is.EqualTo(0));
        }

        [Test]
        public void VerticalSliceLoop_TicksTurnStartAndTurnEndStatusesAccordingToTiming()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);
            loop.State.SetEntityHitPoints(VerticalSliceBattleLoop.UnitA, 30);
            loop.State.SetEntityHitPoints(VerticalSliceBattleLoop.UnitB, 25);

            loop.State.SetStatusEffect(VerticalSliceBattleLoop.UnitA, StatusEffectType.Poison, remainingTurns: 2, potency: 3, tickMoment: StatusEffectTickMoment.TurnEnd);
            loop.State.SetStatusEffect(VerticalSliceBattleLoop.UnitB, StatusEffectType.Poison, remainingTurns: 2, potency: 4, tickMoment: StatusEffectTickMoment.TurnStart);

            var passed = loop.TryPassTurn(VerticalSliceBattleLoop.UnitA, out var result);

            Assert.That(passed, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(loop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitA, out var hpA), Is.True);
            Assert.That(hpA, Is.EqualTo(27));
            Assert.That(loop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitB, out var hpB), Is.True);
            Assert.That(hpB, Is.EqualTo(21));
            Assert.That(loop.State.GetStatusEffectRemaining(VerticalSliceBattleLoop.UnitA, StatusEffectType.Poison), Is.EqualTo(1));
            Assert.That(loop.State.GetStatusEffectRemaining(VerticalSliceBattleLoop.UnitB, StatusEffectType.Poison), Is.EqualTo(1));
            Assert.That(loop.State.EventLog[loop.State.EventLog.Count - 3], Does.Contain("StatusTickEnd"));
            Assert.That(loop.State.EventLog[loop.State.EventLog.Count - 2], Does.Contain("StatusTickStart"));
        }
    }
}
