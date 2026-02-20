using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Core.TurnSystem
{
    public readonly struct BattleActorDefinition
    {
        public BattleActorDefinition(EntityId actorId, int teamId, Position3 startPosition, int startHitPoints, int startMovementPoints = 6)
        {
            ActorId = actorId;
            TeamId = teamId;
            StartPosition = startPosition;
            StartHitPoints = startHitPoints;
            StartMovementPoints = startMovementPoints < 0 ? 0 : startMovementPoints;
        }

        public EntityId ActorId { get; }

        public int TeamId { get; }

        public Position3 StartPosition { get; }

        public int StartHitPoints { get; }

        public int StartMovementPoints { get; }
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

    /// <summary>
    /// Contrat d'objectif de victoire/scénario évalué avant la règle d'élimination standard.
    /// </summary>
    public interface IBattleObjective
    {
        bool TryResolve(BattleState state, IReadOnlyDictionary<EntityId, int> teamByActor, out BattleOutcome outcome);
    }

    /// <summary>
    /// Objectif simple : une équipe gagne dès qu'au moins une unité vivante contrôle la case cible.
    /// Si plusieurs équipes contestent la case, l'objectif ne se résout pas.
    /// </summary>
    public sealed class ControlPointObjective : IBattleObjective
    {
        public ControlPointObjective(Position3 controlPoint)
        {
            ControlPoint = controlPoint;
        }

        public Position3 ControlPoint { get; }

        public bool TryResolve(BattleState state, IReadOnlyDictionary<EntityId, int> teamByActor, out BattleOutcome outcome)
        {
            outcome = default;

            var controllingTeamId = 0;
            foreach (var pair in teamByActor)
            {
                if (!state.TryGetEntityHitPoints(pair.Key, out var hp) || hp <= 0)
                {
                    continue;
                }

                if (!state.TryGetEntityPosition(pair.Key, out var position) || !position.Equals(ControlPoint))
                {
                    continue;
                }

                if (controllingTeamId == 0)
                {
                    controllingTeamId = pair.Value;
                    continue;
                }

                if (controllingTeamId != pair.Value)
                {
                    return false;
                }
            }

            if (controllingTeamId == 0)
            {
                return false;
            }

            outcome = new BattleOutcome(true, controllingTeamId);
            return true;
        }
    }

    public sealed class BattleOutcomeEvaluator
    {
        private readonly IReadOnlyList<IBattleObjective> _objectives;

        public BattleOutcomeEvaluator(IReadOnlyList<IBattleObjective> objectives = null)
        {
            _objectives = objectives;
        }

        public BattleOutcome Evaluate(BattleState state, IReadOnlyDictionary<EntityId, int> teamByActor)
        {
            if (_objectives != null)
            {
                for (var i = 0; i < _objectives.Count; i++)
                {
                    if (_objectives[i].TryResolve(state, teamByActor, out var objectiveOutcome))
                    {
                        return objectiveOutcome;
                    }
                }
            }

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
