using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Infrastructure.Replay
{
    /// <summary>
    /// Enveloppe complète d'un enregistrement de replay.
    /// </summary>
    public sealed class BattleReplayRecord
    {
        public BattleReplayRecord(
            int seed,
            BattleStateSnapshot initialSnapshot,
            IReadOnlyList<RecordedActionCommand> actions,
            IReadOnlyList<BattleStateSnapshot> snapshots)
        {
            Seed = seed;
            InitialSnapshot = initialSnapshot;
            Actions = actions;
            Snapshots = snapshots;
        }

        public int Seed { get; }

        public BattleStateSnapshot InitialSnapshot { get; }

        public IReadOnlyList<RecordedActionCommand> Actions { get; }

        public IReadOnlyList<BattleStateSnapshot> Snapshots { get; }
    }
}
