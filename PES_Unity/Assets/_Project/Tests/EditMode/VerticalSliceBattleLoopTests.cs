using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Core.TurnSystem;
using PES.Grid.Grid3D;
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
            Assert.That(loop.TurnDurationSeconds, Is.EqualTo(10f));
            Assert.That(loop.RemainingTurnSeconds, Is.EqualTo(10f));
        }



        [Test]
        public void Constructor_WhenNoDurationProvided_UsesThirtySecondsDefault()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            Assert.That(loop.TurnDurationSeconds, Is.EqualTo(30f));
            Assert.That(loop.RemainingTurnSeconds, Is.EqualTo(30f));
        }

        [Test]
        public void TryPassTurn_WithCurrentActor_SwitchesTurnAndAdvancesTick()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            var accepted = loop.TryPassTurn(VerticalSliceBattleLoop.UnitA, out var result);

            Assert.That(accepted, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitB"));
            Assert.That(loop.State.Tick, Is.EqualTo(1));
            Assert.That(loop.State.StructuredEventLog.Count, Is.EqualTo(1));
            Assert.That(loop.State.StructuredEventLog[0].Description, Does.Contain("TurnPassed"));
        }

        [Test]
        public void TryAdvanceTurnTimer_WhenDurationNotReached_DoesNotSwitchActor()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3, turnDurationSeconds: 5f);

            var timedOut = loop.TryAdvanceTurnTimer(1.25f, out var timeoutResult);

            Assert.That(timedOut, Is.False);
            Assert.That(timeoutResult, Is.EqualTo(default(ActionResolution)));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitA"));
            Assert.That(loop.CurrentRound, Is.EqualTo(1));
            Assert.That(loop.RemainingTurnSeconds, Is.EqualTo(3.75f).Within(0.0001f));
        }

        [Test]
        public void TryAdvanceTurnTimer_WhenDurationReached_SwitchesActorAndResetsTimer()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3, turnDurationSeconds: 2f);

            var timedOut = loop.TryAdvanceTurnTimer(2.1f, out var timeoutResult);

            Assert.That(timedOut, Is.True);
            Assert.That(timeoutResult.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(timeoutResult.FailureReason, Is.EqualTo(ActionFailureReason.TurnTimedOut));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitB"));
            Assert.That(loop.CurrentRound, Is.EqualTo(1));
            Assert.That(loop.RemainingTurnSeconds, Is.EqualTo(2f).Within(0.0001f));
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
            Assert.That(loop.RemainingTurnSeconds, Is.EqualTo(loop.TurnDurationSeconds).Within(0.0001f));
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
        public void TryExecutePlannedCommand_WhenActionIsRejected_DoesNotConsumeTurn()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3);

            var accepted = loop.TryExecutePlannedCommand(
                VerticalSliceBattleLoop.UnitA,
                new MoveAction(VerticalSliceBattleLoop.UnitA, new GridCoord3(0, 0, 0), new GridCoord3(0, 0, 0)),
                out var result);

            Assert.That(accepted, Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitA"));
            Assert.That(loop.CurrentRound, Is.EqualTo(1));
            Assert.That(loop.RemainingActions, Is.EqualTo(1));
        }


        [Test]
        public void TryExecutePlannedCommand_WithTwoActionsPerTurn_DoesNotSwitchActorAfterFirstAction()
        {
            var loop = new VerticalSliceBattleLoop(seed: 3, actionsPerTurn: 2);

            var accepted = loop.TryExecutePlannedCommand(
                VerticalSliceBattleLoop.UnitA,
                new MoveAction(VerticalSliceBattleLoop.UnitA, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1)),
                out var result);

            Assert.That(accepted, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitA"));
            Assert.That(loop.RemainingActions, Is.EqualTo(1));
        }


        [Test]
        public void ExecuteNextStep_WithControlPointObjective_EndsBattleBeforeElimination()
        {
            var objective = new IBattleObjective[]
            {
                new ControlPointObjective(new Position3(1, 0, 1)),
            };

            var loop = new VerticalSliceBattleLoop(seed: 3, objectives: objective);

            var first = loop.ExecuteNextStep();

            Assert.That(first.Success, Is.True);
            Assert.That(loop.IsBattleOver, Is.True);
            Assert.That(loop.WinnerTeamId, Is.EqualTo(1));
            Assert.That(loop.PeekNextStepLabel(), Is.EqualTo("BattleFinished"));
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

        [Test]
        public void TryExecutePlannedCommand_WhenTurnEnds_ResetsNextActorMovementPoints()
        {
            var definitions = new[]
            {
                new BattleActorDefinition(VerticalSliceBattleLoop.UnitA, 1, new Position3(0, 0, 0), 40, startMovementPoints: 6),
                new BattleActorDefinition(VerticalSliceBattleLoop.UnitB, 2, new Position3(2, 0, 1), 40, startMovementPoints: 6),
            };

            var loop = new VerticalSliceBattleLoop(seed: 3, actorDefinitions: definitions);

            var accepted = loop.TryExecutePlannedCommand(
                VerticalSliceBattleLoop.UnitA,
                new MoveAction(VerticalSliceBattleLoop.UnitA, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1)),
                out var result);

            Assert.That(accepted, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(loop.PeekCurrentActorLabel(), Is.EqualTo("UnitB"));
            Assert.That(loop.State.TryGetEntityMovementPoints(VerticalSliceBattleLoop.UnitB, out var unitBMovement), Is.True);
            Assert.That(unitBMovement, Is.EqualTo(6));
        }

    }
}
