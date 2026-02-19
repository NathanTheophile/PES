// Utility: this script defines the contract that every gameplay action must implement
// to be executable by the central action resolver pipeline.
using PES.Core.Random;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Domain command abstraction for action-driven simulation.
    /// Implementations encapsulate both validation and resulting state mutation logic.
    /// </summary>
    public interface IActionCommand
    {
        /// <summary>
        /// Resolves this action against the provided battle state using centralized RNG access.
        /// </summary>
        ActionResolution Resolve(BattleState state, IRngService rngService);
    }
}
