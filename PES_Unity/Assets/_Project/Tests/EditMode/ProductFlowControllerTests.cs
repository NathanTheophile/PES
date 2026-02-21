using NUnit.Framework;
using PES.Infrastructure.Serialization;
using PES.Presentation.Flow;

namespace PES.Tests.EditMode
{
    public sealed class ProductFlowControllerTests
    {
        [Test]
        public void Boot_WithoutSave_StartsInMenuWithoutResumeFlag()
        {
            var store = new InMemorySessionSaveStore();
            var flow = new ProductFlowController(store);

            flow.Boot();

            Assert.That(flow.CurrentState, Is.EqualTo(ProductFlowState.Menu));
            Assert.That(flow.HasBattleToResume, Is.False);
            Assert.That(flow.LastBattleSeed, Is.EqualTo(0));
        }

        [Test]
        public void StartNewBattle_ThenBoot_RestoresResumeContext()
        {
            var store = new InMemorySessionSaveStore();
            var flow = new ProductFlowController(store);

            flow.Boot();
            flow.StartNewBattle(1337);

            var restartedFlow = new ProductFlowController(store);
            restartedFlow.Boot();

            Assert.That(restartedFlow.CurrentState, Is.EqualTo(ProductFlowState.Menu));
            Assert.That(restartedFlow.HasBattleToResume, Is.True);
            Assert.That(restartedFlow.LastBattleSeed, Is.EqualTo(1337));
        }

        [Test]
        public void CompleteBattleAndReturnToMenu_ClearsResumeFlagInSave()
        {
            var store = new InMemorySessionSaveStore();
            var flow = new ProductFlowController(store);
            flow.Boot();
            flow.StartNewBattle(77);

            flow.CompleteBattleAndReturnToMenu();

            var restartedFlow = new ProductFlowController(store);
            restartedFlow.Boot();
            Assert.That(restartedFlow.HasBattleToResume, Is.False);
            Assert.That(restartedFlow.CurrentState, Is.EqualTo(ProductFlowState.Menu));
        }
    }
}
