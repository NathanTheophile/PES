using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Core.TurnSystem;
using PES.Grid.Grid3D;

namespace PES.Presentation.Scene
{
    /// <summary>
    /// Orchestrateur de démo : initiative round-robin + consommation d'action + condition de victoire minimale.
    /// </summary>
    public sealed class VerticalSliceBattleLoop
    {
        public static readonly EntityId UnitA = new(100);
        public static readonly EntityId UnitB = new(101);

        private const int TeamA = 1;
        private const int TeamB = 2;

        private readonly ActionResolver _resolver;
        private readonly RoundRobinTurnController _turnController;
        private readonly MoveActionPolicy? _movePolicyOverride;
        private readonly BasicAttackActionPolicy? _basicAttackPolicyOverride;
        private readonly Dictionary<EntityId, int> _teamByActor;
        private readonly BattleOutcomeEvaluator _battleOutcomeEvaluator;

        private bool _unitAHasMovedOnce;
        private int? _winnerTeamId;

        public VerticalSliceBattleLoop(
            int seed = 7,
            float turnDurationSeconds = 30f,
            MoveActionPolicy? movePolicyOverride = null,
            BasicAttackActionPolicy? basicAttackPolicyOverride = null,
            int actionsPerTurn = 1,
            IReadOnlyList<BattleActorDefinition> actorDefinitions = null,
            IReadOnlyList<IBattleObjective> objectives = null)
        {
            var definitions = actorDefinitions ?? CreateDefaultActorDefinitions();

            State = new BattleState();
            _resolver = new ActionResolver(new SeededRngService(seed));
            _battleOutcomeEvaluator = new BattleOutcomeEvaluator(objectives);
            _movePolicyOverride = movePolicyOverride;
            _basicAttackPolicyOverride = basicAttackPolicyOverride;
            _teamByActor = new Dictionary<EntityId, int>(definitions.Count);

            var turnOrder = new EntityId[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                var actor = definitions[i];
                turnOrder[i] = actor.ActorId;
                _teamByActor[actor.ActorId] = actor.TeamId;
                State.SetEntityPosition(actor.ActorId, actor.StartPosition);
                State.SetEntityHitPoints(actor.ActorId, actor.StartHitPoints);
                State.SetEntityMovementPoints(actor.ActorId, actor.StartMovementPoints);
            }

            _turnController = new RoundRobinTurnController(turnOrder, actionsPerTurn);
            TurnDurationSeconds = turnDurationSeconds > 0f ? turnDurationSeconds : 30f;
            RemainingTurnSeconds = TurnDurationSeconds;
        }

        public BattleState State { get; }

        public int CurrentRound => _turnController.Round;

        public int RemainingActions => _turnController.RemainingActions;

        public bool IsBattleOver => _winnerTeamId.HasValue;

        public int? WinnerTeamId => _winnerTeamId;

        public float TurnDurationSeconds { get; }

        public float RemainingTurnSeconds { get; private set; }

        public EntityId CurrentActorId => _turnController.CurrentActorId;

        public int CurrentActorMovementPoints
        {
            get
            {
                return State.TryGetEntityMovementPoints(CurrentActorId, out var value) ? value : -1;
            }
        }


        public bool TryAdvanceTurnTimer(float deltaTime, out ActionResolution timeoutResult)
        {
            timeoutResult = default;

            if (IsBattleOver || deltaTime <= 0f)
            {
                return false;
            }

            RemainingTurnSeconds -= deltaTime;
            if (RemainingTurnSeconds > 0f)
            {
                return false;
            }

            EndCurrentTurn();
            timeoutResult = new ActionResolution(false, ActionResolutionCode.Rejected, "TurnTimedOut: next actor", ActionFailureReason.TurnTimedOut);
            return true;
        }


        public bool TryPassTurn(EntityId actorId, out ActionResolution result)
        {
            if (IsBattleOver)
            {
                result = new ActionResolution(false, ActionResolutionCode.Rejected, "BattleFinished: no further actions", ActionFailureReason.InvalidTargeting);
                return false;
            }

            if (!actorId.Equals(CurrentActorId))
            {
                result = new ActionResolution(false, ActionResolutionCode.Rejected, $"TurnRejected: it's {PeekCurrentActorLabel()} turn", ActionFailureReason.InvalidOrigin);
                return false;
            }

            var previousActor = CurrentActorId;
            EndCurrentTurn();
            result = new ActionResolution(true, ActionResolutionCode.Succeeded, $"TurnPassed: {previousActor} -> {CurrentActorId}");
            State.AddEvent(new CombatEventRecord(State.Tick, result.Code, result.FailureReason, result.Description, result.Payload));
            State.AdvanceTick();
            return true;
        }

        public string PeekCurrentActorLabel()
        {
            return CurrentActorId.Equals(UnitA) ? "UnitA" : "UnitB";
        }

        public string PeekNextStepLabel()
        {
            if (IsBattleOver)
            {
                return "BattleFinished";
            }

            var actor = CurrentActorId;
            if (actor.Equals(UnitA))
            {
                return _unitAHasMovedOnce ? "Attack(UnitA->UnitB)" : "Move(UnitA)";
            }

            return "Attack(UnitB->UnitA)";
        }

        /// <summary>
        /// Exécute une commande explicite venant de la couche input/presentation.
        /// </summary>
        public bool TryExecutePlannedCommand(EntityId actorId, IActionCommand command, out ActionResolution result)
        {
            if (IsBattleOver)
            {
                result = new ActionResolution(false, ActionResolutionCode.Rejected, "BattleFinished: no further actions", ActionFailureReason.InvalidTargeting);
                return false;
            }

            if (!actorId.Equals(CurrentActorId))
            {
                result = new ActionResolution(false, ActionResolutionCode.Rejected, $"TurnRejected: it's {PeekCurrentActorLabel()} turn", ActionFailureReason.InvalidOrigin);
                return false;
            }

            if (_turnController.RemainingActions <= 0)
            {
                result = new ActionResolution(false, ActionResolutionCode.Rejected, "TurnRejected: no action points remaining", ActionFailureReason.InvalidOrigin);
                return false;
            }

            result = _resolver.Resolve(State, command);

            if (result.Code != ActionResolutionCode.Rejected)
            {
                _turnController.TryConsumeAction(actorId);
                if (_turnController.RemainingActions <= 0)
                {
                    EndCurrentTurn();
                }
            }

            SyncAliveActorsWithHitPoints();
            EvaluateVictory();
            return true;
        }

        /// <summary>
        /// Exécute l'action scriptée de démo pour conserver le vertical slice pilotable au clavier.
        /// </summary>
        public ActionResolution ExecuteNextStep()
        {
            var actor = CurrentActorId;
            IActionCommand command;

            if (actor.Equals(UnitA))
            {
                if (!_unitAHasMovedOnce)
                {
                    State.TryGetEntityPosition(UnitA, out var unitAPosition);
                    var moveOrigin = new GridCoord3(unitAPosition.X, unitAPosition.Y, unitAPosition.Z);
                    var moveDestination = moveOrigin.X == 0
                        ? new GridCoord3(1, 0, 1)
                        : new GridCoord3(0, 0, 0);

                    command = new MoveAction(UnitA, moveOrigin, moveDestination, _movePolicyOverride);
                    _unitAHasMovedOnce = true;
                }
                else
                {
                    command = new BasicAttackAction(UnitA, UnitB, _basicAttackPolicyOverride);
                }
            }
            else
            {
                command = new BasicAttackAction(UnitB, UnitA, _basicAttackPolicyOverride);
            }

            TryExecutePlannedCommand(actor, command, out var result);
            return result;
        }


        private void EndCurrentTurn()
        {
            _turnController.EndTurn();
            ResetCurrentActorMovementPoints();
            RemainingTurnSeconds = TurnDurationSeconds;
        }


        private void ResetCurrentActorMovementPoints()
        {
            State.ResetMovementPoints(CurrentActorId);
        }

        private void EvaluateVictory()
        {
            var outcome = _battleOutcomeEvaluator.Evaluate(State, _teamByActor);
            if (!outcome.IsBattleOver)
            {
                return;
            }

            _winnerTeamId = outcome.WinnerTeamId;
        }

        private void SyncAliveActorsWithHitPoints()
        {
            foreach (var pair in _teamByActor)
            {
                var isAlive = State.TryGetEntityHitPoints(pair.Key, out var hp) && hp > 0;
                _turnController.SetActorActive(pair.Key, isAlive);
            }
        }

        private static IReadOnlyList<BattleActorDefinition> CreateDefaultActorDefinitions()
        {
            return new[]
            {
                new BattleActorDefinition(UnitA, TeamA, new Position3(0, 0, 0), 40),
                new BattleActorDefinition(UnitB, TeamB, new Position3(2, 0, 1), 40),
            };
        }
    }
}
