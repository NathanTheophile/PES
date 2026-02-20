using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Infrastructure.Replay
{
    /// <summary>
    /// Exécuteur de replay : rejoue un enregistrement sur un état vierge avec la même seed.
    /// </summary>
    public sealed class BattleReplayRunner
    {
        public BattleReplayRunResult Run(BattleReplayRecord record)
        {
            var state = new BattleState();
            state.ApplySnapshot(record.InitialSnapshot);

            var resolver = new ActionResolver(new SeededRngService(record.Seed));
            var snapshots = new List<BattleStateSnapshot>();
            var results = new List<ActionResolution>();

            foreach (var action in record.Actions)
            {
                var command = BuildCommand(action);
                var result = resolver.Resolve(state, command);
                results.Add(result);
                snapshots.Add(state.CreateSnapshot());
            }

            return new BattleReplayRunResult(state.CreateSnapshot(), snapshots, results, state.StructuredEventLog);
        }

        private static IActionCommand BuildCommand(RecordedActionCommand action)
        {
            return action.ActionType switch
            {
                RecordedActionType.Move => new MoveAction(action.ActorId, action.Origin, action.Destination),
                RecordedActionType.BasicAttack => new BasicAttackAction(action.ActorId, action.TargetId),
                RecordedActionType.CastSkill => new CastSkillAction(action.ActorId, action.TargetId, action.SkillPolicy),
                _ => new BasicAttackAction(action.ActorId, action.TargetId),
            };
        }
    }

    public sealed class BattleReplayRunResult
    {
        public BattleReplayRunResult(
            BattleStateSnapshot finalSnapshot,
            IReadOnlyList<BattleStateSnapshot> snapshots,
            IReadOnlyList<ActionResolution> actionResults,
            IReadOnlyList<CombatEventRecord> events)
        {
            FinalSnapshot = finalSnapshot;
            Snapshots = snapshots;
            ActionResults = actionResults;
            Events = events;
        }

        public BattleStateSnapshot FinalSnapshot { get; }

        public IReadOnlyList<BattleStateSnapshot> Snapshots { get; }

        public IReadOnlyList<ActionResolution> ActionResults { get; }

        public IReadOnlyList<CombatEventRecord> Events { get; }
    }
}
