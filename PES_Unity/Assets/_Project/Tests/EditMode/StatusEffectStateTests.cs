using NUnit.Framework;
using PES.Combat.Actions;
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
        public void TickStatusEffects_WithNonDamageStatus_TicksDurationWithoutDamage()
        {
            var state = new BattleState();
            var actor = new EntityId(902);
            state.SetEntityHitPoints(actor, 20);
            state.SetStatusEffect(actor, StatusEffectType.Weakened, remainingTurns: 2, potency: 3, tickMoment: StatusEffectTickMoment.TurnEnd);

            var damage = state.TickStatusEffects(actor, StatusEffectTickMoment.TurnEnd);

            Assert.That(damage, Is.EqualTo(0));
            Assert.That(state.TryGetEntityHitPoints(actor, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(20));
            Assert.That(state.GetStatusEffectRemaining(actor, StatusEffectType.Weakened), Is.EqualTo(1));
        }

        [Test]
        public void Actions_WhenStunned_AreRejectedWithActionInterrupted()
        {
            var state = new BattleState();
            var actor = new EntityId(903);
            var target = new EntityId(904);
            state.SetEntityPosition(actor, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(actor, 20);
            state.SetEntityHitPoints(target, 20);
            state.SetEntitySkillResource(actor, 10);
            state.SetStatusEffect(actor, StatusEffectType.Stunned, remainingTurns: 1, potency: 0, tickMoment: StatusEffectTickMoment.TurnStart);

            var resolver = new ActionResolver(new SeededRngService(11));

            var moveResult = resolver.Resolve(state, new MoveAction(actor, new PES.Grid.Grid3D.GridCoord3(0, 0, 0), new PES.Grid.Grid3D.GridCoord3(1, 0, 0)));
            var attackResult = resolver.Resolve(state, new BasicAttackAction(actor, target));
            var skillResult = resolver.Resolve(state, new CastSkillAction(actor, target, new SkillActionPolicy(999, 1, 3, 4, 100, 2, 1)));

            Assert.That(moveResult.FailureReason, Is.EqualTo(ActionFailureReason.ActionInterrupted));
            Assert.That(attackResult.FailureReason, Is.EqualTo(ActionFailureReason.ActionInterrupted));
            Assert.That(skillResult.FailureReason, Is.EqualTo(ActionFailureReason.ActionInterrupted));

            var tickDamage = state.TickStatusEffects(actor, StatusEffectTickMoment.TurnStart);
            Assert.That(tickDamage, Is.EqualTo(0));
            Assert.That(state.GetStatusEffectRemaining(actor, StatusEffectType.Stunned), Is.EqualTo(0));
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
