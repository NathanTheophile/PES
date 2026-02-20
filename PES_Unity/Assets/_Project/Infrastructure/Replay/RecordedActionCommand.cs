using PES.Combat.Actions;
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
            SkillActionPolicy skillPolicy)
        {
            ActionType = actionType;
            ActorId = actorId;
            TargetId = targetId;
            Origin = origin;
            Destination = destination;
            SkillPolicy = skillPolicy;
        }

        public RecordedActionType ActionType { get; }

        public EntityId ActorId { get; }

        public EntityId TargetId { get; }

        public GridCoord3 Origin { get; }

        public GridCoord3 Destination { get; }

        public SkillActionPolicy SkillPolicy { get; }

        public static RecordedActionCommand Move(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
        {
            return new RecordedActionCommand(
                RecordedActionType.Move,
                actorId,
                default,
                origin,
                destination,
                default);
        }

        public static RecordedActionCommand BasicAttack(EntityId attackerId, EntityId targetId)
        {
            return new RecordedActionCommand(
                RecordedActionType.BasicAttack,
                attackerId,
                targetId,
                default,
                default,
                default);
        }

        /// <summary>
        /// Enregistre une compétence avec sa policy complète pour garantir la fidélité replay.
        /// </summary>
        public static RecordedActionCommand CastSkill(EntityId casterId, EntityId targetId, SkillActionPolicy skillPolicy)
        {
            return new RecordedActionCommand(
                RecordedActionType.CastSkill,
                casterId,
                targetId,
                default,
                default,
                skillPolicy);
        }

        /// <summary>
        /// Compat rétro: conserve la surcharge historique (skillId seul) avec policy par défaut.
        /// </summary>
        public static RecordedActionCommand CastSkill(EntityId casterId, EntityId targetId, int skillId)
        {
            var fallbackPolicy = new SkillActionPolicy(
                skillId: skillId,
                minRange: 1,
                maxRange: 3,
                baseDamage: 8,
                baseHitChance: 85,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1);

            return CastSkill(casterId, targetId, fallbackPolicy);
        }
    }

    public enum RecordedActionType
    {
        Move = 0,
        BasicAttack = 1,
        CastSkill = 2,
    }
}
