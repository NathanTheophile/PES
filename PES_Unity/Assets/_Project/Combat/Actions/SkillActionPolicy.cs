// Utilité : ce script définit le contrat de règles d'une compétence ciblée
// pour rester data-driven et indépendante de la présentation Unity.
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Politique métier d'une compétence ciblée (portée, dégâts, précision, scaling d'élévation).
    /// </summary>
    public readonly struct SkillActionPolicy
    {
        public SkillActionPolicy(
            int skillId,
            int minRange,
            int maxRange,
            int baseDamage,
            int baseHitChance,
            int elevationPerRangeBonus,
            int rangeBonusPerElevationStep,
            int resourceCost = 0,
            int cooldownTurns = 0,
            int splashRadiusXZ = 0,
            int splashDamagePercent = 0,
            int periodicDamage = 0,
            int periodicDurationTurns = 0,
            StatusEffectTickMoment periodicTickMoment = StatusEffectTickMoment.TurnStart,
            int vulnerablePotencyPercent = 0,
            int vulnerableDurationTurns = 0)
        {
            SkillId = skillId;
            MinRange = minRange;
            MaxRange = maxRange;
            BaseDamage = baseDamage;
            BaseHitChance = baseHitChance;
            ElevationPerRangeBonus = elevationPerRangeBonus;
            RangeBonusPerElevationStep = rangeBonusPerElevationStep;
            ResourceCost = resourceCost;
            CooldownTurns = cooldownTurns;
            SplashRadiusXZ = splashRadiusXZ;
            SplashDamagePercent = splashDamagePercent;
            PeriodicDamage = periodicDamage;
            PeriodicDurationTurns = periodicDurationTurns;
            PeriodicTickMoment = periodicTickMoment;
            VulnerablePotencyPercent = vulnerablePotencyPercent;
            VulnerableDurationTurns = vulnerableDurationTurns;
        }

        public int SkillId { get; }

        public int MinRange { get; }

        public int MaxRange { get; }

        public int BaseDamage { get; }

        public int BaseHitChance { get; }

        /// <summary>
        /// Taille d'une tranche d'élévation (Y) donnant un bonus de portée.
        /// </summary>
        public int ElevationPerRangeBonus { get; }

        /// <summary>
        /// Bonus de portée ajouté par tranche d'élévation.
        /// </summary>
        public int RangeBonusPerElevationStep { get; }

        public int ResourceCost { get; }

        public int CooldownTurns { get; }

        public int SplashRadiusXZ { get; }

        public int SplashDamagePercent { get; }

        public int PeriodicDamage { get; }

        public int PeriodicDurationTurns { get; }

        public StatusEffectTickMoment PeriodicTickMoment { get; }

        public int VulnerablePotencyPercent { get; }

        public int VulnerableDurationTurns { get; }

        public bool IsValid =>
            SkillId >= 0 &&
            MinRange >= 0 &&
            MaxRange >= MinRange &&
            BaseDamage >= 0 &&
            BaseHitChance is >= 0 and <= 100 &&
            ElevationPerRangeBonus > 0 &&
            RangeBonusPerElevationStep >= 0 &&
            ResourceCost >= 0 &&
            CooldownTurns >= 0 &&
            SplashRadiusXZ >= 0 &&
            SplashDamagePercent is >= 0 and <= 100 &&
            PeriodicDamage >= 0 &&
            PeriodicDurationTurns >= 0 &&
            VulnerablePotencyPercent is >= 0 and <= 100 &&
            VulnerableDurationTurns >= 0;
    }
}
