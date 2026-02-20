using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Infrastructure.Replay
{
    /// <summary>
    /// Enregistreur in-memory d'actions + snapshots pour rejouer/valider une simulation.
    /// </summary>
    public sealed class BattleReplayRecorder
    {
        private readonly List<RecordedActionCommand> _actions = new();
        private readonly List<BattleStateSnapshot> _snapshots = new();

        private BattleStateSnapshot _initialSnapshot;

        public BattleReplayRecorder(int seed)
        {
            Seed = seed;
        }

        public int Seed { get; }

        public void CaptureInitialState(BattleState state)
        {
            _initialSnapshot = state.CreateSnapshot();
        }

        public void RecordAction(RecordedActionCommand action, BattleState state)
        {
            _actions.Add(action);
            _snapshots.Add(state.CreateSnapshot());
        }

        public BattleReplayRecord Build()
        {
            return new BattleReplayRecord(Seed, _initialSnapshot, _actions, _snapshots);
        }
    }
}
