// Utility: this script provides a seed-based RNG implementation used to keep simulations reproducible.
using System;

namespace PES.Core.Random
{
    /// <summary>
    /// Deterministic RNG adapter backed by System.Random with explicit seed storage.
    /// </summary>
    public sealed class SeededRngService : IRngService
    {
        // Internal PRNG instance. Same seed -> same random sequence.
        private readonly Random _random;

        /// <summary>
        /// Creates a deterministic RNG with a fixed seed.
        /// </summary>
        public SeededRngService(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>
        /// Seed used to initialize this RNG instance.
        /// </summary>
        public int Seed { get; }

        /// <summary>
        /// Returns the next pseudo-random integer in the requested range.
        /// </summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
