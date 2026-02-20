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
            GridCoord3 destination,
            int skillId)
        {
            ActionType = actionType;
            ActorId = actorId;
            TargetId = targetId;
            Origin = origin;
            Destination = destination;
            SkillId = skillId;
        }

        public RecordedActionType ActionType { get; }

        public EntityId ActorId { get; }

        public EntityId TargetId { get; }

        public GridCoord3 Origin { get; }

        public GridCoord3 Destination { get; }

        public int SkillId { get; }

        public static RecordedActionCommand Move(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
        {
            return new RecordedActionCommand(RecordedActionType.Move, actorId, default, origin, destination, 0);
        }

        public static RecordedActionCommand BasicAttack(EntityId attackerId, EntityId targetId)
        {
            return new RecordedActionCommand(RecordedActionType.BasicAttack, attackerId, targetId, default, default, 0);
        }

        public static RecordedActionCommand CastSkill(EntityId casterId, EntityId targetId, int skillId)
        {
            return new RecordedActionCommand(RecordedActionType.CastSkill, casterId, targetId, default, default, skillId);
        }
    }

    public enum RecordedActionType
    {
        Move = 0,
        BasicAttack = 1,
        CastSkill = 2,
    }
}
