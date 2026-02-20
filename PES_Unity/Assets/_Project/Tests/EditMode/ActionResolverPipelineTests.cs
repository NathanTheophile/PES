// Utilité : ce script valide que les actions passent par le pipeline métier de résolution
// et que les mutations de BattleState/journalisation se comportent comme attendu.
using NUnit.Framework;
using PES.Combat.Actions;
using PES.Combat.Resolution;
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
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.None));
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
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.OutOfRange));
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
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.HitRollMissed));
            Assert.That(state.Tick, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog[0].Code, Is.EqualTo(ActionResolutionCode.Missed));

            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(30));
        }


        [Test]
        public void Resolve_BasicAttackAction_WithPolicyOverride_RejectsWhenTargetOutOfCustomRange()
        {
            // Arrange : cible à distance 2, acceptable par défaut mais hors portée avec maxRange override=1.
            var state = new BattleState();
            var attacker = new EntityId(91);
            var target = new EntityId(92);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(2, 0, 0));
            state.SetEntityHitPoints(target, 30);

            var resolver = new ActionResolver(new SeededRngService(42));
            var customPolicy = new BasicAttackActionPolicy(
                minRange: 1,
                maxRange: 1,
                maxLineOfSightDelta: 2,
                resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 12, baseHitChance: 80));

            // Act.
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, target, customPolicy));

            // Assert : rejet piloté par la config de portée.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.OutOfRange));
        }

        [Test]
        public void Resolve_BasicAttackAction_WithPolicyOverride_AppliesCustomDamageProfile()
        {
            // Arrange : même action avec profil de dégâts custom (baseDamage=20, hitChance=95).
            var state = new BattleState();
            var attacker = new EntityId(93);
            var target = new EntityId(94);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(target, 40);

            var resolver = new ActionResolver(new SequenceRngService(0, 0));
            var customPolicy = new BasicAttackActionPolicy(
                minRange: 1,
                maxRange: 2,
                maxLineOfSightDelta: 2,
                resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 20, baseHitChance: 95));

            // Act.
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, target, customPolicy));

            // Assert : hit garanti + dégâts déterministes custom (20 + variance 0).
            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.None));
            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(20));
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
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.LineOfSightBlocked));
            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(30));
        }

        [Test]
        public void Resolve_BasicAttackAction_WithTerrainObstacle_IsRejectedByLineOfSight()
        {
            // Arrange : obstacle terrain placé entre l'attaquant et la cible.
            var state = new BattleState();
            var attacker = new EntityId(32);
            var target = new EntityId(33);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(2, 0, 0));
            state.SetEntityHitPoints(target, 30);
            state.SetBlockedPosition(new Position3(1, 0, 0));

            var resolver = new ActionResolver(new SeededRngService(42));
            var action = new BasicAttackAction(attacker, target);

            // Act.
            var result = resolver.Resolve(state, action);

            // Assert : rejet LOS sur obstacle intermédiaire, HP inchangés.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.Description, Does.Contain("line of sight blocked"));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.LineOfSightBlocked));
            Assert.That(state.TryGetEntityHitPoints(target, out var remainingHp), Is.True);
            Assert.That(remainingHp, Is.EqualTo(30));
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
        public void Resolve_BasicAttackAction_WhenTargetMissingHitPoints_ReturnsStructuredReason()
        {
            // Arrange : positions valides mais HP de la cible absents de BattleState.
            var state = new BattleState();
            var attacker = new EntityId(82);
            var target = new EntityId(83);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(1, 0, 0));

            var resolver = new ActionResolver(new SeededRngService(42));

            // Act.
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, target));

            // Assert : rejet explicite et raison normalisée exploitable.
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.MissingHitPoints));
            Assert.That(result.Description, Does.Contain("missing hit points"));
        }


        [Test]
        public void Resolve_BasicAttackAction_WhenTargetTooClose_IsRejectedWithTooCloseReason()
        {
            // Arrange : cible sur la même case XY que l'attaquant (distance 0).
            var state = new BattleState();
            var attacker = new EntityId(84);
            var target = new EntityId(85);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityPosition(target, new Position3(0, 0, 0));
            state.SetEntityHitPoints(target, 30);

            var resolver = new ActionResolver(new SeededRngService(42));

            // Act.
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, target));

            // Assert : rejet structuré "too close".
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.TooClose));
            Assert.That(result.Description, Does.Contain("target too close"));
        }

        [Test]
        public void Resolve_BasicAttackAction_HighGroundDealsMoreDamageThanLowGround_WithSameRngSequence()
        {
            // Arrange : même RNG sur deux simulations, seule la hauteur relative change.
            var attacker = new EntityId(86);
            var target = new EntityId(87);

            var highGroundState = new BattleState();
            highGroundState.SetEntityPosition(attacker, new Position3(0, 0, 2));
            highGroundState.SetEntityPosition(target, new Position3(1, 0, 0));
            highGroundState.SetEntityHitPoints(target, 40);

            var lowGroundState = new BattleState();
            lowGroundState.SetEntityPosition(attacker, new Position3(0, 0, 0));
            lowGroundState.SetEntityPosition(target, new Position3(1, 0, 2));
            lowGroundState.SetEntityHitPoints(target, 40);

            // 60 => touche dans les deux cas ; 1 => même variance dégâts.
            var highResolver = new ActionResolver(new SequenceRngService(60, 1));
            var lowResolver = new ActionResolver(new SequenceRngService(60, 1));

            // Act.
            var highResult = highResolver.Resolve(highGroundState, new BasicAttackAction(attacker, target));
            var lowResult = lowResolver.Resolve(lowGroundState, new BasicAttackAction(attacker, target));

            // Assert : les deux touchent mais le bonus de hauteur augmente les dégâts.
            Assert.That(highResult.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
            Assert.That(lowResult.Code, Is.EqualTo(ActionResolutionCode.Succeeded));

            Assert.That(highGroundState.TryGetEntityHitPoints(target, out var highHp), Is.True);
            Assert.That(lowGroundState.TryGetEntityHitPoints(target, out var lowHp), Is.True);
            Assert.That(highHp, Is.LessThan(lowHp));

            Assert.That(highResult.Description, Does.Contain("hBonus:4"));
            Assert.That(lowResult.Description, Does.Contain("hBonus:-4"));
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
        public void Resolve_BasicAttackAction_WhenSelfTargeting_IsRejectedWithSelfTargetingReason()
        {
            var state = new BattleState();
            var attacker = new EntityId(210);
            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityHitPoints(attacker, 20);

            var resolver = new ActionResolver(new SeededRngService(1));
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, attacker));

            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.SelfTargeting));
        }

        [Test]
        public void Resolve_BasicAttackAction_WhenIntermediateEntityBlocksLineOfSight_IsRejected()
        {
            var state = new BattleState();
            var attacker = new EntityId(220);
            var blocker = new EntityId(221);
            var target = new EntityId(222);

            state.SetEntityPosition(attacker, new Position3(0, 0, 0));
            state.SetEntityHitPoints(attacker, 25);
            state.SetEntityPosition(blocker, new Position3(1, 0, 0));
            state.SetEntityHitPoints(blocker, 25);
            state.SetEntityPosition(target, new Position3(2, 0, 0));
            state.SetEntityHitPoints(target, 25);

            var policy = new BasicAttackActionPolicy(
                minRange: 1,
                maxRange: 3,
                maxLineOfSightDelta: 1,
                resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 10, baseHitChance: 100));

            var resolver = new ActionResolver(new SeededRngService(1));
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, target, policy));

            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.LineOfSightBlocked));
        }

        [Test]
        public void Resolve_BasicAttackAction_Success_ExposesStructuredPayloadAndMirrorsEventLog()
        {
            var state = new BattleState();
            var attacker = new EntityId(230);
            var target = new EntityId(231);
            state.SetEntityPosition(attacker, new Position3(0, 0, 1));
            state.SetEntityHitPoints(attacker, 30);
            state.SetEntityPosition(target, new Position3(1, 0, 0));
            state.SetEntityHitPoints(target, 40);

            var policy = new BasicAttackActionPolicy(
                minRange: 1,
                maxRange: 2,
                maxLineOfSightDelta: 2,
                resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 10, baseHitChance: 95));

            var resolver = new ActionResolver(new SequenceRngService(0, 2));
            var result = resolver.Resolve(state, new BasicAttackAction(attacker, target, policy));

            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Succeeded));
            Assert.That(result.Payload.HasValue, Is.True);
            Assert.That(result.Payload.Value.Kind, Is.EqualTo("AttackResolved"));
            Assert.That(state.StructuredEventLog.Count, Is.EqualTo(1));
            Assert.That(state.StructuredEventLog[0].Payload.HasValue, Is.True);
            Assert.That(state.StructuredEventLog[0].Payload.Value.Kind, Is.EqualTo("AttackResolved"));
            Assert.That(state.StructuredEventLog[0].FailureReason, Is.EqualTo(ActionFailureReason.None));
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
