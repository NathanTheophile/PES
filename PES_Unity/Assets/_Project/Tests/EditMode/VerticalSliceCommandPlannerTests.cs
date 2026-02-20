using NUnit.Framework;
using PES.Combat.Actions;
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
        public void PlannedLabel_WithSkillPlan_DisplaysSkillTarget()
        {
            var state = new BattleState();
            var planner = new VerticalSliceCommandPlanner(state);
            planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            planner.PlanSkill(VerticalSliceBattleLoop.UnitB);

            Assert.That(planner.PlannedLabel, Does.Contain("Skill"));
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
    }
}
