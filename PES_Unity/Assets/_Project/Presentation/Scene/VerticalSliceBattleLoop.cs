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
        private DemoStep _nextStep;

        public VerticalSliceBattleLoop(int seed = 7)
        {
            State = new BattleState();
            _resolver = new ActionResolver(new SeededRngService(seed));
            _nextStep = DemoStep.MoveUnitA;

            State.SetEntityPosition(UnitA, new Position3(0, 0, 0));
            State.SetEntityPosition(UnitB, new Position3(2, 0, 1));
            State.SetEntityHitPoints(UnitA, 40);
            State.SetEntityHitPoints(UnitB, 40);
        }

        public BattleState State { get; }

        /// <summary>
        /// Retourne le nom lisible de la prochaine étape de la boucle de démo.
        /// </summary>
        public string PeekNextStepLabel()
        {
            return _nextStep switch
            {
                DemoStep.MoveUnitA => "Move(UnitA)",
                DemoStep.AttackUnitAtoB => "Attack(UnitA->UnitB)",
                _ => "Attack(UnitB->UnitA)",
            };
        }

        /// <summary>
        /// Exécute la prochaine action de démonstration.
        /// Séquence: Move(A) -> Attack(A,B) -> Attack(B,A) puis boucle.
        /// </summary>
        public ActionResolution ExecuteNextStep()
        {
            ActionResolution result;

            switch (_nextStep)
            {
                case DemoStep.MoveUnitA:
                    State.TryGetEntityPosition(UnitA, out var unitAPosition);
                    var moveOrigin = new GridCoord3(unitAPosition.X, unitAPosition.Y, unitAPosition.Z);
                    var moveDestination = moveOrigin.X == 0
                        ? new GridCoord3(1, 0, 1)
                        : new GridCoord3(0, 0, 0);

                    result = _resolver.Resolve(
                        State,
                        new MoveAction(UnitA, moveOrigin, moveDestination));
                    _nextStep = DemoStep.AttackUnitAtoB;
                    break;

                case DemoStep.AttackUnitAtoB:
                    result = _resolver.Resolve(State, new BasicAttackAction(UnitA, UnitB));
                    _nextStep = DemoStep.AttackUnitBtoA;
                    break;

                default:
                    result = _resolver.Resolve(State, new BasicAttackAction(UnitB, UnitA));
                    _nextStep = DemoStep.MoveUnitA;
                    break;
            }

            return result;
        }

        private enum DemoStep
        {
            MoveUnitA = 0,
            AttackUnitAtoB = 1,
            AttackUnitBtoA = 2,
        }
    }
}
