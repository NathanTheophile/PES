using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Combat.Actions
{
    public readonly struct MoveAction : IActionCommand
    {
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
            return new ActionResolution(true, $"MoveAction: {ActorId} {Origin} -> {Destination}");
        }
    }
}
