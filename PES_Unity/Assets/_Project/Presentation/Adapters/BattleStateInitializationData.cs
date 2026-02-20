using System;
using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Presentation.Adapters
{
    public readonly struct EntityInitializationData
    {
        public EntityInitializationData(
            EntityId entityId,
            Position3 spawnPosition,
            int hitPoints,
            int movementPoints,
            int skillResource,
            IReadOnlyList<string> tags)
        {
            EntityId = entityId;
            SpawnPosition = spawnPosition;
            HitPoints = hitPoints;
            MovementPoints = movementPoints;
            SkillResource = skillResource;
            Tags = tags ?? Array.Empty<string>();
        }

        public EntityId EntityId { get; }

        public Position3 SpawnPosition { get; }

        public int HitPoints { get; }

        public int MovementPoints { get; }

        public int SkillResource { get; }

        public IReadOnlyList<string> Tags { get; }
    }

    public readonly struct BattleStateInitializationData
    {
        public BattleStateInitializationData(IReadOnlyList<EntityInitializationData> entities)
        {
            Entities = entities ?? Array.Empty<EntityInitializationData>();
        }

        public IReadOnlyList<EntityInitializationData> Entities { get; }
    }
}
