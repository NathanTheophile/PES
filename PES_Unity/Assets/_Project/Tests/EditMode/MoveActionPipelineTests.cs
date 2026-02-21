using NUnit.Framework;
using PES.Combat.Actions;
using PES.Combat.Resolution;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;


namespace PES.Tests.EditMode
{
    public class MoveActionPipelineTests
    {
        [Test]
                public void Resolve_MoveAction_GoesThroughPipelineAndUpdatesState()
                {
                    // Arrange : état initial avec un acteur placé à l'origine.
                    var state = new BattleState();
                    var actor = new EntityId(1);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
        
                    // RNG seedée pour conserver un comportement reproductible de test.
                    var rng = new SeededRngService(42);
                    var resolver = new ActionResolver(rng);
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 1));
        
                    // Act : toujours passer par le resolver (pas d'appel direct hors pipeline).
                    var result = resolver.Resolve(state, action);
        
                    // Assert : résultat de l'action + effets de pipeline.
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
                    Assert.That(result.Description, Does.Contain("MoveActionResolved"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.None));
                    Assert.That(state.Tick, Is.EqualTo(1));
                    Assert.That(state.EventLog.Count, Is.EqualTo(1));
                    Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
                    Assert.That(state.StructuredEventLog[0].Code, Is.EqualTo(result.Code));
                    Assert.That(state.StructuredEventLog[0].Tick, Is.EqualTo(0));
        
                    // Assert : position effectivement mise à jour dans l'état métier.
                    Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
                    Assert.That(position.X, Is.EqualTo(1));
                    Assert.That(position.Y, Is.EqualTo(0));
                    Assert.That(position.Z, Is.EqualTo(1));
                }

        [Test]
                public void Resolve_MoveAction_WithTooHighVerticalStep_IsRejectedAndStateUnchanged()
                {
                    // Arrange : acteur avec position initiale connue.
                    var state = new BattleState();
                    var actor = new EntityId(2);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
        
                    var rng = new SeededRngService(42);
                    var resolver = new ActionResolver(rng);
        
                    // Action invalide qui viole MaxVerticalStep.
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(0, 0, 3));
        
                    // Act : résolution via le même pipeline.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet, mais le resolver journalise quand même et incrémente le tick.
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.Description, Does.Contain("MoveActionRejected"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.VerticalStepTooHigh));
                    Assert.That(state.Tick, Is.EqualTo(1));
                    Assert.That(state.EventLog.Count, Is.EqualTo(1));
                    Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
                    Assert.That(state.StructuredEventLog[0].Code, Is.EqualTo(result.Code));
        
                    // Assert : rollback / état inchangé.
                    Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
                    Assert.That(position.X, Is.EqualTo(0));
                    Assert.That(position.Y, Is.EqualTo(0));
                    Assert.That(position.Z, Is.EqualTo(0));
                }

        [Test]
                public void Resolve_MoveAction_WithInvalidOrigin_ReturnsStructuredFailureReason()
                {
                    // Arrange : l'acteur est à (0,0,0) mais l'origine déclarée est incorrecte.
                    var state = new BattleState();
                    var actor = new EntityId(9);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(1, 0, 0), new GridCoord3(2, 0, 0));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet lisible + raison stable pour exploitation outillage/replay.
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.Description, Does.Contain("invalid origin"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.InvalidOrigin));
                }

        [Test]
                public void Resolve_MoveAction_WhenDestinationEqualsOrigin_IsRejectedWithNoMovementReason()
                {
                    // Arrange : destination identique à l'origine (aucun déplacement réel).
                    var state = new BattleState();
                    var actor = new EntityId(12);
                    state.SetEntityPosition(actor, new Position3(2, 0, 1));
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(2, 0, 1), new GridCoord3(2, 0, 1));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet explicite no-op.
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.NoMovement));
                    Assert.That(result.Description, Does.Contain("origin and destination are identical"));
                }

        [Test]
                public void Resolve_MoveAction_WithTooManySteps_IsRejectedAndStateUnchanged()
                {
                    // Arrange : destination nécessitant plus de pas que le budget autorisé.
                    var state = new BattleState();
                    var actor = new EntityId(3);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(4, 0, 0));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet et rollback position.
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.Description, Does.Contain("MoveActionRejected"));
                    Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
                    Assert.That(position.X, Is.EqualTo(0));
                    Assert.That(position.Y, Is.EqualTo(0));
                    Assert.That(position.Z, Is.EqualTo(0));
                }

        [Test]
                public void Resolve_MoveAction_WhenPathCellIsBlocked_IsRejectedAndStateUnchanged()
                {
                    // Arrange : obstacle terrain directement sur la trajectoire X.
                    var state = new BattleState();
                    var actor = new EntityId(4);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetBlockedPosition(new Position3(1, 0, 0));
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(2, 0, 0));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet "blocked path" et aucune mutation de position.
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.Description, Does.Contain("blocked path"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.BlockedPath));
                    Assert.That(state.TryGetEntityPosition(actor, out var position), Is.True);
                    Assert.That(position.X, Is.EqualTo(0));
                    Assert.That(position.Y, Is.EqualTo(0));
                    Assert.That(position.Z, Is.EqualTo(0));
                }

        [Test]
                public void Resolve_MoveAction_WhenDestinationOccupied_IsRejectedAndStateUnchanged()
                {
                    // Arrange : la case d'arrivée est occupée par une autre entité.
                    var state = new BattleState();
                    var actor = new EntityId(5);
                    var blocker = new EntityId(6);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetEntityPosition(blocker, new Position3(1, 0, 0));
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 0));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet "destination occupied".
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.Description, Does.Contain("destination occupied"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.DestinationOccupied));
                    Assert.That(state.TryGetEntityPosition(actor, out var actorPosition), Is.True);
                    Assert.That(actorPosition.X, Is.EqualTo(0));
                    Assert.That(actorPosition.Y, Is.EqualTo(0));
                    Assert.That(actorPosition.Z, Is.EqualTo(0));
                }

        [Test]
                public void Resolve_MoveAction_WithHighTerrainCost_IsRejectedAndStateUnchanged()
                {
                    // Arrange : coût terrain élevé sur la case d'arrivée -> budget dépassé.
                    var state = new BattleState();
                    var actor = new EntityId(7);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetMovementCost(new Position3(1, 0, 0), 5);
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 0));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : rejet par coût de mouvement.
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.Description, Does.Contain("movement cost exceeded"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.MovementBudgetExceeded));
                    Assert.That(state.TryGetEntityPosition(actor, out var actorPosition), Is.True);
                    Assert.That(actorPosition.X, Is.EqualTo(0));
                    Assert.That(actorPosition.Y, Is.EqualTo(0));
                    Assert.That(actorPosition.Z, Is.EqualTo(0));
                }

        [Test]
                public void Resolve_MoveAction_WithConfiguredTerrainCostWithinBudget_Succeeds()
                {
                    // Arrange : coût terrain custom dans le budget global.
                    var state = new BattleState();
                    var actor = new EntityId(8);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetMovementCost(new Position3(1, 0, 0), 2);
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 0));
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : action acceptée et position mise à jour.
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
                    Assert.That(result.Description, Does.Contain("cost:2"));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.None));
                    Assert.That(state.TryGetEntityPosition(actor, out var actorPosition), Is.True);
                    Assert.That(actorPosition.X, Is.EqualTo(1));
                    Assert.That(actorPosition.Y, Is.EqualTo(0));
                    Assert.That(actorPosition.Z, Is.EqualTo(0));
                }

        [Test]
                public void Resolve_MoveAction_WithPolicyOverride_AllowsLongerMoveBudget()
                {
                    // Arrange : destination normalement refusée avec budget par défaut (coût 4), acceptée avec budget override.
                    var state = new BattleState();
                    var actor = new EntityId(90);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var customPolicy = new MoveActionPolicy(maxMovementCostPerAction: 4, maxVerticalStepPerTile: 1);
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(4, 0, 0), customPolicy);
        
                    // Act.
                    var result = resolver.Resolve(state, action);
        
                    // Assert : l'override data-driven permet la réussite du déplacement.
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.None));
                    Assert.That(state.TryGetEntityPosition(actor, out var actorPosition), Is.True);
                    Assert.That(actorPosition.X, Is.EqualTo(4));
                }

        [Test]
                public void Resolve_MoveAction_WhenActorHasZeroHitPoints_IsRejectedAsActorDefeated()
                {
                    var state = new BattleState();
                    var actor = new EntityId(200);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetEntityHitPoints(actor, 0);
        
                    var resolver = new ActionResolver(new SeededRngService(1));
                    var result = resolver.Resolve(state, new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 0)));
        
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.ActorDefeated));
                }

        [Test]
                public void Resolve_MoveAction_WithInvalidPolicy_IsRejectedWithInvalidPolicyReason()
                {
                    var state = new BattleState();
                    var actor = new EntityId(201);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetEntityHitPoints(actor, 10);
        
                    var invalidPolicy = new MoveActionPolicy(maxMovementCostPerAction: 0, maxVerticalStepPerTile: -1);
                    var resolver = new ActionResolver(new SeededRngService(1));
                    var result = resolver.Resolve(state, new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 0), invalidPolicy));
        
                    Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.InvalidPolicy));
                }

        [Test]
                public void Resolve_MoveAction_WithMovementPoints_ConsumesMovementBudget()
                {
                    var state = new BattleState();
                    var actor = new EntityId(30);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetEntityMovementPoints(actor, maxMovementPoints: 6, currentMovementPoints: 4);
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(2, 0, 0));
        
                    var result = resolver.Resolve(state, action);
        
                    Assert.That(result.Success, Is.True);
                    Assert.That(state.TryGetEntityMovementPoints(actor, out var remaining), Is.True);
                    Assert.That(remaining, Is.EqualTo(2));
                }

        [Test]
                public void Resolve_MoveAction_WithInsufficientMovementPoints_IsRejected()
                {
                    var state = new BattleState();
                    var actor = new EntityId(31);
                    state.SetEntityPosition(actor, new Position3(0, 0, 0));
                    state.SetEntityMovementPoints(actor, maxMovementPoints: 6, currentMovementPoints: 1);
        
                    var resolver = new ActionResolver(new SeededRngService(42));
                    var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(2, 0, 0));
        
                    var result = resolver.Resolve(state, action);
        
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.MovementPointsInsufficient));
                    Assert.That(state.TryGetEntityMovementPoints(actor, out var remaining), Is.True);
                    Assert.That(remaining, Is.EqualTo(1));
                }

    }
}