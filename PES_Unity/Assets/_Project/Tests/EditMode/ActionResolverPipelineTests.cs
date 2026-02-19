using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Tests.EditMode
{
    public class ActionResolverPipelineTests
    {
        [Test]
        public void Resolve_MoveAction_GoesThroughPipelineAndUpdatesState()
        {
            var state = new BattleState();
            var rng = new SeededRngService(42);
            var resolver = new ActionResolver(rng);
            var action = new MoveAction(
                new EntityId(1),
                new GridCoord3(0, 0, 0),
                new GridCoord3(1, 0, 1));

            var result = resolver.Resolve(state, action);

            Assert.That(result.Success, Is.True);
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));
            Assert.That(state.EventLog[0], Does.Contain("MoveAction"));
        }
    }
}
