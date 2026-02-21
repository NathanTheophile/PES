using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public sealed class SkillCooldownStore
    {
        private readonly Dictionary<SkillCooldownKey, int> _skillCooldowns = new();

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
                if (pair.Key.EntityId.Equals(entityId))
                {
                    keys.Add(pair.Key);
                }
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

        public SkillCooldownSnapshot[] CreateSnapshots()
        {
            var entries = new List<KeyValuePair<SkillCooldownKey, int>>(_skillCooldowns);
            entries.Sort((a, b) =>
            {
                var entityCompare = a.Key.EntityId.Value.CompareTo(b.Key.EntityId.Value);
                return entityCompare != 0 ? entityCompare : a.Key.SkillId.CompareTo(b.Key.SkillId);
            });

            var snapshots = new SkillCooldownSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                snapshots[i] = new SkillCooldownSnapshot(pair.Key.EntityId, pair.Key.SkillId, pair.Value);
            }

            return snapshots;
        }

        public void ApplySnapshots(IReadOnlyList<SkillCooldownSnapshot> snapshots)
        {
            _skillCooldowns.Clear();
            for (var i = 0; i < snapshots.Count; i++)
            {
                var row = snapshots[i];
                _skillCooldowns[new SkillCooldownKey(row.EntityId, row.SkillId)] = row.RemainingTurns;
            }
        }
    }
}
