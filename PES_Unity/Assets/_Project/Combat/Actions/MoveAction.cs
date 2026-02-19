// Utility: this script implements the first domain movement command
// with basic validation for origin consistency, distance, and vertical step.
using System;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Command representing a move attempt from one grid coordinate to another.
    /// </summary>
    public readonly struct MoveAction : IActionCommand
    {
        // Bootstrap movement cap in horizontal Manhattan distance.
        private const int MaxDistancePerAction = 3;

        // Bootstrap movement cap for one action in vertical delta.
        private const int MaxVerticalStep = 1;

        /// <summary>
        /// Creates a movement command for a specific actor.
        /// </summary>
        public MoveAction(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
        {
            ActorId = actorId;
            Origin = origin;
            Destination = destination;
        }

        /// <summary>Actor that tries to move.</summary>
        public EntityId ActorId { get; }

        /// <summary>Expected actor origin used for stale-command validation.</summary>
        public GridCoord3 Origin { get; }

        /// <summary>Requested destination.</summary>
        public GridCoord3 Destination { get; }

        /// <summary>
        /// Resolves movement against current battle state.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            // First guard: entity must exist and be exactly at the declared origin.
            if (!state.TryMoveEntity(ActorId, ToPosition(Origin), ToPosition(Destination)))
            {
                return new ActionResolution(false, $"MoveActionRejected: invalid origin for {ActorId} ({Origin} -> {Destination})");
            }

            // Compute horizontal and vertical costs in grid space.
            var horizontalDistance = Math.Abs(Destination.X - Origin.X) + Math.Abs(Destination.Y - Origin.Y);
            var verticalDelta = Math.Abs(Destination.Z - Origin.Z);

            // Reject over-budget moves and rollback the temporary state mutation.
            if (horizontalDistance > MaxDistancePerAction || verticalDelta > MaxVerticalStep)
            {
                state.SetEntityPosition(ActorId, ToPosition(Origin));
                return new ActionResolution(false, $"MoveActionRejected: out of bounds for {ActorId} ({Origin} -> {Destination})");
            }

            // Action accepted.
            return new ActionResolution(true, $"MoveActionResolved: {ActorId} {Origin} -> {Destination}");
        }

        // Mapping helper between grid coordinate type and state storage coordinate type.
        private static Position3 ToPosition(GridCoord3 coord)
        {
            return new Position3(coord.X, coord.Y, coord.Z);
        }
    }
}
