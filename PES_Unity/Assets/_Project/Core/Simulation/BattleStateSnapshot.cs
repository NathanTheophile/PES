using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public readonly struct BattleStateSnapshot
    {
        public BattleStateSnapshot(
            int tick,
            EntityPositionSnapshot[] entityPositions,
            EntityHitPointSnapshot[] entityHitPoints,
            EntityMovementPointSnapshot[] entityMovementPoints = null,
            EntitySkillResourceSnapshot[] entitySkillResources = null,
            SkillCooldownSnapshot[] skillCooldowns = null,
            StatusEffectSnapshot[] statusEffects = null,
            EntityRpgStatsSnapshot[] entityRpgStats = null)
        {
            Tick = tick;
            EntityPositions = entityPositions;
            EntityHitPoints = entityHitPoints;
            EntityMovementPoints = entityMovementPoints ?? new EntityMovementPointSnapshot[0];
            EntitySkillResources = entitySkillResources ?? new EntitySkillResourceSnapshot[0];
            SkillCooldowns = skillCooldowns ?? new SkillCooldownSnapshot[0];
            StatusEffects = statusEffects ?? new StatusEffectSnapshot[0];
            EntityRpgStats = entityRpgStats ?? new EntityRpgStatsSnapshot[0];
        }

        public int Tick { get; }

        public IReadOnlyList<EntityPositionSnapshot> EntityPositions { get; }

        public IReadOnlyList<EntityHitPointSnapshot> EntityHitPoints { get; }

        public IReadOnlyList<EntityMovementPointSnapshot> EntityMovementPoints { get; }

        public IReadOnlyList<EntitySkillResourceSnapshot> EntitySkillResources { get; }

        public IReadOnlyList<SkillCooldownSnapshot> SkillCooldowns { get; }

        public IReadOnlyList<StatusEffectSnapshot> StatusEffects { get; }

        public IReadOnlyList<EntityRpgStatsSnapshot> EntityRpgStats { get; }
    }
}
