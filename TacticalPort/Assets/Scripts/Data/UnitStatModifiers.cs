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
        ActionPoints = 1,
        Movement = 2,
        MeleeDamage = 3,
        RangedDamage = 4,
        Initiative = 5,
        MeleeResistance = 6,
        RangedResistance = 7,
        RemainingPoints = 8
    }

    public sealed class UnitStatModifiers
    {
        public static readonly UnitStatModifiers None = new UnitStatModifiers();

        public int Health { get; }
        public int ActionPoints { get; }
        public int Movement { get; }
        public int MeleeDamage { get; }
        public int MeleeResistance { get; }
        public int RangedDamage { get; }
        public int RangedResistance { get; }
        public int Initiative { get; }

        public UnitStatModifiers(
            int pHealth = 0,
            int pActionPoints = 0,
            int pMovement = 0,
            int pMeleeDamage = 0,
            int pMeleeResistance = 0,
            int pRangedDamage = 0,
            int pRangedResistance = 0,
            int pInitiative = 0)
        {
            Health = pHealth;
            ActionPoints = pActionPoints;
            Movement = pMovement;
            MeleeDamage = pMeleeDamage;
            MeleeResistance = pMeleeResistance;
            RangedDamage = pRangedDamage;
            RangedResistance = pRangedResistance;
            Initiative = pInitiative;
        }
    }
}
