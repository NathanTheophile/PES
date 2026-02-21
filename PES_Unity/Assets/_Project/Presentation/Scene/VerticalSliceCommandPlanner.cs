using System.Collections.Generic;
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
        private readonly IReadOnlyDictionary<EntityId, SkillActionPolicy[]> _skillLoadoutByActor;
        private EntityId _selectedActor;
        private bool _hasSelection;
        private PlannedActionKind _plannedKind;
        private GridCoord3 _plannedDestination;
        private EntityId _plannedTarget;
        private int _plannedSkillSlot;

        public VerticalSliceCommandPlanner(
            BattleState state,
            MoveActionPolicy? movePolicyOverride = null,
            BasicAttackActionPolicy? basicAttackPolicyOverride = null,
            SkillActionPolicy? skillPolicyOverride = null,
            IReadOnlyDictionary<EntityId, SkillActionPolicy[]> skillLoadoutByActor = null)
        {
            _state = state;
            _movePolicyOverride = movePolicyOverride;
            _basicAttackPolicyOverride = basicAttackPolicyOverride;
            _skillPolicyOverride = skillPolicyOverride;
            _skillLoadoutByActor = skillLoadoutByActor;
            _plannedKind = PlannedActionKind.None;
        }

        public bool HasActorSelection => _hasSelection;

        public EntityId SelectedActorId => _selectedActor;

        public int GetAvailableSkillCount(EntityId actorId)
        {
            if (_skillLoadoutByActor != null && _skillLoadoutByActor.TryGetValue(actorId, out var policies) && policies != null)
            {
                return policies.Length;
            }

            return _skillPolicyOverride.HasValue ? 1 : 0;
        }


        public bool TryGetSkillPolicy(EntityId actorId, int skillSlot, out SkillActionPolicy policy)
        {
            if (_skillLoadoutByActor != null && _skillLoadoutByActor.TryGetValue(actorId, out var policies) &&
                policies != null)
            {
                if (skillSlot < 0 || skillSlot >= policies.Length)
                {
                    policy = default;
                    return false;
                }

                policy = policies[skillSlot];
                return true;
            }

            if (_skillPolicyOverride.HasValue && skillSlot == 0)
            {
                policy = _skillPolicyOverride.Value;
                return true;
            }

            policy = default;
            return false;
        }

        public bool HasPlannedAction => _plannedKind != PlannedActionKind.None;



        public bool HasPlannedMove => _plannedKind == PlannedActionKind.Move;

        public bool TryGetPlannedMoveDestination(out GridCoord3 destination)
        {
            if (_plannedKind == PlannedActionKind.Move)
            {
                destination = _plannedDestination;
                return true;
            }

            destination = default;
            return false;
        }

        public bool HasPlannedSkill => _plannedKind == PlannedActionKind.Skill;

        public int PlannedSkillSlot => _plannedKind == PlannedActionKind.Skill ? _plannedSkillSlot : -1;

        public bool TryGetPlannedTarget(out EntityId targetId)
        {
            if (_plannedKind == PlannedActionKind.Attack || _plannedKind == PlannedActionKind.Skill)
            {
                targetId = _plannedTarget;
                return true;
            }

            targetId = default;
            return false;
        }

        public string PlannedLabel => _plannedKind switch
        {
            PlannedActionKind.Move => $"Move to {_plannedDestination}",
            PlannedActionKind.Attack => $"Attack {_plannedTarget}",
            PlannedActionKind.Skill => $"Skill[{_plannedSkillSlot}] {_plannedTarget}",
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

        public void PlanSkill(EntityId targetId, int skillSlot = 0)
        {
            _plannedKind = PlannedActionKind.Skill;
            _plannedTarget = targetId;
            _plannedSkillSlot = skillSlot < 0 ? 0 : skillSlot;
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
                    if (!TryResolveSkillPolicyOverride(_selectedActor, _plannedSkillSlot, out var policyOverride))
                    {
                        return false;
                    }

                    command = new CastSkillAction(_selectedActor, _plannedTarget, policyOverride);
                    return true;

                default:
                    return false;
            }
        }

        public void ClearPlannedAction()
        {
            _plannedKind = PlannedActionKind.None;
        }

        private bool TryResolveSkillPolicyOverride(EntityId actorId, int skillSlot, out SkillActionPolicy? policy)
        {
            if (TryGetSkillPolicy(actorId, skillSlot, out var resolvedPolicy))
            {
                policy = resolvedPolicy;
                return true;
            }

            policy = default;
            return false;
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
