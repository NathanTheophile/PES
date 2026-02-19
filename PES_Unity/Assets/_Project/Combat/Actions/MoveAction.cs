using System;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Combat.Actions
{
    public readonly struct MoveAction : IActionCommand
    {
        private const int MaxDistancePerAction = 3;
        private const int MaxVerticalStep = 1;

        public MoveAction(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
        {
            ActorId = actorId;
            Origin = origin;
            Destination = destination;
        }

        public EntityId ActorId { get; }
        public GridCoord3 Origin { get; }
        public GridCoord3 Destination { get; }

        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            if (!state.TryMoveEntity(ActorId, ToPosition(Origin), ToPosition(Destination)))
            {
                return new ActionResolution(false, $"MoveActionRejected: invalid origin for {ActorId} ({Origin} -> {Destination})");
            }

            var horizontalDistance = Math.Abs(Destination.X - Origin.X) + Math.Abs(Destination.Y - Origin.Y);
            var verticalDelta = Math.Abs(Destination.Z - Origin.Z);

            if (horizontalDistance > MaxDistancePerAction || verticalDelta > MaxVerticalStep)
            {
                state.SetEntityPosition(ActorId, ToPosition(Origin));
                return new ActionResolution(false, $"MoveActionRejected: out of bounds for {ActorId} ({Origin} -> {Destination})");
            }

            return new ActionResolution(true, $"MoveActionResolved: {ActorId} {Origin} -> {Destination}");
        }

        private static Position3 ToPosition(GridCoord3 coord)
        {
            return new Position3(coord.X, coord.Y, coord.Z);
        }
    }
}
