namespace PES.Core.Simulation
{
    public enum DamageElement
    {
        Blunt = 0,
        Physical = 1,
        Piercing = 2,
        Explosive = 3,
        Elemental = 4,
        Spiritual = 5,
    }

    public readonly struct DamageElementValues
    {
        public DamageElementValues(int blunt, int physical, int piercing, int explosive, int elemental, int spiritual)
        {
            Blunt = blunt;
            Physical = physical;
            Piercing = piercing;
            Explosive = explosive;
            Elemental = elemental;
            Spiritual = spiritual;
        }

        public int Blunt { get; }
        public int Physical { get; }
        public int Piercing { get; }
        public int Explosive { get; }
        public int Elemental { get; }
        public int Spiritual { get; }

        public int GetValue(DamageElement element)
        {
            return element switch
            {
                DamageElement.Blunt => Blunt,
                DamageElement.Physical => Physical,
                DamageElement.Piercing => Piercing,
                DamageElement.Explosive => Explosive,
                DamageElement.Elemental => Elemental,
                DamageElement.Spiritual => Spiritual,
                _ => 0,
            };
        }
    }

    public readonly struct CombatantRpgStats
    {
        public CombatantRpgStats(
            int actionPoints,
            int movementPoints,
            int range,
            int elevation,
            int summonCapacity,
            int hitPoints,
            int assiduity,
            int rapidity,
            int criticalChance,
            int criticalDamage,
            int criticalResistance,
            DamageElementValues attack,
            DamageElementValues power,
            DamageElementValues defense,
            DamageElementValues resistance)
        {
            ActionPoints = actionPoints;
            MovementPoints = movementPoints;
            Range = range;
            Elevation = elevation;
            SummonCapacity = summonCapacity;
            HitPoints = hitPoints;
            Assiduity = assiduity;
            Rapidity = rapidity;
            CriticalChance = criticalChance;
            CriticalDamage = criticalDamage;
            CriticalResistance = criticalResistance;
            Attack = attack;
            Power = power;
            Defense = defense;
            Resistance = resistance;
        }

        public int ActionPoints { get; }
        public int MovementPoints { get; }
        public int Range { get; }
        public int Elevation { get; }
        public int SummonCapacity { get; }
        public int HitPoints { get; }
        public int Assiduity { get; }
        public int Rapidity { get; }
        public int CriticalChance { get; }
        public int CriticalDamage { get; }
        public int CriticalResistance { get; }

        public DamageElementValues Attack { get; }
        public DamageElementValues Power { get; }
        public DamageElementValues Defense { get; }
        public DamageElementValues Resistance { get; }

        public static CombatantRpgStats Empty => new(
            actionPoints: 0,
            movementPoints: 0,
            range: 0,
            elevation: 0,
            summonCapacity: 0,
            hitPoints: 0,
            assiduity: 0,
            rapidity: 0,
            criticalChance: 0,
            criticalDamage: 0,
            criticalResistance: 0,
            attack: default,
            power: new DamageElementValues(100, 100, 100, 100, 100, 100),
            defense: default,
            resistance: default);
    }
}
