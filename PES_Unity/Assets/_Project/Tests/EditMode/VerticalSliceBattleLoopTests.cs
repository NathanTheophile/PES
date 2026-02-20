using NUnit.Framework;
using PES.Combat.Actions;
using PES.Presentation.Scene;

namespace PES.Tests.EditMode
{
    public class VerticalSliceBattleLoopTests
    {
        [Test]
        public void Constructor_InitializesRoundAndCurrentActor()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            Assert.That(loop.CurrentRound, Is.EqualTo(1));
            Assert.That(loop.RemainingActions, Is.EqualTo(1));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitA"));
            Assert.That(loop.PeekNextStepLabel(), Is.EqualTo("Move(UnitA)"));
            Assert.That(loop.IsBattleOver, Is.False);
        }

        [Test]
        public void ExecuteNextStep_ConsumesActionAndSwitchesTurn()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            var first = loop.ExecuteNextStep();

            Assert.That(first.Success, Is.True);
            Assert.That(first.Description, Does.Contain("MoveActionResolved"));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitB"));
            Assert.That(loop.CurrentRound, Is.EqualTo(1));
            Assert.That(loop.RemainingActions, Is.EqualTo(1));
            Assert.That(loop.PeekNextStepLabel(), Is.EqualTo("Attack(UnitB->UnitA)"));
        }

        [Test]
        public void ExecuteNextStep_AfterTwoTurns_StartsNextRoundWithUnitA()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            loop.ExecuteNextStep();
            loop.ExecuteNextStep();

            Assert.That(loop.CurrentRound, Is.EqualTo(2));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitA"));
            Assert.That(loop.PeekNextStepLabel(), Is.EqualTo("Attack(UnitA->UnitB)"));
        }


        [Test]
        public void TryExecutePlannedCommand_WhenActorIsNotCurrentTurn_IsRejected()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            var accepted = loop.TryExecutePlannedCommand(
                VerticalSliceBattleLoop.UnitB,
                new BasicAttackAction(VerticalSliceBattleLoop.UnitB, VerticalSliceBattleLoop.UnitA),
                out var result);

            Assert.That(accepted, Is.False);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description, Does.Contain("TurnRejected"));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitA"));
        }

        [Test]
        public void ExecuteNextStep_BattleEventuallyEndsAndWinnerIsSet()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            // Boucle bornée de sécurité pour atteindre une fin de combat.
            for (var i = 0; i < 40 && !loop.IsBattleOver; i++)
            {
                loop.ExecuteNextStep();
            }

            Assert.That(loop.IsBattleOver, Is.True);
            Assert.That(loop.WinnerTeamId.HasValue, Is.True);
            Assert.That(loop.PeekNextStepLabel(), Is.EqualTo("BattleFinished"));

            var afterEnd = loop.ExecuteNextStep();
            Assert.That(afterEnd.Success, Is.False);
            Assert.That(afterEnd.Description, Does.Contain("BattleFinished"));
        }
    }
}
