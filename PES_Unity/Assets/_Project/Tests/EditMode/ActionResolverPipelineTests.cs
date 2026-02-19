// Utilité : ce script valide que les actions passent par le pipeline métier de résolution
// et que les mutations de BattleState/journalisation se comportent comme attendu.
using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode du pipeline orienté commandes via ActionResolver.
    /// </summary>
    public class ActionResolverPipelineTests
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
            Assert.That(state.TryGetEntityPosition(actor, out var actorPosition), Is.True);
            Assert.That(actorPosition.X, Is.EqualTo(1));
            Assert.That(actorPosition.Y, Is.EqualTo(0));
            Assert.That(actorPosition.Z, Is.EqualTo(0));
        }

        [Test]
        public void Resolve_BasicAttackAction_InRangeWithLineOfSight_AppliesDamage()
        {
            // Arrange : attaquant/cible valides, en portée et avec HP configurés.
            var state = new BattleState();
            var attacker = new EntityId(10);
            var target = new EntityId(11);
            state.SetEntityPosition(attacker, new Position3(0, 0, 1));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(target, 30);

            // Seed choisie pour obtenir un jet qui touche.
            var resolver = new ActionResolver(new SeededRngService(3));
            var action = new BasicAttackAction(attacker, target);

            // Act.
            var result = resolver.Resolve(state, action);

            // Assert : succès, dégâts appliqués et pipeline actif.
            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
            Assert.That(result.Description, Does.Contain("BasicAttackResolved"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog[0].Code, Is.EqualTo(result.Code));

            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.LessThan(30));
        }

        [Test]
        public void Resolve_BasicAttackAction_OutOfRange_IsRejectedWithoutDamage()
        {
            // Arrange : cible trop éloignée sur XY.
            var state = new BattleState();
            var attacker = new EntityId(20);
            var target = new EntityId(21);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(5, 0, 0));
            state.SetEntityHitPoints(target, 30);

            var resolver = new ActionResolver(new SeededRngService(42));
            var action = new BasicAttackAction(attacker, target);

            // Act.
            var result = resolver.Resolve(state, action);

            // Assert : rejet propre, HP inchangés mais tick/log avancent via resolver.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.Description, Does.Contain("out of range"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.EventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog[0].Code, Is.EqualTo(result.Code));

            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(30));
        }

        [Test]
        public void Resolve_BasicAttackAction_WhenHitRollFails_ReturnsMissedWithoutDamage()
        {
            // Arrange : attaquant/cible valides ; RNG forcée pour rater le jet de précision.
            var state = new BattleState();
            var attacker = new EntityId(40);
            var target = new EntityId(41);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(target, 30);

            // Premier tirage = 95 (miss), le second n'est pas consommé dans ce scénario.
            var resolver = new ActionResolver(new SequenceRngService(95, 0));
            var action = new BasicAttackAction(attacker, target);

            // Act.
            var result = resolver.Resolve(state, action);

            // Assert : issue "missed", aucun dégât, mais pipeline/log actifs.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Missed));
            Assert.That(result.Description, Does.Contain("BasicAttackMissed"));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog[0].Code, Is.EqualTo(ActionResolutionCode.Missed));

            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(30));
        }

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

            // Act : 1) move valide 2) basic attack qui touche avec la seed choisie.
            var moveResult = resolver.Resolve(state, new MoveAction(mover, new GridCoord3(0, 0, 0), new GridCoord3(1, 0, 0)));
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
        public void Resolve_BasicAttackAction_WithHugeVerticalDelta_IsRejectedByLineOfSight()
        {
            // Arrange : cible en portée XY mais avec différence de hauteur trop importante.
            var state = new BattleState();
            var attacker = new EntityId(30);
            var target = new EntityId(31);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 4));
            state.SetEntityHitPoints(target, 30);

            var resolver = new ActionResolver(new SeededRngService(42));
            var action = new BasicAttackAction(attacker, target);

            // Act.
            var result = resolver.Resolve(state, action);

            // Assert : rejet LOS et HP inchangés.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.Description, Does.Contain("line of sight blocked"));
            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(30));
        }

        // Utilité : faux RNG de test pour forcer des séquences déterministes contrôlées.
        private sealed class SequenceRngService : IRngService
        {
            private readonly int[] _values;
            private int _index;

            public SequenceRngService(params int[] values)
            {
                _values = values;
                _index = 0;
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                var value = _values[_index < _values.Length ? _index : _values.Length - 1];
                _index++;
                return value;
            }
        }
    }
}
