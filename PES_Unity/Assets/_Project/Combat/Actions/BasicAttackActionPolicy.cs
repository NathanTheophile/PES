using PES.Combat.Resolution;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Politique data-driven d'une attaque basique (portée/LOS/résolution).
    /// </summary>
    public readonly struct BasicAttackActionPolicy
    {
        public BasicAttackActionPolicy(
            int minRange,
            int maxRange,
            int maxLineOfSightDelta,
            BasicAttackResolutionPolicy resolutionPolicy,
            DamageElement damageElement = DamageElement.Physical,
            int baseCriticalChance = 5)
        {
            MinRange = minRange;
            MaxRange = maxRange;
            MaxLineOfSightDelta = maxLineOfSightDelta;
            ResolutionPolicy = resolutionPolicy;
            DamageElement = damageElement;
            BaseCriticalChance = baseCriticalChance;
        }

        public int MinRange { get; }

        public int MaxRange { get; }

        public int MaxLineOfSightDelta { get; }

        public BasicAttackResolutionPolicy ResolutionPolicy { get; }

        public DamageElement DamageElement { get; }

        public int BaseCriticalChance { get; }
    }
}
