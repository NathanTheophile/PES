using System.Collections.Generic;
using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Presentation.Scene;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Tests.EditMode
{
    public class VerticalSliceCommandPlannerTests
    {
        [Test]
        public void TryBuildCommand_WithMovePlan_ProducesMoveActionForSelectedActor()
        {
            var state = new BattleState();
            state.SetEntityPosition(VerticalSliceBattleLoop.UnitA, new Position3(0, 0, 0));

            var planner = new VerticalSliceCommandPlanner(state);
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanMove(new GridCoord3(1, 0, 1));

            var built = planner.TryBuildCommand(out var actorId, out var command);

            Assert.That(built, Is.True);
            Assert.That(actorId, Is.EqualTo(VerticalSliceBattleLoop.UnitA));
            Assert.That(command, Is.TypeOf<MoveAction>());
        }

        [Test]
        public void TryBuildCommand_WithAttackPlan_ProducesBasicAttackAction()
        {
            var state = new BattleState();
            var planner = new VerticalSliceCommandPlanner(state);
            planner.SelectActor(VerticalSliceBattleLoop.UnitB);
            planner.PlanAttack(VerticalSliceBattleLoop.UnitA);

            var built = planner.TryBuildCommand(out var actorId, out var command);

            Assert.That(built, Is.True);
            Assert.That(actorId, Is.EqualTo(VerticalSliceBattleLoop.UnitB));
            Assert.That(command, Is.TypeOf<BasicAttackAction>());
        }

        [Test]
        public void PlannedLabel_WithSkillPlan_DisplaysSkillTargetAndSlot()
        {
            var state = new BattleState();
            var planner = new VerticalSliceCommandPlanner(state);
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB, skillSlot: 2);

            Assert.That(planner.PlannedLabel, Does.Contain("Skill[2]"));
            Assert.That(planner.PlannedLabel, Does.Contain("Entity(101)"));
        }

        [Test]
        public void TryBuildCommand_WithSkillPlan_ProducesCastSkillAction()
        {
            var state = new BattleState();
            var planner = new VerticalSliceCommandPlanner(
                state,
                skillPolicyOverride: new SkillActionPolicy(0, 1, 3, 8, 85, 2, 1));
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB);

            var built = planner.TryBuildCommand(out var actorId, out var command);

            Assert.That(built, Is.True);
            Assert.That(actorId, Is.EqualTo(VerticalSliceBattleLoop.UnitA));
            Assert.That(command, Is.TypeOf<CastSkillAction>());
        }

        [Test]
        public void TryBuildCommand_WithPerActorSkillLoadout_UsesSelectedSkillSlotPolicy()
        {
            var state = new BattleState();
            state.SetEntityPosition(VerticalSliceBattleLoop.UnitA, new Position3(0, 0, 0));
            state.SetEntityPosition(VerticalSliceBattleLoop.UnitB, new Position3(1, 0, 0));
            state.SetEntityHitPoints(VerticalSliceBattleLoop.UnitA, 40);
            state.SetEntityHitPoints(VerticalSliceBattleLoop.UnitB, 40);
            state.SetEntitySkillResource(VerticalSliceBattleLoop.UnitA, 10);

            var loadout = new Dictionary<EntityId, SkillActionPolicy[]>
            {
                [VerticalSliceBattleLoop.UnitA] = new[]
                {
                    new SkillActionPolicy(1, 1, 3, 3, 100, 2, 1, 1, 0),
                    new SkillActionPolicy(9, 1, 3, 5, 100, 2, 1, 1, 0),
                }
            };

            var planner = new VerticalSliceCommandPlanner(
                state,
                skillLoadoutByActor: loadout);

            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB, skillSlot: 1);

            var built = planner.TryBuildCommand(out _, out var command);

            Assert.That(built, Is.True);
            Assert.That(command, Is.TypeOf<CastSkillAction>());

            var resolution = command.Resolve(state, new SeededRngService(1));
            Assert.That(resolution.Payload.HasValue, Is.True);
            Assert.That(resolution.Payload.Value.Value1, Is.EqualTo(9));
        }

        [Test]
        public void TryGetSkillPolicy_WithSingleOverrideOnlySlotZero_IsTrue()
        {
            var state = new BattleState();
            var policy = new SkillActionPolicy(5, 1, 3, 8, 85, 2, 1);
            var planner = new VerticalSliceCommandPlanner(state, skillPolicyOverride: policy);

            var ok = planner.TryGetSkillPolicy(VerticalSliceBattleLoop.UnitA, 0, out var resolved);

            Assert.That(ok, Is.True);
            Assert.That(resolved.SkillId, Is.EqualTo(5));
        }

        [Test]
        public void TryGetSkillPolicy_WithSingleOverrideAndSlotOne_IsFalse()
        {
            var state = new BattleState();
            var policy = new SkillActionPolicy(5, 1, 3, 8, 85, 2, 1);
            var planner = new VerticalSliceCommandPlanner(state, skillPolicyOverride: policy);

            var ok = planner.TryGetSkillPolicy(VerticalSliceBattleLoop.UnitA, 1, out _);

            Assert.That(ok, Is.False);
        }

        [Test]
        public void GetAvailableSkillCount_WithSingleOverride_ReturnsOne()
        {
            var state = new BattleState();
            var policy = new SkillActionPolicy(5, 1, 3, 8, 85, 2, 1);
            var planner = new VerticalSliceCommandPlanner(state, skillPolicyOverride: policy);

            var count = planner.GetAvailableSkillCount(VerticalSliceBattleLoop.UnitA);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void GetAvailableSkillCount_WithoutSkillPolicies_ReturnsZero()
        {
            var state = new BattleState();
            var planner = new VerticalSliceCommandPlanner(state);

            var count = planner.GetAvailableSkillCount(VerticalSliceBattleLoop.UnitA);

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void TryBuildCommand_WithSkillPlanAndNoPolicies_ReturnsFalse()
        {
            var state = new BattleState();
            var planner = new VerticalSliceCommandPlanner(state);
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB, skillSlot: 0);

            var built = planner.TryBuildCommand(out _, out _);

            Assert.That(built, Is.False);
        }

        [Test]
        public void TryBuildCommand_WithSkillPlanAndOutOfRangeSlot_ReturnsFalse()
        {
            var state = new BattleState();
            var loadout = new Dictionary<EntityId, SkillActionPolicy[]>
            {
                [VerticalSliceBattleLoop.UnitA] = new[]
                {
                    new SkillActionPolicy(1, 1, 3, 3, 100, 2, 1, 1, 0),
                }
            };

            var planner = new VerticalSliceCommandPlanner(state, skillLoadoutByActor: loadout);
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB, skillSlot: 5);

            var built = planner.TryBuildCommand(out _, out _);

            Assert.That(built, Is.False);
        }


        [Test]
        public void HasPlannedAction_WhenMoveIsPlanned_IsTrueThenFalseAfterClear()
        {
            var state = new BattleState();
            state.SetEntityPosition(VerticalSliceBattleLoop.UnitA, new Position3(0, 0, 0));

            var planner = new VerticalSliceCommandPlanner(state);
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanMove(new GridCoord3(1, 0, 1));

            Assert.That(planner.HasPlannedAction, Is.True);

            planner.ClearPlannedAction();

            Assert.That(planner.HasPlannedAction, Is.False);
            Assert.That(planner.PlannedLabel, Is.EqualTo("None"));
        }

        [Test]
        public void HasPlannedAction_DefaultPlanner_IsFalse()
        {
            var planner = new VerticalSliceCommandPlanner(new BattleState());

            Assert.That(planner.HasPlannedAction, Is.False);
            Assert.That(planner.PlannedLabel, Is.EqualTo("None"));
        }



        [Test]
        public void PlannedSkillAccessors_WhenSkillPlanned_ReturnTargetAndSlot()
        {
            var planner = new VerticalSliceCommandPlanner(new BattleState());
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB, skillSlot: 1);

            var hasTarget = planner.TryGetPlannedTarget(out var target);

            Assert.That(planner.HasPlannedSkill, Is.True);
            Assert.That(planner.PlannedSkillSlot, Is.EqualTo(1));
            Assert.That(hasTarget, Is.True);
            Assert.That(target, Is.EqualTo(VerticalSliceBattleLoop.UnitB));
        }

        [Test]
        public void PlannedSkillAccessors_WhenNoSkillPlanned_ReturnDefaults()
        {
            var planner = new VerticalSliceCommandPlanner(new BattleState());
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanMove(new GridCoord3(1, 0, 1));

            var hasTarget = planner.TryGetPlannedTarget(out var target);

            Assert.That(planner.HasPlannedSkill, Is.False);
            Assert.That(planner.PlannedSkillSlot, Is.EqualTo(-1));
            Assert.That(hasTarget, Is.False);
            Assert.That(target, Is.EqualTo(default(EntityId)));
        }

    }
}
