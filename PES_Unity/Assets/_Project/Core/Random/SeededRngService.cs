using System;

namespace PES.Core.Random
{
    public sealed class SeededRngService : IRngService
    {
        private readonly Random _random;

        public SeededRngService(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        public int Seed { get; }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
