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
