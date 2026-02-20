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
        public void TryConsumeAction_WhenNonCurrentInactiveActor_IsRejected()
        {
            var a = new EntityId(10);
            var b = new EntityId(11);
            var controller = new RoundRobinTurnController(new[] { a, b }, actionsPerTurn: 1);

            controller.SetActorActive(a, false);

            // Le contrôleur bascule automatiquement vers le prochain acteur actif (b).
            Assert.That(controller.CurrentActorId, Is.EqualTo(b));

            // Une consommation au nom de l'acteur inactif doit rester rejetée.
            var consumed = controller.TryConsumeAction(a);
            Assert.That(consumed, Is.False);
        }


        [Test]
        public void SetActorActive_WhenCurrentActorRemainsSame_DoesNotRefillActionsAtZero()
        {
            var a = new EntityId(30);
            var b = new EntityId(31);
            var controller = new RoundRobinTurnController(new[] { a, b }, actionsPerTurn: 1);

            var consumed = controller.TryConsumeAction(a);
            Assert.That(consumed, Is.True);
            Assert.That(controller.RemainingActions, Is.EqualTo(0));

            controller.SetActorActive(a, true);
            controller.SetActorActive(b, true);

            Assert.That(controller.CurrentActorId, Is.EqualTo(a));
            Assert.That(controller.RemainingActions, Is.EqualTo(0));
        }

        [Test]
        public void SetActorActive_WhenCurrentActorBecomesInactive_RefillsForNextActiveActor()
        {
            var a = new EntityId(40);
            var b = new EntityId(41);
            var controller = new RoundRobinTurnController(new[] { a, b }, actionsPerTurn: 2);

            controller.TryConsumeAction(a);
            controller.TryConsumeAction(a);
            Assert.That(controller.RemainingActions, Is.EqualTo(0));

            controller.SetActorActive(a, false);

            Assert.That(controller.CurrentActorId, Is.EqualTo(b));
            Assert.That(controller.RemainingActions, Is.EqualTo(2));
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
