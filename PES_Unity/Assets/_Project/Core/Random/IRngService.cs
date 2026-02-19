// Utility: this script defines a centralized random service abstraction for deterministic gameplay.
namespace PES.Core.Random
{
    /// <summary>
    /// RNG contract used by domain rules so randomness is explicit, injectable, and reproducible.
    /// </summary>
    public interface IRngService
    {
        /// <summary>
        /// Returns an integer in [minInclusive, maxExclusive).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);
    }
}
