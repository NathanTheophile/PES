using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Simulation;

namespace PES.Tests.EditMode
{
    public sealed class StatusEffectDamageModifierTests
    {
        [Test]
        public void Apply_WithoutStatuses_ReturnsBaseDamage()
        {
            var state = new BattleState();
            var attacker = new EntityId(1000);
            var defender = new EntityId(1001);

            var adjusted = StatusEffectDamageModifier.Apply(state, attacker, defender, 12);

            Assert.That(adjusted, Is.EqualTo(12));
        }

        [Test]
        public void Apply_WithWeakenedFortifiedAndMarked_CombinesDeterministically()
        {
            var state = new BattleState();
            var attacker = new EntityId(1002);
            var defender = new EntityId(1003);

            state.SetStatusEffect(attacker, StatusEffectType.Weakened, remainingTurns: 2, potency: 3);
            state.SetStatusEffect(defender, StatusEffectType.Fortified, remainingTurns: 2, potency: 2);
            state.SetStatusEffect(defender, StatusEffectType.Marked, remainingTurns: 2, potency: 4);

            var adjusted = StatusEffectDamageModifier.Apply(state, attacker, defender, 10);

            Assert.That(adjusted, Is.EqualTo(9)); // 10 - 3 - 2 + 4
        }
    }
}
