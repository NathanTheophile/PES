using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Presentation.Scene
{
    /// <summary>
    /// Petit orchestrateur pour démontrer un loop move/attack avec 2 unités sur une map à dénivelé.
    /// </summary>
    public sealed class VerticalSliceBattleLoop
    {
        public static readonly EntityId UnitA = new(100);
        public static readonly EntityId UnitB = new(101);

        private readonly ActionResolver _resolver;
        private int _loopStep;

        public VerticalSliceBattleLoop(int seed = 7)
        {
            State = new BattleState();
            _resolver = new ActionResolver(new SeededRngService(seed));

            State.SetEntityPosition(UnitA, new Position3(0, 0, 0));
            State.SetEntityPosition(UnitB, new Position3(2, 0, 1));
            State.SetEntityHitPoints(UnitA, 40);
            State.SetEntityHitPoints(UnitB, 40);
        }

        public BattleState State { get; }

        /// <summary>
        /// Exécute la prochaine action de démonstration.
        /// Séquence: Move(A) -> Attack(A,B) -> Attack(B,A) puis boucle.
        /// </summary>
        public ActionResolution ExecuteNextStep()
        {
            ActionResolution result;

            switch (_loopStep)
            {
                case 0:
                    State.TryGetEntityPosition(UnitA, out var unitAPosition);
                    var moveOrigin = new GridCoord3(unitAPosition.X, unitAPosition.Y, unitAPosition.Z);
                    var moveDestination = moveOrigin.X == 0
                        ? new GridCoord3(1, 0, 1)
                        : new GridCoord3(0, 0, 0);

                    result = _resolver.Resolve(
                        State,
                        new MoveAction(UnitA, moveOrigin, moveDestination));
                    break;

                case 1:
                    result = _resolver.Resolve(State, new BasicAttackAction(UnitA, UnitB));
                    break;

                default:
                    result = _resolver.Resolve(State, new BasicAttackAction(UnitB, UnitA));
                    break;
            }

            _loopStep = (_loopStep + 1) % 3;
            return result;
        }
    }
}
