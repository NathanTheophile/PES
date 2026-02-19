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
            var actor = new EntityId(1);
            state.SetEntityPosition(actor, new Position3(0, 0, 0));

            var rng = new SeededRngService(42);
            var resolver = new ActionResolver(rng);
            var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1));

            var result = resolver.Resolve(state, action);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Description, Does.Contain("MoveActionResolved"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));

            Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
            Assert.That(position.X, Is.EqualTo(1));
            Assert.That(position.Y, Is.EqualTo(0));
            Assert.That(position.Z, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_MoveAction_WithTooHighVerticalStep_IsRejectedAndStateUnchanged()
        {
            var state = new BattleState();
            var actor = new EntityId(2);
            state.SetEntityPosition(actor, new Position3(0, 0, 0));

            var rng = new SeededRngService(42);
            var resolver = new ActionResolver(rng);
            var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(0, 0, 3));

            var result = resolver.Resolve(state, action);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Description, Does.Contain("MoveActionRejected"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));

            Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
            Assert.That(position.X, Is.EqualTo(0));
            Assert.That(position.Y, Is.EqualTo(0));
            Assert.That(position.Z, Is.EqualTo(0));
        }
    }
}
