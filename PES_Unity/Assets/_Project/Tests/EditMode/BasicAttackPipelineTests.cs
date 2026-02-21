using NUnit.Framework;
using PES.Combat.Actions;
using PES.Combat.Resolution;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;


namespace PES.Tests.EditMode
{
    public class BasicAttackPipelineTests
    {
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
                public void Resolve_BasicAttackAction_UsesRpgElementalFormulaDamage()
                {
                    var state = new BattleState();
                    var attacker = new EntityId(880);
                    var target = new EntityId(881);
                    state.SetEntityPosition(attacker, new Position3(0, 0, 0));
                    state.SetEntityPosition(target, new Position3(1, 0, 0));
                    state.SetEntityHitPoints(attacker, 100);
                    state.SetEntityHitPoints(target, 100);
        
                    state.SetEntityRpgStats(attacker, new CombatantRpgStats(
                        actionPoints: 6,
                        movementPoints: 6,
                        range: 1,
                        elevation: 1,
                        summonCapacity: 1,
                        hitPoints: 100,
                        assiduity: 0,
                        rapidity: 0,
                        criticalChance: 0,
                        criticalDamage: 0,
                        criticalResistance: 0,
                        attack: new DamageElementValues(20, 0, 0, 0, 0, 0),
                        power: new DamageElementValues(100, 100, 100, 100, 100, 100),
                        defense: default,
                        resistance: default));
        
                    state.SetEntityRpgStats(target, new CombatantRpgStats(
                        actionPoints: 6,
                        movementPoints: 6,
                        range: 1,
                        elevation: 1,
                        summonCapacity: 1,
                        hitPoints: 100,
                        assiduity: 0,
                        rapidity: 0,
                        criticalChance: 0,
                        criticalDamage: 0,
                        criticalResistance: 0,
                        attack: default,
                        power: new DamageElementValues(100, 100, 100, 100, 100, 100),
                        defense: new DamageElementValues(5, 0, 0, 0, 0, 0),
                        resistance: default));
        
                    var policy = new BasicAttackActionPolicy(
                        minRange: 1,
                        maxRange: 2,
                        maxLineOfSightDelta: 2,
                        resolutionPolicy: new BasicAttackResolutionPolicy(baseDamage: 10, baseHitChance: 100),
                        damageElement: DamageElement.Blunt,
                        baseCriticalChance: 0);
        
                    var resolver = new ActionResolver(new SeededRngService(11));
                    var result = resolver.Resolve(state, new BasicAttackAction(attacker, target, policy));
        
                    Assert.That(result.Success, Is.True);
                    Assert.That(state.TryGetEntityHitPoints(target, out var hp), Is.True);
                    Assert.That(hp, Is.EqualTo(75));
                }

    }
}