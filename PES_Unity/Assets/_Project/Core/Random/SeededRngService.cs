// Utilité : ce script fournit une implémentation RNG à seed fixe pour des simulations reproductibles.
using System;

namespace PES.Core.Random
{
    /// <summary>
    /// Adaptateur RNG déterministe basé sur System.Random avec seed explicite.
    /// </summary>
    public sealed class SeededRngService : IRngService
    {
        // PRNG interne : même seed => même séquence pseudo-aléatoire.
        private readonly Random _random;

        /// <summary>
        /// Construit un RNG déterministe avec une seed donnée.
        /// </summary>
        public SeededRngService(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>
        /// Seed utilisée pour initialiser l'instance.
        /// </summary>
        public int Seed { get; }

        /// <summary>
        /// Retourne la prochaine valeur pseudo-aléatoire dans l'intervalle demandé.
        /// </summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
