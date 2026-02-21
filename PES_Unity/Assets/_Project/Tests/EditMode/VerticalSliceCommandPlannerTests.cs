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
            var planner = new VerticalSliceCommandPlanner(state);
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
    }
}
