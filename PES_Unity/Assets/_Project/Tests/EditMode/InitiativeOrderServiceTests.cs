using NUnit.Framework;
using PES.Core.Simulation;
using PES.Core.TurnSystem;

namespace PES.Tests.EditMode
{
    public class InitiativeOrderServiceTests
    {
        [Test]
        public void BuildIndividualTurnOrder_SortsByRapidityDescending()
        {
            var actor1 = new EntityId(201);
            var actor2 = new EntityId(202);
            var actor3 = new EntityId(203);

            var order = InitiativeOrderService.BuildIndividualTurnOrder(new[]
            {
                new BattleActorDefinition(actor1, teamId: 1, new Position3(0, 0, 0), 40, rapidity: 15),
                new BattleActorDefinition(actor2, teamId: 2, new Position3(1, 0, 0), 40, rapidity: 22),
                new BattleActorDefinition(actor3, teamId: 1, new Position3(2, 0, 0), 40, rapidity: 8),
            });

            Assert.That(order[0].ActorId, Is.EqualTo(actor2));
            Assert.That(order[1].ActorId, Is.EqualTo(actor1));
            Assert.That(order[2].ActorId, Is.EqualTo(actor3));
        }

        [Test]
        public void BuildIndividualTurnOrder_DoesNotAlternateTeams_WhenSameTeamHasHighestRapidity()
        {
            var actorA1 = new EntityId(301);
            var actorA2 = new EntityId(302);
            var actorB1 = new EntityId(401);
            var actorB2 = new EntityId(402);
            var actorB3 = new EntityId(403);
            var actorA3 = new EntityId(303);

            var order = InitiativeOrderService.BuildIndividualTurnOrder(new[]
            {
                new BattleActorDefinition(actorA1, teamId: 1, new Position3(0, 0, 0), 40, rapidity: 40),
                new BattleActorDefinition(actorA2, teamId: 1, new Position3(1, 0, 0), 40, rapidity: 35),
                new BattleActorDefinition(actorB1, teamId: 2, new Position3(2, 0, 0), 40, rapidity: 30),
                new BattleActorDefinition(actorB2, teamId: 2, new Position3(3, 0, 0), 40, rapidity: 25),
                new BattleActorDefinition(actorB3, teamId: 2, new Position3(4, 0, 0), 40, rapidity: 20),
                new BattleActorDefinition(actorA3, teamId: 1, new Position3(5, 0, 0), 40, rapidity: 10),
            });

            Assert.That(order[0].ActorId, Is.EqualTo(actorA1));
            Assert.That(order[1].ActorId, Is.EqualTo(actorA2));
            Assert.That(order[2].ActorId, Is.EqualTo(actorB1));
            Assert.That(order[3].ActorId, Is.EqualTo(actorB2));
            Assert.That(order[4].ActorId, Is.EqualTo(actorB3));
            Assert.That(order[5].ActorId, Is.EqualTo(actorA3));
        }
    }
}
