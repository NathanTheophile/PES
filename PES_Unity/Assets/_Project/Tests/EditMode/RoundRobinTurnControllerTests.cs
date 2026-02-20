using NUnit.Framework;
using PES.Core.Simulation;
using PES.Core.TurnSystem;

namespace PES.Tests.EditMode
{
    public sealed class RoundRobinTurnControllerTests
    {
        [Test]
        public void EndTurn_SkipsInactiveActor()
        {
            var a = new EntityId(1);
            var b = new EntityId(2);
            var c = new EntityId(3);
            var controller = new RoundRobinTurnController(new[] { a, b, c }, actionsPerTurn: 1);

            controller.SetActorActive(b, false);
            controller.EndTurn();

            Assert.That(controller.CurrentActorId, Is.EqualTo(c));
        }

        [Test]
        public void TryConsumeAction_WhenActorInactive_ReturnsFalse()
        {
            var a = new EntityId(10);
            var b = new EntityId(11);
            var controller = new RoundRobinTurnController(new[] { a, b }, actionsPerTurn: 1);

            controller.SetActorActive(a, false);

            var consumed = controller.TryConsumeAction(controller.CurrentActorId);
            Assert.That(consumed, Is.False);
        }

        [Test]
        public void EndTurn_WhenNoActiveActor_SetsZeroRemainingActions()
        {
            var a = new EntityId(20);
            var b = new EntityId(21);
            var controller = new RoundRobinTurnController(new[] { a, b }, actionsPerTurn: 2);

            controller.SetActorActive(a, false);
            controller.SetActorActive(b, false);
            controller.EndTurn();

            Assert.That(controller.RemainingActions, Is.EqualTo(0));
        }
    }
}
