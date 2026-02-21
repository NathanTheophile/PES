namespace PES.Core.Simulation
{
    public readonly struct DamageFormulaResolution
    {
        public DamageFormulaResolution(int finalDamage, bool isCritical, int criticalChance)
        {
            FinalDamage = finalDamage;
            IsCritical = isCritical;
            CriticalChance = criticalChance;
        }

        public int FinalDamage { get; }

        public bool IsCritical { get; }

        public int CriticalChance { get; }
    }

    public static class DamageFormulaCalculator
    {
        public static DamageFormulaResolution Resolve(
            CombatantRpgStats attacker,
            CombatantRpgStats defender,
            int spellBaseDamage,
            int spellBaseCriticalChance,
            DamageElement element,
            bool forceCritical = false)
        {
            var attack = attacker.Attack.GetValue(element);
            var defense = defender.Defense.GetValue(element);
            var powerPercent = attacker.Power.GetValue(element);
            var resistancePercent = defender.Resistance.GetValue(element);

            var prePowerDamage = spellBaseDamage + attack - defense;
            var poweredDamage = prePowerDamage * (powerPercent / 100f);
            var afterResistanceDamage = poweredDamage * (1f - (resistancePercent / 100f));
            var normalizedDamage = System.MathF.Floor(afterResistanceDamage);

            var criticalChance = spellBaseCriticalChance + attacker.CriticalChance;
            var isCritical = forceCritical;
            var finalDamage = normalizedDamage;

            if (isCritical)
            {
                var criticalMultiplier = (1.25f + (attacker.CriticalDamage / 100f)) - (defender.CriticalResistance / 100f);
                finalDamage *= criticalMultiplier;
            }

            var rounded = (int)System.MathF.Floor(finalDamage);
            return new DamageFormulaResolution(rounded < 0 ? 0 : rounded, isCritical, criticalChance);
        }
    }
}
