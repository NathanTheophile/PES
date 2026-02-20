using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Infrastructure.Replay
{
    /// <summary>
    /// Action sérialisable minimale pour rejouer une simulation sans dépendre d'objets runtime Unity.
    /// </summary>
    public readonly struct RecordedActionCommand
    {
        private RecordedActionCommand(
            RecordedActionType actionType,
            EntityId actorId,
            EntityId targetId,
            GridCoord3 origin,
            GridCoord3 destination)
        {
            ActionType = actionType;
            ActorId = actorId;
            TargetId = targetId;
            Origin = origin;
            Destination = destination;
        }

        public RecordedActionType ActionType { get; }

        public EntityId ActorId { get; }

        public EntityId TargetId { get; }

        public GridCoord3 Origin { get; }

        public GridCoord3 Destination { get; }

        public static RecordedActionCommand Move(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
        {
            return new RecordedActionCommand(RecordedActionType.Move, actorId, default, origin, destination);
        }

        public static RecordedActionCommand BasicAttack(EntityId attackerId, EntityId targetId)
        {
            return new RecordedActionCommand(RecordedActionType.BasicAttack, attackerId, targetId, default, default);
        }
    }

    public enum RecordedActionType
    {
        Move = 0,
        BasicAttack = 1,
    }
}
