using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public readonly struct BattleStateSnapshot
    {
        public const int CurrentContractVersion = 1;

        public BattleStateSnapshot(
            int tick,
            EntityPositionSnapshot[] entityPositions,
            EntityHitPointSnapshot[] entityHitPoints,
            EntityMovementPointSnapshot[] entityMovementPoints = null,
            EntitySkillResourceSnapshot[] entitySkillResources = null,
            SkillCooldownSnapshot[] skillCooldowns = null,
            StatusEffectSnapshot[] statusEffects = null,
            EntityRpgStatsSnapshot[] entityRpgStats = null,
            int contractVersion = CurrentContractVersion)
        {
            Tick = tick;
            EntityPositions = entityPositions;
            EntityHitPoints = entityHitPoints;
            EntityMovementPoints = entityMovementPoints ?? new EntityMovementPointSnapshot[0];
            EntitySkillResources = entitySkillResources ?? new EntitySkillResourceSnapshot[0];
            SkillCooldowns = skillCooldowns ?? new SkillCooldownSnapshot[0];
            StatusEffects = statusEffects ?? new StatusEffectSnapshot[0];
            EntityRpgStats = entityRpgStats ?? new EntityRpgStatsSnapshot[0];
            ContractVersion = contractVersion <= 0 ? CurrentContractVersion : contractVersion;
        }

        public int Tick { get; }

        public int ContractVersion { get; }

        public IReadOnlyList<EntityPositionSnapshot> EntityPositions { get; }

        public IReadOnlyList<EntityHitPointSnapshot> EntityHitPoints { get; }

        public IReadOnlyList<EntityMovementPointSnapshot> EntityMovementPoints { get; }

        public IReadOnlyList<EntitySkillResourceSnapshot> EntitySkillResources { get; }

        public IReadOnlyList<SkillCooldownSnapshot> SkillCooldowns { get; }

        public IReadOnlyList<StatusEffectSnapshot> StatusEffects { get; }

        public IReadOnlyList<EntityRpgStatsSnapshot> EntityRpgStats { get; }
    }
}
