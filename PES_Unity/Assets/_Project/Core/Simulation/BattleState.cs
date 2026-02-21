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
        private readonly SkillCooldownStore _skillCooldownStore = new();
        private readonly StatusEffectStateStore _statusEffectStore = new();
        private readonly CombatantStatsStore _combatantStatsStore = new();

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

        public void SetEntityRpgStats(EntityId entityId, CombatantRpgStats stats)
        {
            _combatantStatsStore.SetEntityRpgStats(entityId, stats);
        }

        public bool TryGetEntityRpgStats(EntityId entityId, out CombatantRpgStats stats)
        {
            return _combatantStatsStore.TryGetEntityRpgStats(entityId, out stats);
        }

        public CombatantRpgStats GetEntityRpgStatsOrEmpty(EntityId entityId)
        {
            return _combatantStatsStore.GetEntityRpgStatsOrEmpty(entityId);
        }

        public bool TryApplyEntityRpgDamage(EntityId attackerId, EntityId defenderId, int spellBaseDamage, int spellBaseCriticalChance, DamageElement element, bool forceCritical = false)
        {
            if (!TryGetEntityHitPoints(defenderId, out _))
            {
                return false;
            }

            var attackerStats = GetEntityRpgStatsOrEmpty(attackerId);
            var defenderStats = GetEntityRpgStatsOrEmpty(defenderId);
            var resolution = DamageFormulaCalculator.Resolve(attackerStats, defenderStats, spellBaseDamage, spellBaseCriticalChance, element, forceCritical);
            return TryApplyDamage(defenderId, resolution.FinalDamage);
        }

        public bool TryApplyDamage(EntityId entityId, int damage)
        {
            if (!_entityHitPoints.TryGetValue(entityId, out var hp))
            {
                return false;
            }

            var safeDamage = damage < 0 ? 0 : damage;
            var next = hp - safeDamage;
            _entityHitPoints[entityId] = next < 0 ? 0 : next;
            return true;
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
            _skillCooldownStore.TickDownSkillCooldowns(entityId, turns);
        }

        public void SetSkillCooldown(EntityId entityId, int skillId, int remainingTurns)
        {
            _skillCooldownStore.SetSkillCooldown(entityId, skillId, remainingTurns);
        }

        public int GetSkillCooldown(EntityId entityId, int skillId)
        {
            return _skillCooldownStore.GetSkillCooldown(entityId, skillId);
        }


        public void SetStatusEffect(
            EntityId entityId,
            StatusEffectType effectType,
            int remainingTurns,
            int potency = 0,
            StatusEffectTickMoment tickMoment = StatusEffectTickMoment.TurnStart)
        {
            _statusEffectStore.SetStatusEffect(entityId, effectType, remainingTurns, potency, tickMoment);
        }

        public int GetStatusEffectRemaining(EntityId entityId, StatusEffectType effectType)
        {
            return _statusEffectStore.GetStatusEffectRemaining(entityId, effectType);
        }

        public bool HasStatusEffect(EntityId entityId, StatusEffectType effectType)
        {
            return _statusEffectStore.HasStatusEffect(entityId, effectType);
        }

        public bool IsActionInterrupted(EntityId entityId)
        {
            return HasStatusEffect(entityId, StatusEffectType.Stunned);
        }

        public int TickStatusEffects(EntityId entityId, StatusEffectTickMoment tickMoment, int turns = 1)
        {
            return _statusEffectStore.TickStatusEffects(entityId, tickMoment, turns, TryApplyDamage);
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
            var positions = SnapshotSort.CreateEntityPositionSnapshots(_entityPositions);
            var hitPoints = SnapshotSort.CreateEntityHitPointSnapshots(_entityHitPoints);
            var movementPoints = SnapshotSort.CreateEntityMovementPointSnapshots(_entityCurrentMovementPoints, _entityMaxMovementPoints);
            var skillResources = SnapshotSort.CreateEntitySkillResourceSnapshots(_entitySkillResources);
            var skillCooldowns = _skillCooldownStore.CreateSnapshots();
            var statusEffects = _statusEffectStore.CreateSnapshots();
            var rpgStats = _combatantStatsStore.CreateSnapshots();

            return new BattleStateSnapshot(Tick, positions, hitPoints, movementPoints, skillResources, skillCooldowns, statusEffects, rpgStats);
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

            _skillCooldownStore.ApplySnapshots(snapshot.SkillCooldowns);
            _statusEffectStore.ApplySnapshots(snapshot.StatusEffects);
            _combatantStatsStore.ApplySnapshots(snapshot.EntityRpgStats);

            Tick = snapshot.Tick;
        }
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
