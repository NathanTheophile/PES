using NUnit.Framework;
using PES.Combat.Actions;
using PES.Combat.Resolution;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;


namespace PES.Tests.EditMode
{
    public class ResolverDeterminismTests
    {
        [Test]
                public void Resolve_TwoActions_StoresOrderedStructuredTicksAndDescriptions()
                {
                    // Arrange : deux acteurs pour enchaîner un déplacement puis une attaque réussie.
                    var state = new BattleState();
                    var mover = new EntityId(50);
                    var attacker = new EntityId(51);
                    var target = new EntityId(52);
        
                    state.SetEntityPosition(mover, new Position3(0, 0, 0));
                    state.SetEntityPosition(attacker, new Position3(0, 0, 1));
                    state.SetEntityPosition(target, new Position3(1, 0, 0));
                    state.SetEntityHitPoints(target, 30);
        
                    var resolver = new ActionResolver(new SeededRngService(3));
        
                    // Act : 1) move valide (sans traverser la case occupée en X->Y->Z) 2) basic attack qui touche avec la seed choisie.
                    var moveResult = resolver.Resolve(state, new MoveAction(mover, new GridCoord3(0, 0, 0), new GridCoord3(0, 1, 0)));
                    var attackResult = resolver.Resolve(state, new BasicAttackAction(attacker, target));
        
                    // Assert : les résultats sont correctement ordonnés et journalisés.
                    Assert.That(moveResult.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
                    Assert.That(attackResult.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
        
                    Assert.That(state.Tick, Is.EqualTo(2));
                    Assert.That(state.StructuredEventLog.Count, Is.EqualTo(2));
        
                    Assert.That(state.StructuredEventLog[0].Tick, Is.EqualTo(0));
                    Assert.That(state.StructuredEventLog[0].Description, Does.Contain("MoveActionResolved"));
                    Assert.That(state.StructuredEventLog[1].Tick, Is.EqualTo(1));
                    Assert.That(state.StructuredEventLog[1].Description, Does.Contain("BasicAttackResolved"));
                }

        [Test]
                public void CreateSnapshot_CapturesStateAndRemainsStableAfterFurtherMutations()
                {
                    // Arrange : état initial avec une entité positionnée et des HP.
                    var state = new BattleState();
                    var entity = new EntityId(60);
                    state.SetEntityPosition(entity, new Position3(2, 3, 1));
                    state.SetEntityHitPoints(entity, 42);
        
                    // Act : on capture un snapshot puis on mutile l'état live.
                    var snapshot = state.CreateSnapshot();
                    state.SetEntityPosition(entity, new Position3(9, 9, 9));
                    state.TryApplyDamage(entity, 10);
                    state.AdvanceTick();
        
                    // Assert : le snapshot reste inchangé et reflète bien l'état au moment de capture.
                    Assert.That(snapshot.Tick, Is.EqualTo(0));
                    Assert.That(snapshot.EntityPositions.Count, Is.EqualTo(1));
                    Assert.That(snapshot.EntityHitPoints.Count, Is.EqualTo(1));
        
                    Assert.That(snapshot.EntityPositions[0].EntityId, Is.EqualTo(entity));
                    Assert.That(snapshot.EntityPositions[0].Position.X, Is.EqualTo(2));
                    Assert.That(snapshot.EntityPositions[0].Position.Y, Is.EqualTo(3));
                    Assert.That(snapshot.EntityPositions[0].Position.Z, Is.EqualTo(1));
        
                    Assert.That(snapshot.EntityHitPoints[0].EntityId, Is.EqualTo(entity));
                    Assert.That(snapshot.EntityHitPoints[0].HitPoints, Is.EqualTo(42));
                }

        [Test]
                public void ApplySnapshot_RestoresPreviousCombatState()
                {
                    // Arrange : état de départ et snapshot de référence.
                    var state = new BattleState();
                    var entity = new EntityId(70);
                    state.SetEntityPosition(entity, new Position3(1, 2, 3));
                    state.SetEntityHitPoints(entity, 50);
                    state.AdvanceTick();
        
                    var snapshot = state.CreateSnapshot();
        
                    // Mutilations postérieures pour simuler une divergence locale.
                    state.SetEntityPosition(entity, new Position3(9, 9, 9));
                    state.TryApplyDamage(entity, 20);
                    state.AdvanceTick();
        
                    // Act : restauration depuis le snapshot.
                    state.ApplySnapshot(snapshot);
        
                    // Assert : tick, position et HP reviennent exactement à l'instant capturé.
                    Assert.That(state.Tick, Is.EqualTo(1));
                    Assert.That(state.TryGetEntityPosition(entity, out var restoredPosition), Is.True);
                    Assert.That(restoredPosition.X, Is.EqualTo(1));
                    Assert.That(restoredPosition.Y, Is.EqualTo(2));
                    Assert.That(restoredPosition.Z, Is.EqualTo(3));
        
                    Assert.That(state.TryGetEntityHitPoints(entity, out var restoredHp), Is.True);
                    Assert.That(restoredHp, Is.EqualTo(50));
                }

        [Test]
                public void Resolve_BasicAttackAction_WithSameSeed_ProducesDeterministicSequence()
                {
                    // Arrange : deux simulations identiques exécutées avec la même seed.
                    var stateA = new BattleState();
                    var stateB = new BattleState();
        
                    var attacker = new EntityId(80);
                    var target = new EntityId(81);
        
                    stateA.SetEntityPosition(attacker, new Position3(0, 0, 1));
                    stateA.SetEntityPosition(target, new Position3(1, 0, 0));
                    stateA.SetEntityHitPoints(target, 40);
        
                    stateB.SetEntityPosition(attacker, new Position3(0, 0, 1));
                    stateB.SetEntityPosition(target, new Position3(1, 0, 0));
                    stateB.SetEntityHitPoints(target, 40);
        
                    var resolverA = new ActionResolver(new SeededRngService(123));
                    var resolverB = new ActionResolver(new SeededRngService(123));
        
                    // Act : même suite d'actions sur deux états clones.
                    var resultA1 = resolverA.Resolve(stateA, new BasicAttackAction(attacker, target));
                    var resultA2 = resolverA.Resolve(stateA, new BasicAttackAction(attacker, target));
        
                    var resultB1 = resolverB.Resolve(stateB, new BasicAttackAction(attacker, target));
                    var resultB2 = resolverB.Resolve(stateB, new BasicAttackAction(attacker, target));
        
                    // Assert : mêmes codes/résultats et même état final.
                    Assert.That(resultA1.Code, Is.EqualTo(resultB1.Code));
                    Assert.That(resultA1.FailureReason, Is.EqualTo(resultB1.FailureReason));
                    Assert.That(resultA1.Description, Is.EqualTo(resultB1.Description));
        
                    Assert.That(resultA2.Code, Is.EqualTo(resultB2.Code));
                    Assert.That(resultA2.FailureReason, Is.EqualTo(resultB2.FailureReason));
                    Assert.That(resultA2.Description, Is.EqualTo(resultB2.Description));
        
                    Assert.That(stateA.TryGetEntityHitPoints(target, out var hpA), Is.True);
                    Assert.That(stateB.TryGetEntityHitPoints(target, out var hpB), Is.True);
                    Assert.That(hpA, Is.EqualTo(hpB));
                }

        [Test]
                public void Resolve_BasicAttackAction_WithSameSeed_ProducesSameStructuredOutcome()
                {
                    var attacker = new EntityId(240);
                    var target = new EntityId(241);
                    var firstState = new BattleState();
                    firstState.SetEntityPosition(attacker, new Position3(0, 0, 0));
                    firstState.SetEntityHitPoints(attacker, 30);
                    firstState.SetEntityPosition(target, new Position3(1, 0, 0));
                    firstState.SetEntityHitPoints(target, 30);
        
                    var secondState = new BattleState();
                    secondState.SetEntityPosition(attacker, new Position3(0, 0, 0));
                    secondState.SetEntityHitPoints(attacker, 30);
                    secondState.SetEntityPosition(target, new Position3(1, 0, 0));
                    secondState.SetEntityHitPoints(target, 30);
        
                    var first = new ActionResolver(new SeededRngService(99)).Resolve(firstState, new BasicAttackAction(attacker, target));
                    var second = new ActionResolver(new SeededRngService(99)).Resolve(secondState, new BasicAttackAction(attacker, target));
        
                    Assert.That(first.Code, Is.EqualTo(second.Code));
                    Assert.That(first.FailureReason, Is.EqualTo(second.FailureReason));
                    Assert.That(first.Payload.HasValue, Is.EqualTo(second.Payload.HasValue));
                    if (first.Payload.HasValue && second.Payload.HasValue)
                    {
                        Assert.That(first.Payload.Value.Kind, Is.EqualTo(second.Payload.Value.Kind));
                        Assert.That(first.Payload.Value.Value1, Is.EqualTo(second.Payload.Value.Value1));
                        Assert.That(first.Payload.Value.Value2, Is.EqualTo(second.Payload.Value.Value2));
                        Assert.That(first.Payload.Value.Value3, Is.EqualTo(second.Payload.Value.Value3));
                    }
                }

    }
}