using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Core.TurnSystem
{
    public readonly struct BattleActorDefinition
    {
        public BattleActorDefinition(EntityId actorId, int teamId, Position3 startPosition, int startHitPoints)
        {
            ActorId = actorId;
            TeamId = teamId;
            StartPosition = startPosition;
            StartHitPoints = startHitPoints;
        }

        public EntityId ActorId { get; }

        public int TeamId { get; }

        public Position3 StartPosition { get; }

        public int StartHitPoints { get; }
    }

    public readonly struct BattleOutcome
    {
        public BattleOutcome(bool isBattleOver, int? winnerTeamId)
        {
            IsBattleOver = isBattleOver;
            WinnerTeamId = winnerTeamId;
        }

        public bool IsBattleOver { get; }

        public int? WinnerTeamId { get; }
    }

    public sealed class BattleOutcomeEvaluator
    {
        public BattleOutcome Evaluate(BattleState state, IReadOnlyDictionary<EntityId, int> teamByActor)
        {
            var aliveTeams = new HashSet<int>();

            foreach (var pair in teamByActor)
            {
                if (!state.TryGetEntityHitPoints(pair.Key, out var hp) || hp <= 0)
                {
                    continue;
                }

                aliveTeams.Add(pair.Value);
                if (aliveTeams.Count > 1)
                {
                    return new BattleOutcome(false, null);
                }
            }

            if (aliveTeams.Count == 0)
            {
                return new BattleOutcome(true, 0);
            }

            foreach (var teamId in aliveTeams)
            {
                return new BattleOutcome(true, teamId);
            }

            return new BattleOutcome(false, null);
        }
    }
}
