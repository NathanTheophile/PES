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

        private bool _unitAHasMovedOnce;
        private int? _winnerTeamId;

        public VerticalSliceBattleLoop(int seed = 7)
        {
            State = new BattleState();
            _resolver = new ActionResolver(new SeededRngService(seed));
            _turnController = new RoundRobinTurnController(new[] { UnitA, UnitB }, actionsPerTurn: 1);

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

        public string PeekCurrentActorLabel()
        {
            var actor = _turnController.CurrentActorId;
            return actor.Equals(UnitA) ? "UnitA" : "UnitB";
        }

        public string PeekNextStepLabel()
        {
            if (IsBattleOver)
            {
                return "BattleFinished";
            }

            var actor = _turnController.CurrentActorId;
            if (actor.Equals(UnitA))
            {
                return _unitAHasMovedOnce ? "Attack(UnitA->UnitB)" : "Move(UnitA)";
            }

            return "Attack(UnitB->UnitA)";
        }

        /// <summary>
        /// Exécute l'action de l'acteur courant puis consomme l'action et termine le tour.
        /// </summary>
        public ActionResolution ExecuteNextStep()
        {
            if (IsBattleOver)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, "BattleFinished: no further actions", ActionFailureReason.InvalidTargeting);
            }

            var actor = _turnController.CurrentActorId;
            ActionResolution result;

            if (actor.Equals(UnitA))
            {
                if (!_unitAHasMovedOnce)
                {
                    State.TryGetEntityPosition(UnitA, out var unitAPosition);
                    var moveOrigin = new GridCoord3(unitAPosition.X, unitAPosition.Y, unitAPosition.Z);
                    var moveDestination = moveOrigin.X == 0
                        ? new GridCoord3(1, 0, 1)
                        : new GridCoord3(0, 0, 0);

                    result = _resolver.Resolve(State, new MoveAction(UnitA, moveOrigin, moveDestination));
                    if (result.Success)
                    {
                        _unitAHasMovedOnce = true;
                    }
                }
                else
                {
                    result = _resolver.Resolve(State, new BasicAttackAction(UnitA, UnitB));
                }
            }
            else
            {
                result = _resolver.Resolve(State, new BasicAttackAction(UnitB, UnitA));
            }

            _turnController.TryConsumeAction(actor);
            if (_turnController.RemainingActions <= 0)
            {
                _turnController.EndTurn();
            }

            EvaluateVictory();
            return result;
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
