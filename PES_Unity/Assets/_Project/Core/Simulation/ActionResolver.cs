// Utility: this script orchestrates the deterministic action pipeline:
// command execution, event recording, and tick progression.
using PES.Core.Random;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Central domain service responsible for resolving action commands.
    /// It enforces a single flow: resolve command -> log event -> advance simulation tick.
    /// </summary>
    public sealed class ActionResolver
    {
        // Centralized RNG instance injected once, enabling reproducible seeded simulations.
        private readonly IRngService _rngService;

        /// <summary>
        /// Creates a resolver bound to a specific RNG service.
        /// </summary>
        public ActionResolver(IRngService rngService)
        {
            _rngService = rngService;
        }

        /// <summary>
        /// Executes one action command and applies standard pipeline post-processing.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IActionCommand command)
        {
            // 1) Let the action validate itself and mutate state as needed.
            var result = command.Resolve(state, _rngService);

            // 2) Persist a human-readable event for replay/debug traces.
            state.AddEvent(result.Description);

            // 3) Advance deterministic timeline after each resolved command.
            state.AdvanceTick();
            return result;
        }
    }

    /// <summary>
    /// Standard output DTO describing the result of command resolution.
    /// </summary>
    public readonly struct ActionResolution
    {
        /// <summary>
        /// Creates a new action result object.
        /// </summary>
        public ActionResolution(bool success, string description)
        {
            Success = success;
            Description = description;
        }

        /// <summary>
        /// True when the action succeeded and produced an accepted gameplay result.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Text summary intended for logs/debug and eventual telemetry/event stream.
        /// </summary>
        public string Description { get; }
    }
}
