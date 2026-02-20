using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Presentation.Scene
{
    /// <summary>
    /// Planificateur de commande côté présentation (sélection acteur/intention -> IActionCommand).
    /// </summary>
    public sealed class VerticalSliceCommandPlanner
    {
        private readonly BattleState _state;
        private readonly MoveActionPolicy? _movePolicyOverride;
        private readonly BasicAttackActionPolicy? _basicAttackPolicyOverride;
        private readonly SkillActionPolicy? _skillPolicyOverride;
        private EntityId _selectedActor;
        private bool _hasSelection;
        private PlannedActionKind _plannedKind;
        private GridCoord3 _plannedDestination;
        private EntityId _plannedTarget;

        public VerticalSliceCommandPlanner(
            BattleState state,
            MoveActionPolicy? movePolicyOverride = null,
            BasicAttackActionPolicy? basicAttackPolicyOverride = null,
            SkillActionPolicy? skillPolicyOverride = null)
        {
            _state = state;
            _movePolicyOverride = movePolicyOverride;
            _basicAttackPolicyOverride = basicAttackPolicyOverride;
            _skillPolicyOverride = skillPolicyOverride;
            _plannedKind = PlannedActionKind.None;
        }

        public bool HasActorSelection => _hasSelection;

        public EntityId SelectedActorId => _selectedActor;

        public string PlannedLabel => _plannedKind switch
        {
            PlannedActionKind.Move => $"Move to {_plannedDestination}",
            PlannedActionKind.Attack => $"Attack {_plannedTarget}",
            PlannedActionKind.Skill => $"Skill {_plannedTarget}",
            _ => "None",
        };

        public void SelectActor(EntityId actorId)
        {
            _selectedActor = actorId;
            _hasSelection = true;
        }

        public void PlanMove(GridCoord3 destination)
        {
            _plannedKind = PlannedActionKind.Move;
            _plannedDestination = destination;
        }

        public void PlanAttack(EntityId targetId)
        {
            _plannedKind = PlannedActionKind.Attack;
            _plannedTarget = targetId;
        }

        public void PlanSkill(EntityId targetId)
        {
            _plannedKind = PlannedActionKind.Skill;
            _plannedTarget = targetId;
        }

        public bool TryBuildCommand(out EntityId actorId, out IActionCommand command)
        {
            actorId = default;
            command = default;

            if (!_hasSelection || _plannedKind == PlannedActionKind.None)
            {
                return false;
            }

            actorId = _selectedActor;

            switch (_plannedKind)
            {
                case PlannedActionKind.Move:
                    if (!_state.TryGetEntityPosition(_selectedActor, out var originPosition))
                    {
                        return false;
                    }

                    var origin = new GridCoord3(originPosition.X, originPosition.Y, originPosition.Z);
                    command = new MoveAction(_selectedActor, origin, _plannedDestination, _movePolicyOverride);
                    return true;

                case PlannedActionKind.Attack:
                    command = new BasicAttackAction(_selectedActor, _plannedTarget, _basicAttackPolicyOverride);
                    return true;

                case PlannedActionKind.Skill:
                    command = new CastSkillAction(_selectedActor, _plannedTarget, _skillPolicyOverride);
                    return true;

                default:
                    return false;
            }
        }

        public void ClearPlannedAction()
        {
            _plannedKind = PlannedActionKind.None;
        }

        private enum PlannedActionKind
        {
            None = 0,
            Move = 1,
            Attack = 2,
            Skill = 3,
        }
    }
}
