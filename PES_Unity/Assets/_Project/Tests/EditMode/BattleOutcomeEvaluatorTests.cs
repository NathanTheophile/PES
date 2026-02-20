using System.Collections.Generic;
using NUnit.Framework;
using PES.Core.Simulation;
using PES.Core.TurnSystem;

namespace PES.Tests.EditMode
{
    public sealed class BattleOutcomeEvaluatorTests
    {
        [Test]
        public void Evaluate_WhenTwoTeamsAlive_ReturnsNotOver()
        {
            var state = new BattleState();
            var unitA = new EntityId(1);
            var unitB = new EntityId(2);
            state.SetEntityHitPoints(unitA, 10);
            state.SetEntityHitPoints(unitB, 5);

            var evaluator = new BattleOutcomeEvaluator();
            var outcome = evaluator.Evaluate(state, new Dictionary<EntityId, int>
            {
                [unitA] = 1,
                [unitB] = 2,
            });

            Assert.That(outcome.IsBattleOver, Is.False);
            Assert.That(outcome.WinnerTeamId.HasValue, Is.False);
        }

        [Test]
        public void Evaluate_WhenSingleTeamAlive_ReturnsWinner()
        {
            var state = new BattleState();
            var unitA = new EntityId(3);
            var unitB = new EntityId(4);
            state.SetEntityHitPoints(unitA, 7);
            state.SetEntityHitPoints(unitB, 0);

            var evaluator = new BattleOutcomeEvaluator();
            var outcome = evaluator.Evaluate(state, new Dictionary<EntityId, int>
            {
                [unitA] = 7,
                [unitB] = 8,
            });

            Assert.That(outcome.IsBattleOver, Is.True);
            Assert.That(outcome.WinnerTeamId, Is.EqualTo(7));
        }

        [Test]
        public void Evaluate_WhenNoActorAlive_ReturnsDraw()
        {
            var state = new BattleState();
            var unitA = new EntityId(30);
            var unitB = new EntityId(31);
            state.SetEntityHitPoints(unitA, 0);
            state.SetEntityHitPoints(unitB, -3);

            var evaluator = new BattleOutcomeEvaluator();
            var outcome = evaluator.Evaluate(state, new Dictionary<EntityId, int>
            {
                [unitA] = 1,
                [unitB] = 2,
            });

            Assert.That(outcome.IsBattleOver, Is.True);
            Assert.That(outcome.WinnerTeamId, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_WithControlPointObjective_WhenOneTeamControlsPoint_ReturnsObjectiveWinner()
        {
            var state = new BattleState();
            var unitA = new EntityId(40);
            var unitB = new EntityId(41);

            state.SetEntityPosition(unitA, new Position3(1, 1, 0));
            state.SetEntityHitPoints(unitA, 15);
            state.SetEntityPosition(unitB, new Position3(3, 1, 0));
            state.SetEntityHitPoints(unitB, 15);

            var evaluator = new BattleOutcomeEvaluator(new IBattleObjective[]
            {
                new ControlPointObjective(new Position3(1, 1, 0)),
            });

            var outcome = evaluator.Evaluate(state, new Dictionary<EntityId, int>
            {
                [unitA] = 1,
                [unitB] = 2,
            });

            Assert.That(outcome.IsBattleOver, Is.True);
            Assert.That(outcome.WinnerTeamId, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_WithControlPointObjective_WhenPointIsContested_DoesNotResolveObjective()
        {
            var state = new BattleState();
            var unitA = new EntityId(50);
            var unitB = new EntityId(51);

            state.SetEntityPosition(unitA, new Position3(2, 2, 0));
            state.SetEntityHitPoints(unitA, 10);
            state.SetEntityPosition(unitB, new Position3(2, 2, 0));
            state.SetEntityHitPoints(unitB, 10);

            var evaluator = new BattleOutcomeEvaluator(new IBattleObjective[]
            {
                new ControlPointObjective(new Position3(2, 2, 0)),
            });

            var outcome = evaluator.Evaluate(state, new Dictionary<EntityId, int>
            {
                [unitA] = 1,
                [unitB] = 2,
            });

            Assert.That(outcome.IsBattleOver, Is.False);
            Assert.That(outcome.WinnerTeamId.HasValue, Is.False);
        }
    }
}
