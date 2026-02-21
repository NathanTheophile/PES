// Utilité : ce script contient l'état mémoire du combat utilisé par le pipeline de résolution.
// Il centralise les mutations déterministes (positions, points de vie, ressources, ticks, journal d'événements).
using System.Collections.Generic;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Conteneur d'état métier pour la simulation tactique.
    /// Cette classe n'utilise volontairement aucune API Unity afin de rester testable et portable.
    /// </summary>
    public sealed class BattleState
    {
        private readonly Dictionary<EntityId, Position3> _entityPositions = new();
        private readonly Dictionary<EntityId, int> _entityHitPoints = new();

        // PM persistants par entité (pool par tour).
        private readonly Dictionary<EntityId, int> _entityMaxMovementPoints = new();
        private readonly Dictionary<EntityId, int> _entityCurrentMovementPoints = new();

        private readonly Dictionary<EntityId, int> _entitySkillResources = new();
        private readonly Dictionary<SkillCooldownKey, int> _skillCooldowns = new();
        private readonly Dictionary<StatusEffectKey, StatusEffectState> _statusEffects = new();

        private readonly HashSet<Position3> _blockedPositions = new();
        private readonly Dictionary<Position3, int> _positionMovementCosts = new();
        private readonly List<string> _eventLog = new();
        private readonly CombatEventLog _combatEventLog = new();

        public IReadOnlyList<string> EventLog => _eventLog;

        public IReadOnlyList<CombatEventRecord> StructuredEventLog => _combatEventLog.Entries;

        public int Tick { get; private set; }

        public void AdvanceTick()
        {
            Tick++;
        }

        public void AddEvent(string evt)
        {
            _eventLog.Add(evt);
        }

        public void AddEvent(CombatEventRecord record)
        {
            _combatEventLog.Add(record);
            _eventLog.Add(record.Description);
        }

        public void SetEntityPosition(EntityId entityId, Position3 position)
        {
            _entityPositions[entityId] = position;
        }

        public bool TryGetEntityPosition(EntityId entityId, out Position3 position)
        {
            return _entityPositions.TryGetValue(entityId, out position);
        }

        public void SetEntityHitPoints(EntityId entityId, int hitPoints)
        {
            _entityHitPoints[entityId] = hitPoints;
        }

        public bool TryGetEntityHitPoints(EntityId entityId, out int hitPoints)
        {
            return _entityHitPoints.TryGetValue(entityId, out hitPoints);
        }

        public bool TryApplyDamage(EntityId entityId, int damage)
        {
            if (!_entityHitPoints.TryGetValue(entityId, out var hp))
            {
                return false;
            }

            var safeDamage = damage < 0 ? 0 : damage;
            var amplifiedDamage = ApplyIncomingDamageModifiers(entityId, safeDamage);
            var next = hp - amplifiedDamage;
            _entityHitPoints[entityId] = next < 0 ? 0 : next;
            return true;
        }


        private int ApplyIncomingDamageModifiers(EntityId entityId, int baseDamage)
        {
            if (baseDamage <= 0)
            {
                return 0;
            }

            var vulnerableTurns = GetStatusEffectRemaining(entityId, StatusEffectType.Vulnerable);
            if (vulnerableTurns <= 0)
            {
                return baseDamage;
            }

            var key = new StatusEffectKey(entityId, StatusEffectType.Vulnerable);
            if (!_statusEffects.TryGetValue(key, out var vulnerableState) || vulnerableState.Potency <= 0)
            {
                return baseDamage;
            }

            var amplified = baseDamage + ((baseDamage * vulnerableState.Potency) / 100);
            return amplified < 0 ? 0 : amplified;
        }

        /// <summary>
        /// Définit les PM max/courants d'une entité (courants = max par défaut).
        /// </summary>
        public void SetEntityMovementPoints(EntityId entityId, int maxMovementPoints, int? currentMovementPoints = null)
        {
            var safeMax = maxMovementPoints < 0 ? 0 : maxMovementPoints;
            var safeCurrent = currentMovementPoints ?? safeMax;
            if (safeCurrent < 0)
            {
                safeCurrent = 0;
            }

            if (safeCurrent > safeMax)
            {
                safeCurrent = safeMax;
            }

            _entityMaxMovementPoints[entityId] = safeMax;
            _entityCurrentMovementPoints[entityId] = safeCurrent;
        }

        public bool TryGetEntityMovementPoints(EntityId entityId, out int movementPoints)
        {
            return _entityCurrentMovementPoints.TryGetValue(entityId, out movementPoints);
        }

        public bool TryGetEntityMaxMovementPoints(EntityId entityId, out int maxMovementPoints)
        {
            return _entityMaxMovementPoints.TryGetValue(entityId, out maxMovementPoints);
        }

        public bool TryConsumeMovementPoints(EntityId entityId, int movementCost)
        {
            if (!_entityCurrentMovementPoints.TryGetValue(entityId, out var current))
            {
                return false;
            }

            var safeCost = movementCost < 0 ? 0 : movementCost;
            if (safeCost > current)
            {
                return false;
            }

            _entityCurrentMovementPoints[entityId] = current - safeCost;
            return true;
        }

        public bool ResetMovementPoints(EntityId entityId)
        {
            if (!_entityMaxMovementPoints.TryGetValue(entityId, out var max))
            {
                return false;
            }

            _entityCurrentMovementPoints[entityId] = max;
            return true;
        }


        public void SetEntitySkillResource(EntityId entityId, int amount)
        {
            _entitySkillResources[entityId] = amount < 0 ? 0 : amount;
        }

        public bool TryGetEntitySkillResource(EntityId entityId, out int amount)
        {
            return _entitySkillResources.TryGetValue(entityId, out amount);
        }

        public bool TryConsumeEntitySkillResource(EntityId entityId, int cost)
        {
            var safeCost = cost < 0 ? 0 : cost;
            if (!_entitySkillResources.TryGetValue(entityId, out var current))
            {
                return safeCost == 0;
            }

            if (safeCost > current)
            {
                return false;
            }

            _entitySkillResources[entityId] = current - safeCost;
            return true;
        }

        public void TickDownSkillCooldowns(EntityId entityId, int turns = 1)
        {
            var safeTurns = turns < 0 ? 0 : turns;
            if (safeTurns == 0 || _skillCooldowns.Count == 0)
            {
                return;
            }

            var keys = new List<SkillCooldownKey>();
            foreach (var pair in _skillCooldowns)
            {
                if (!pair.Key.EntityId.Equals(entityId))
                {
                    continue;
                }

                keys.Add(pair.Key);
            }

            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var next = _skillCooldowns[key] - safeTurns;
                if (next <= 0)
                {
                    _skillCooldowns.Remove(key);
                    continue;
                }

                _skillCooldowns[key] = next;
            }
        }

        public void SetSkillCooldown(EntityId entityId, int skillId, int remainingTurns)
        {
            var key = new SkillCooldownKey(entityId, skillId);
            var safe = remainingTurns < 0 ? 0 : remainingTurns;
            if (safe == 0)
            {
                _skillCooldowns.Remove(key);
                return;
            }

            _skillCooldowns[key] = safe;
        }

        public int GetSkillCooldown(EntityId entityId, int skillId)
        {
            var key = new SkillCooldownKey(entityId, skillId);
            return _skillCooldowns.TryGetValue(key, out var remaining) ? remaining : 0;
        }


        public void SetStatusEffect(
            EntityId entityId,
            StatusEffectType effectType,
            int remainingTurns,
            int potency = 0,
            StatusEffectTickMoment tickMoment = StatusEffectTickMoment.TurnStart)
        {
            var key = new StatusEffectKey(entityId, effectType);
            var safeTurns = remainingTurns < 0 ? 0 : remainingTurns;
            if (safeTurns == 0)
            {
                _statusEffects.Remove(key);
                return;
            }

            var safePotency = potency < 0 ? 0 : potency;
            _statusEffects[key] = new StatusEffectState(safeTurns, safePotency, tickMoment);
        }

        public int GetStatusEffectRemaining(EntityId entityId, StatusEffectType effectType)
        {
            var key = new StatusEffectKey(entityId, effectType);
            return _statusEffects.TryGetValue(key, out var state) ? state.RemainingTurns : 0;
        }

        public int TickStatusEffects(EntityId entityId, StatusEffectTickMoment tickMoment, int turns = 1)
        {
            var safeTurns = turns < 0 ? 0 : turns;
            if (safeTurns == 0 || _statusEffects.Count == 0)
            {
                return 0;
            }

            var keys = new List<StatusEffectKey>();
            foreach (var pair in _statusEffects)
            {
                if (!pair.Key.EntityId.Equals(entityId))
                {
                    continue;
                }

                keys.Add(pair.Key);
            }

            var totalPeriodicDamage = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var state = _statusEffects[key];

                if (state.TickMoment == tickMoment &&
                    key.EffectType == StatusEffectType.Poison &&
                    state.Potency > 0 &&
                    TryApplyDamage(entityId, state.Potency))
                {
                    totalPeriodicDamage += state.Potency;
                }

                if (state.TickMoment != tickMoment)
                {
                    continue;
                }

                var nextTurns = state.RemainingTurns - safeTurns;
                if (nextTurns <= 0)
                {
                    _statusEffects.Remove(key);
                    continue;
                }

                _statusEffects[key] = new StatusEffectState(nextTurns, state.Potency, state.TickMoment);
            }

            return totalPeriodicDamage;
        }

        public bool TryMoveEntity(EntityId entityId, Position3 expectedOrigin, Position3 destination)
        {
            if (!_entityPositions.TryGetValue(entityId, out var current) || !current.Equals(expectedOrigin))
            {
                return false;
            }

            _entityPositions[entityId] = destination;
            return true;
        }

        public void SetBlockedPosition(Position3 position, bool blocked = true)
        {
            if (blocked)
            {
                _blockedPositions.Add(position);
                return;
            }

            _blockedPositions.Remove(position);
        }

        public bool IsPositionBlocked(Position3 position)
        {
            return _blockedPositions.Contains(position);
        }

        public bool IsPositionOccupied(Position3 position, EntityId? ignoredEntityId = null)
        {
            foreach (var pair in _entityPositions)
            {
                if (ignoredEntityId.HasValue && pair.Key.Equals(ignoredEntityId.Value))
                {
                    continue;
                }

                if (pair.Value.Equals(position))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetMovementCost(Position3 position, int movementCost)
        {
            _positionMovementCosts[position] = movementCost < 1 ? 1 : movementCost;
        }

        public int GetMovementCost(Position3 position)
        {
            return _positionMovementCosts.TryGetValue(position, out var movementCost) ? movementCost : 1;
        }

        public IEnumerable<Position3> GetBlockedPositions()
        {
            return _blockedPositions;
        }

        public IEnumerable<KeyValuePair<EntityId, Position3>> GetEntityPositions()
        {
            return _entityPositions;
        }

        public BattleStateSnapshot CreateSnapshot()
        {
            var positions = new EntityPositionSnapshot[_entityPositions.Count];
            var index = 0;
            foreach (var pair in _entityPositions)
            {
                positions[index++] = new EntityPositionSnapshot(pair.Key, pair.Value);
            }

            var hitPoints = new EntityHitPointSnapshot[_entityHitPoints.Count];
            index = 0;
            foreach (var pair in _entityHitPoints)
            {
                hitPoints[index++] = new EntityHitPointSnapshot(pair.Key, pair.Value);
            }

            var movementPoints = new EntityMovementPointSnapshot[_entityCurrentMovementPoints.Count];
            index = 0;
            foreach (var pair in _entityCurrentMovementPoints)
            {
                var max = _entityMaxMovementPoints.TryGetValue(pair.Key, out var value) ? value : pair.Value;
                movementPoints[index++] = new EntityMovementPointSnapshot(pair.Key, pair.Value, max);
            }

            var skillResources = new EntitySkillResourceSnapshot[_entitySkillResources.Count];
            index = 0;
            foreach (var pair in _entitySkillResources)
            {
                skillResources[index++] = new EntitySkillResourceSnapshot(pair.Key, pair.Value);
            }

            var skillCooldowns = new SkillCooldownSnapshot[_skillCooldowns.Count];
            index = 0;
            foreach (var pair in _skillCooldowns)
            {
                skillCooldowns[index++] = new SkillCooldownSnapshot(pair.Key.EntityId, pair.Key.SkillId, pair.Value);
            }

            var statusEffects = new StatusEffectSnapshot[_statusEffects.Count];
            index = 0;
            foreach (var pair in _statusEffects)
            {
                statusEffects[index++] = new StatusEffectSnapshot(pair.Key.EntityId, pair.Key.EffectType, pair.Value.RemainingTurns, pair.Value.Potency, pair.Value.TickMoment);
            }

            return new BattleStateSnapshot(Tick, positions, hitPoints, movementPoints, skillResources, skillCooldowns, statusEffects);
        }

        public void ApplySnapshot(BattleStateSnapshot snapshot)
        {
            _entityPositions.Clear();
            foreach (var row in snapshot.EntityPositions)
            {
                _entityPositions[row.EntityId] = row.Position;
            }

            _entityHitPoints.Clear();
            foreach (var row in snapshot.EntityHitPoints)
            {
                _entityHitPoints[row.EntityId] = row.HitPoints;
            }

            _entityCurrentMovementPoints.Clear();
            _entityMaxMovementPoints.Clear();
            foreach (var row in snapshot.EntityMovementPoints)
            {
                _entityCurrentMovementPoints[row.EntityId] = row.MovementPoints;
                _entityMaxMovementPoints[row.EntityId] = row.MaxMovementPoints;
            }

            _entitySkillResources.Clear();
            foreach (var row in snapshot.EntitySkillResources)
            {
                _entitySkillResources[row.EntityId] = row.Amount;
            }

            _skillCooldowns.Clear();
            foreach (var row in snapshot.SkillCooldowns)
            {
                _skillCooldowns[new SkillCooldownKey(row.EntityId, row.SkillId)] = row.RemainingTurns;
            }

            _statusEffects.Clear();
            foreach (var row in snapshot.StatusEffects)
            {
                _statusEffects[new StatusEffectKey(row.EntityId, row.EffectType)] = new StatusEffectState(row.RemainingTurns, row.Potency, row.TickMoment);
            }

            Tick = snapshot.Tick;
        }
    }

    public readonly struct BattleStateSnapshot
    {
        public BattleStateSnapshot(
            int tick,
            EntityPositionSnapshot[] entityPositions,
            EntityHitPointSnapshot[] entityHitPoints,
            EntityMovementPointSnapshot[] entityMovementPoints = null,
            EntitySkillResourceSnapshot[] entitySkillResources = null,
            SkillCooldownSnapshot[] skillCooldowns = null,
            StatusEffectSnapshot[] statusEffects = null)
        {
            Tick = tick;
            EntityPositions = entityPositions;
            EntityHitPoints = entityHitPoints;
            EntityMovementPoints = entityMovementPoints ?? new EntityMovementPointSnapshot[0];
            EntitySkillResources = entitySkillResources ?? new EntitySkillResourceSnapshot[0];
            SkillCooldowns = skillCooldowns ?? new SkillCooldownSnapshot[0];
            StatusEffects = statusEffects ?? new StatusEffectSnapshot[0];
        }

        public int Tick { get; }

        public IReadOnlyList<EntityPositionSnapshot> EntityPositions { get; }

        public IReadOnlyList<EntityHitPointSnapshot> EntityHitPoints { get; }

        public IReadOnlyList<EntityMovementPointSnapshot> EntityMovementPoints { get; }

        public IReadOnlyList<EntitySkillResourceSnapshot> EntitySkillResources { get; }

        public IReadOnlyList<SkillCooldownSnapshot> SkillCooldowns { get; }

        public IReadOnlyList<StatusEffectSnapshot> StatusEffects { get; }
    }

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
        Vulnerable = 2,
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

    public readonly struct Position3
    {
        public Position3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
