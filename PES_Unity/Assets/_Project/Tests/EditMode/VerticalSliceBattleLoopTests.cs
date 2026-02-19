using NUnit.Framework;
using PES.Presentation.Scene;

namespace PES.Tests.EditMode
{
    public class VerticalSliceBattleLoopTests
    {
        [Test]
        public void Constructor_InitializesTwoUnitsWithHitPointsAndHeightOffset()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            Assert.That(loop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitA, out var unitA), Is.True);
            Assert.That(loop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitB, out var unitB), Is.True);
            Assert.That(unitA.Z, Is.EqualTo(0));
            Assert.That(unitB.Z, Is.EqualTo(1));

            Assert.That(loop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitA, out var hpA), Is.True);
            Assert.That(loop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitB, out var hpB), Is.True);
            Assert.That(hpA, Is.EqualTo(40));
            Assert.That(hpB, Is.EqualTo(40));
        }

        [Test]
        public void ExecuteNextStep_RunsMoveThenAttackSequenceAndAdvancesTicks()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            var first = loop.ExecuteNextStep();
            var second = loop.ExecuteNextStep();

            Assert.That(first.Success, Is.True);
            Assert.That(first.Description, Does.Contain("MoveActionResolved"));
            Assert.That(second.Description, Does.Contain("BasicAttack"));
            Assert.That(loop.State.Tick, Is.EqualTo(2));
            Assert.That(loop.State.StructuredEventLog.Count, Is.EqualTo(2));

            Assert.That(loop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitA, out var unitA), Is.True);
            Assert.That(unitA.X, Is.EqualTo(1));
            Assert.That(unitA.Z, Is.EqualTo(1));
        }
    }
}
