// Utilité : ce script définit le contrat de règles d'une compétence ciblée
// pour rester data-driven et indépendante de la présentation Unity.
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
            int rangeBonusPerElevationStep)
        {
            SkillId = skillId;
            MinRange = minRange;
            MaxRange = maxRange;
            BaseDamage = baseDamage;
            BaseHitChance = baseHitChance;
            ElevationPerRangeBonus = elevationPerRangeBonus;
            RangeBonusPerElevationStep = rangeBonusPerElevationStep;
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

        public bool IsValid =>
            SkillId >= 0 &&
            MinRange >= 0 &&
            MaxRange >= MinRange &&
            BaseDamage >= 0 &&
            BaseHitChance is >= 0 and <= 100 &&
            ElevationPerRangeBonus > 0 &&
            RangeBonusPerElevationStep >= 0;
    }
}
