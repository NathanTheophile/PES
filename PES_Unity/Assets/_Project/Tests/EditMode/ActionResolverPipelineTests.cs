// Utility: this script validates that actions are resolved through the domain pipeline
// and that BattleState mutations/logging behave as expected.
using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the resolver-driven command pipeline.
    /// </summary>
    public class ActionResolverPipelineTests
    {
        [Test]
        public void Resolve_MoveAction_GoesThroughPipelineAndUpdatesState()
        {
            // Arrange a clean battle state with one actor at origin.
            var state = new BattleState();
            var actor = new EntityId(1);
            state.SetEntityPosition(actor, new Position3(0, 0, 0));

            // Seeded RNG keeps test behavior reproducible across runs.
            var rng = new SeededRngService(42);
            var resolver = new ActionResolver(rng);
            var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1));

            // Act through the resolver (never calling action side-effects externally).
            var result = resolver.Resolve(state, action);

            // Assert action result and pipeline side-effects.
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description, Does.Contain("MoveActionResolved"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));

            // Assert entity position has effectively changed in domain state.
            Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
            Assert.That(position.X, Is.EqualTo(1));
            Assert.That(position.Y, Is.EqualTo(0));
            Assert.That(position.Z, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_MoveAction_WithTooHighVerticalStep_IsRejectedAndStateUnchanged()
        {
            // Arrange actor with known initial position.
            var state = new BattleState();
            var actor = new EntityId(2);
            state.SetEntityPosition(actor, new Position3(0, 0, 0));

            var rng = new SeededRngService(42);
            var resolver = new ActionResolver(rng);

            // Request an invalid move violating MaxVerticalStep.
            var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(0, 0, 3));

            // Act through the same pipeline.
            var result = resolver.Resolve(state, action);

            // Assert rejection while resolver still logs and advances simulation tick.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description, Does.Contain("MoveActionRejected"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));

            // Assert rollback/unchanged state.
            Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
            Assert.That(position.X, Is.EqualTo(0));
            Assert.That(position.Y, Is.EqualTo(0));
            Assert.That(position.Z, Is.EqualTo(0));
        }
    }
}
