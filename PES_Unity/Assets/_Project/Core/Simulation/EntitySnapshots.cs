using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public readonly struct EntityPositionSnapshot
    {
        public EntityPositionSnapshot(EntityId entityId, Position3 position)
        {
            EntityId = entityId;
            Position = position;
        }

        public EntityId EntityId { get; }

        public Position3 Position { get; }
    }

    public readonly struct EntityHitPointSnapshot
    {
        public EntityHitPointSnapshot(EntityId entityId, int hitPoints)
        {
            EntityId = entityId;
            HitPoints = hitPoints;
        }

        public EntityId EntityId { get; }

        public int HitPoints { get; }
    }

    public readonly struct EntityMovementPointSnapshot
    {
        public EntityMovementPointSnapshot(EntityId entityId, int movementPoints, int maxMovementPoints)
        {
            EntityId = entityId;
            MovementPoints = movementPoints;
            MaxMovementPoints = maxMovementPoints;
        }

        public EntityId EntityId { get; }

        public int MovementPoints { get; }

        public int MaxMovementPoints { get; }
    }

    public readonly struct EntitySkillResourceSnapshot
    {
        public EntitySkillResourceSnapshot(EntityId entityId, int amount)
        {
            EntityId = entityId;
            Amount = amount;
        }

        public EntityId EntityId { get; }

        public int Amount { get; }
    }

    public readonly struct SkillCooldownSnapshot
    {
        public SkillCooldownSnapshot(EntityId entityId, int skillId, int remainingTurns)
        {
            EntityId = entityId;
            SkillId = skillId;
            RemainingTurns = remainingTurns;
        }

        public EntityId EntityId { get; }

        public int SkillId { get; }

        public int RemainingTurns { get; }
    }

    public readonly struct SkillCooldownKey
    {
        public SkillCooldownKey(EntityId entityId, int skillId)
        {
            EntityId = entityId;
            SkillId = skillId;
        }

        public EntityId EntityId { get; }

        public int SkillId { get; }
    }

    public enum StatusEffectType
    {
        None = 0,
        Poison = 1,
    }

    public enum StatusEffectTickMoment
    {
        TurnStart = 0,
        TurnEnd = 1,
    }

    public readonly struct StatusEffectSnapshot
    {
        public StatusEffectSnapshot(EntityId entityId, StatusEffectType effectType, int remainingTurns, int potency, StatusEffectTickMoment tickMoment)
        {
            EntityId = entityId;
            EffectType = effectType;
            RemainingTurns = remainingTurns;
            Potency = potency;
            TickMoment = tickMoment;
        }

        public EntityId EntityId { get; }

        public StatusEffectType EffectType { get; }

        public int RemainingTurns { get; }

        public int Potency { get; }

        public StatusEffectTickMoment TickMoment { get; }
    }

    public readonly struct StatusEffectKey
    {
        public StatusEffectKey(EntityId entityId, StatusEffectType effectType)
        {
            EntityId = entityId;
            EffectType = effectType;
        }

        public EntityId EntityId { get; }

        public StatusEffectType EffectType { get; }
    }

    public readonly struct StatusEffectState
    {
        public StatusEffectState(int remainingTurns, int potency, StatusEffectTickMoment tickMoment)
        {
            RemainingTurns = remainingTurns;
            Potency = potency;
            TickMoment = tickMoment;
        }

        public int RemainingTurns { get; }

        public int Potency { get; }

        public StatusEffectTickMoment TickMoment { get; }
    }

    public readonly struct EntityRpgStatsSnapshot
    {
        public EntityRpgStatsSnapshot(EntityId entityId, CombatantRpgStats stats)
        {
            EntityId = entityId;
            Stats = stats;
        }

        public EntityId EntityId { get; }

        public CombatantRpgStats Stats { get; }
    }

    internal static class SnapshotSort
    {
        public static EntityPositionSnapshot[] CreateEntityPositionSnapshots(Dictionary<EntityId, Position3> source)
        {
            var entries = new List<KeyValuePair<EntityId, Position3>>(source);
            entries.Sort((a, b) => a.Key.Value.CompareTo(b.Key.Value));

            var snapshots = new EntityPositionSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                snapshots[i] = new EntityPositionSnapshot(pair.Key, pair.Value);
            }

            return snapshots;
        }

        public static EntityHitPointSnapshot[] CreateEntityHitPointSnapshots(Dictionary<EntityId, int> source)
        {
            var entries = new List<KeyValuePair<EntityId, int>>(source);
            entries.Sort((a, b) => a.Key.Value.CompareTo(b.Key.Value));

            var snapshots = new EntityHitPointSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                snapshots[i] = new EntityHitPointSnapshot(pair.Key, pair.Value);
            }

            return snapshots;
        }

        public static EntityMovementPointSnapshot[] CreateEntityMovementPointSnapshots(Dictionary<EntityId, int> current, Dictionary<EntityId, int> max)
        {
            var entries = new List<KeyValuePair<EntityId, int>>(current);
            entries.Sort((a, b) => a.Key.Value.CompareTo(b.Key.Value));

            var snapshots = new EntityMovementPointSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                var maxMovementPoints = max.TryGetValue(pair.Key, out var value) ? value : pair.Value;
                snapshots[i] = new EntityMovementPointSnapshot(pair.Key, pair.Value, maxMovementPoints);
            }

            return snapshots;
        }

        public static EntitySkillResourceSnapshot[] CreateEntitySkillResourceSnapshots(Dictionary<EntityId, int> source)
        {
            var entries = new List<KeyValuePair<EntityId, int>>(source);
            entries.Sort((a, b) => a.Key.Value.CompareTo(b.Key.Value));

            var snapshots = new EntitySkillResourceSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                snapshots[i] = new EntitySkillResourceSnapshot(pair.Key, pair.Value);
            }

            return snapshots;
        }
    }
}
