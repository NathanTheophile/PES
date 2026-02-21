using System;
using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public sealed class StatusEffectStateStore
    {
        private readonly Dictionary<StatusEffectKey, StatusEffectState> _statusEffects = new();

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

        public int GetStatusEffectPotency(EntityId entityId, StatusEffectType effectType)
        {
            var key = new StatusEffectKey(entityId, effectType);
            return _statusEffects.TryGetValue(key, out var state) ? state.Potency : 0;
        }

        public int TickStatusEffects(EntityId entityId, StatusEffectTickMoment tickMoment, int turns, Func<EntityId, int, bool> tryApplyDamage)
        {
            var safeTurns = turns < 0 ? 0 : turns;
            if (safeTurns == 0 || _statusEffects.Count == 0)
            {
                return 0;
            }

            var keys = new List<StatusEffectKey>();
            foreach (var pair in _statusEffects)
            {
                if (pair.Key.EntityId.Equals(entityId))
                {
                    keys.Add(pair.Key);
                }
            }

            var totalPeriodicDamage = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var state = _statusEffects[key];

                if (state.TickMoment == tickMoment &&
                    key.EffectType == StatusEffectType.Poison &&
                    state.Potency > 0 &&
                    tryApplyDamage(entityId, state.Potency))
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

        public StatusEffectSnapshot[] CreateSnapshots()
        {
            var entries = new List<KeyValuePair<StatusEffectKey, StatusEffectState>>(_statusEffects);
            entries.Sort((a, b) =>
            {
                var entityCompare = a.Key.EntityId.Value.CompareTo(b.Key.EntityId.Value);
                if (entityCompare != 0)
                {
                    return entityCompare;
                }

                var typeCompare = ((int)a.Key.EffectType).CompareTo((int)b.Key.EffectType);
                if (typeCompare != 0)
                {
                    return typeCompare;
                }

                return ((int)a.Value.TickMoment).CompareTo((int)b.Value.TickMoment);
            });

            var snapshots = new StatusEffectSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                snapshots[i] = new StatusEffectSnapshot(pair.Key.EntityId, pair.Key.EffectType, pair.Value.RemainingTurns, pair.Value.Potency, pair.Value.TickMoment);
            }

            return snapshots;
        }

        public void ApplySnapshots(IReadOnlyList<StatusEffectSnapshot> snapshots)
        {
            _statusEffects.Clear();
            for (var i = 0; i < snapshots.Count; i++)
            {
                var row = snapshots[i];
                _statusEffects[new StatusEffectKey(row.EntityId, row.EffectType)] = new StatusEffectState(row.RemainingTurns, row.Potency, row.TickMoment);
            }
        }
    }
}
