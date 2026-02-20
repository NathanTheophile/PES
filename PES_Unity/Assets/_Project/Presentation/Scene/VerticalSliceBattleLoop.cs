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

        private bool _unitAHasMovedOnce;
        private int? _winnerTeamId;

        public VerticalSliceBattleLoop(
            int seed = 7,
            float turnDurationSeconds = 10f,
            MoveActionPolicy? movePolicyOverride = null,
            BasicAttackActionPolicy? basicAttackPolicyOverride = null)
        {
            State = new BattleState();
            _resolver = new ActionResolver(new SeededRngService(seed));
            _turnController = new RoundRobinTurnController(new[] { UnitA, UnitB }, actionsPerTurn: 1);
            TurnDurationSeconds = turnDurationSeconds > 0f ? turnDurationSeconds : 10f;
            RemainingTurnSeconds = TurnDurationSeconds;
            _movePolicyOverride = movePolicyOverride;
            _basicAttackPolicyOverride = basicAttackPolicyOverride;

            State.SetEntityPosition(UnitA, new Position3(0, 0, 0));
            State.SetEntityPosition(UnitB, new Position3(2, 0, 1));
            State.SetEntityHitPoints(UnitA, 40);
            State.SetEntityHitPoints(UnitB, 40);
        }

        public BattleState State { get; }

        public int CurrentRound => _turnController.Round;

        public int RemainingActions => _turnController.RemainingActions;

        public bool IsBattleOver => _winnerTeamId.HasValue;

        public int? WinnerTeamId => _winnerTeamId;

        public float TurnDurationSeconds { get; }

        public float RemainingTurnSeconds { get; private set; }

        public EntityId CurrentActorId => _turnController.CurrentActorId;


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

            // Une action rejetée ne consomme pas le tour ; l'acteur peut corriger sa commande.
            if (result.Code != ActionResolutionCode.Rejected)
            {
                _turnController.TryConsumeAction(actorId);
                if (_turnController.RemainingActions <= 0)
                {
                    EndCurrentTurn();
                }
            }

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
            RemainingTurnSeconds = TurnDurationSeconds;
        }

        private void EvaluateVictory()
        {
            if (!State.TryGetEntityHitPoints(UnitA, out var hpA) || !State.TryGetEntityHitPoints(UnitB, out var hpB))
            {
                return;
            }

            if (hpA <= 0 && hpB <= 0)
            {
                _winnerTeamId = 0;
                return;
            }

            if (hpA <= 0)
            {
                _winnerTeamId = TeamB;
                return;
            }

            if (hpB <= 0)
            {
                _winnerTeamId = TeamA;
            }
        }
    }
}
