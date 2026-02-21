using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public sealed class CombatantStatsStore
    {
        private readonly Dictionary<EntityId, CombatantRpgStats> _entityRpgStats = new();

        public void SetEntityRpgStats(EntityId entityId, CombatantRpgStats stats)
        {
            _entityRpgStats[entityId] = stats;
        }

        public bool TryGetEntityRpgStats(EntityId entityId, out CombatantRpgStats stats)
        {
            return _entityRpgStats.TryGetValue(entityId, out stats);
        }

        public CombatantRpgStats GetEntityRpgStatsOrEmpty(EntityId entityId)
        {
            return _entityRpgStats.TryGetValue(entityId, out var stats) ? stats : CombatantRpgStats.Empty;
        }

        public EntityRpgStatsSnapshot[] CreateSnapshots()
        {
            var entries = new List<KeyValuePair<EntityId, CombatantRpgStats>>(_entityRpgStats);
            entries.Sort((a, b) => a.Key.Value.CompareTo(b.Key.Value));

            var snapshots = new EntityRpgStatsSnapshot[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var pair = entries[i];
                snapshots[i] = new EntityRpgStatsSnapshot(pair.Key, pair.Value);
            }

            return snapshots;
        }

        public void ApplySnapshots(IReadOnlyList<EntityRpgStatsSnapshot> snapshots)
        {
            _entityRpgStats.Clear();
            for (var i = 0; i < snapshots.Count; i++)
            {
                var row = snapshots[i];
                _entityRpgStats[row.EntityId] = row.Stats;
            }
        }
    }
}
