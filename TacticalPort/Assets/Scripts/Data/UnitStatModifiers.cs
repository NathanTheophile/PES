#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Validated runtime stat bonuses resolved from a persisted unit build.
//  Data
#endregion

namespace TacticalPort.Data
{
    public enum UnitStatType
    {
        Health = 0,
        Energy = 1,
        Mobility = 2,
        MeleeDamage = 3,
        RangedDamage = 4,
        Velocity = 5,
        MeleeResistance = 6,
        RangedResistance = 7,
        RemainingPoints = 8
    }

    public sealed class UnitStatModifiers
    {
        public static readonly UnitStatModifiers None = new UnitStatModifiers();

        public int Health { get; }
        public int Energy { get; }
        public int Mobility { get; }
        public int MeleeDamage { get; }
        public int MeleeResistance { get; }
        public int RangedDamage { get; }
        public int RangedResistance { get; }
        public int Velocity { get; }

        public UnitStatModifiers(
            int pHealth = 0,
            int pEnergy = 0,
            int pMobility = 0,
            int pMeleeDamage = 0,
            int pMeleeResistance = 0,
            int pRangedDamage = 0,
            int pRangedResistance = 0,
            int pVelocity = 0)
        {
            Health = pHealth;
            Energy = pEnergy;
            Mobility = pMobility;
            MeleeDamage = pMeleeDamage;
            MeleeResistance = pMeleeResistance;
            RangedDamage = pRangedDamage;
            RangedResistance = pRangedResistance;
            Velocity = pVelocity;
        }
    }
}
