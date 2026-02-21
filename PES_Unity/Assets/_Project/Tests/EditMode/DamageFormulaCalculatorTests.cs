using NUnit.Framework;
using PES.Core.Simulation;

namespace PES.Tests.EditMode
{
    public sealed class DamageFormulaCalculatorTests
    {
        [Test]
        public void Resolve_NonCritical_Blunt_UsesRequestedFormula()
        {
            var attacker = new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 5,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 100,
                assiduity: 0,
                rapidity: 0,
                criticalChance: 12,
                criticalDamage: 25,
                criticalResistance: 0,
                attack: new DamageElementValues(20, 0, 0, 0, 0, 0),
                power: new DamageElementValues(150, 100, 100, 100, 100, 100),
                defense: default,
                resistance: default);

            var defender = new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 5,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 100,
                assiduity: 0,
                rapidity: 0,
                criticalChance: 0,
                criticalDamage: 0,
                criticalResistance: 10,
                attack: default,
                power: DamageElementValuesSerializedLike.HundredPercent,
                defense: new DamageElementValues(5, 0, 0, 0, 0, 0),
                resistance: new DamageElementValues(10, 0, 0, 0, 0, 0));

            var result = DamageFormulaCalculator.Resolve(attacker, defender, spellBaseDamage: 30, spellBaseCriticalChance: 15, DamageElement.Blunt, forceCritical: false);

            Assert.That(result.CriticalChance, Is.EqualTo(27));
            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.FinalDamage, Is.EqualTo(60));
        }

        [Test]
        public void Resolve_Critical_AppliesCriticalMultiplierAndResistanceCritical()
        {
            var attacker = new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 5,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 100,
                assiduity: 0,
                rapidity: 0,
                criticalChance: 12,
                criticalDamage: 25,
                criticalResistance: 0,
                attack: new DamageElementValues(20, 0, 0, 0, 0, 0),
                power: new DamageElementValues(150, 100, 100, 100, 100, 100),
                defense: default,
                resistance: default);

            var defender = new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 5,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 100,
                assiduity: 0,
                rapidity: 0,
                criticalChance: 0,
                criticalDamage: 0,
                criticalResistance: 10,
                attack: default,
                power: DamageElementValuesSerializedLike.HundredPercent,
                defense: new DamageElementValues(5, 0, 0, 0, 0, 0),
                resistance: new DamageElementValues(10, 0, 0, 0, 0, 0));

            var result = DamageFormulaCalculator.Resolve(attacker, defender, spellBaseDamage: 30, spellBaseCriticalChance: 15, DamageElement.Blunt, forceCritical: true);

            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.FinalDamage, Is.EqualTo(84));
        }

        [Test]
        public void BattleState_TryApplyEntityRpgDamage_ReducesDefenderHitPoints()
        {
            var state = new BattleState();
            var attackerId = new EntityId(1);
            var defenderId = new EntityId(2);
            state.SetEntityHitPoints(defenderId, 100);

            var attacker = new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 5,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 100,
                assiduity: 0,
                rapidity: 0,
                criticalChance: 0,
                criticalDamage: 0,
                criticalResistance: 0,
                attack: new DamageElementValues(15, 0, 0, 0, 0, 0),
                power: new DamageElementValues(100, 100, 100, 100, 100, 100),
                defense: default,
                resistance: default);

            var defender = new CombatantRpgStats(
                actionPoints: 6,
                movementPoints: 5,
                range: 1,
                elevation: 1,
                summonCapacity: 1,
                hitPoints: 100,
                assiduity: 0,
                rapidity: 0,
                criticalChance: 0,
                criticalDamage: 0,
                criticalResistance: 0,
                attack: default,
                power: DamageElementValuesSerializedLike.HundredPercent,
                defense: new DamageElementValues(5, 0, 0, 0, 0, 0),
                resistance: default);

            state.SetEntityRpgStats(attackerId, attacker);
            state.SetEntityRpgStats(defenderId, defender);

            var applied = state.TryApplyEntityRpgDamage(attackerId, defenderId, spellBaseDamage: 10, spellBaseCriticalChance: 5, DamageElement.Blunt);

            Assert.That(applied, Is.True);
            Assert.That(state.TryGetEntityHitPoints(defenderId, out var hp), Is.True);
            Assert.That(hp, Is.EqualTo(80));
        }

        private static class DamageElementValuesSerializedLike
        {
            public static DamageElementValues HundredPercent => new(100, 100, 100, 100, 100, 100);
        }
    }
}
