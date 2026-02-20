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
    }
}
