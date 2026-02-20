using PES.Combat.Resolution;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Politique data-driven d'une attaque basique (portée/LOS/résolution).
    /// </summary>
    public readonly struct BasicAttackActionPolicy
    {
        public BasicAttackActionPolicy(int minRange, int maxRange, int maxLineOfSightDelta, BasicAttackResolutionPolicy resolutionPolicy)
        {
            MinRange = minRange;
            MaxRange = maxRange;
            MaxLineOfSightDelta = maxLineOfSightDelta;
            ResolutionPolicy = resolutionPolicy;
        }

        public int MinRange { get; }

        public int MaxRange { get; }

        public int MaxLineOfSightDelta { get; }

        public BasicAttackResolutionPolicy ResolutionPolicy { get; }
    }
}
