// Utility: this script provides a first deterministic-ready basic attack command stub.
// It demonstrates RNG usage through the centralized service.
using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Command representing a direct basic attack from one entity to another.
    /// </summary>
    public readonly struct BasicAttackAction : IActionCommand
    {
        /// <summary>
        /// Creates a basic attack command.
        /// </summary>
        public BasicAttackAction(EntityId attackerId, EntityId targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        /// <summary>Attacking entity identifier.</summary>
        public EntityId AttackerId { get; }

        /// <summary>Target entity identifier.</summary>
        public EntityId TargetId { get; }

        /// <summary>
        /// Resolves a temporary hit/miss attack rule using centralized RNG.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            // Roll in [0,100) for a bootstrap hit chance.
            var roll = rngService.NextInt(0, 100);

            // Temporary rule: 75% hit chance (>= 25).
            var hit = roll >= 25;
            var summary = hit ? "hit" : "miss";

            // Return result details for event logs and debugging.
            return new ActionResolution(hit, $"BasicAttackAction: {AttackerId} -> {TargetId} [{summary}:{roll}]");
        }
    }
}
