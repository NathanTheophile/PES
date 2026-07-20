using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class PassiveHealthObservationTests
    {
        private readonly List<Object> _Objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int lIndex = _Objects.Count - 1; lIndex >= 0; lIndex--)
                Object.DestroyImmediate(_Objects[lIndex]);
            _Objects.Clear();
        }

        [Test]
        public void BeginTurnSelectsLowestHealthPercentageThenUnitIdAndOwnsMarker()
        {
            StateDefinition lMarker = CreateState("marker", pPassiveMarker: true);
            PassiveDefinition lPassive = CreateObservedPassive(
                "observer", lMarker, CreateState("reaction"), PassiveHealthLossReactionTarget.MarkedUnit, true);
            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit("owner", Team.TeamA, pPassive: lPassive), 4, 4, 0),
                Spawn(CreateUnit("enemy", Team.TeamB), 7, 7, 0),
                Spawn(CreateUnit("ally-a", Team.TeamA), 2, 2, 1),
                Spawn(CreateUnit("ally-b", Team.TeamA), 3, 3, 2));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lOwner);
            lBattle.TryGetUnit(new UnitId(3), out UnitRuntime lAllyA);
            lBattle.TryGetUnit(new UnitId(4), out UnitRuntime lAllyB);
            lAllyA.ApplyDirectHealthLoss(50);
            lAllyB.ApplyDirectHealthLoss(50);

            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleStateRuntime lMarkerRuntime = FindState(lAllyA, lMarker);
            Assert.That(lMarkerRuntime, Is.Not.Null);
            Assert.That(lMarkerRuntime.SourceUnitId, Is.EqualTo(lOwner.Id));
            Assert.That(FindState(lAllyB, lMarker), Is.Null);
        }

        [Test]
        public void CompleteSkillResolutionTriggersSolidarityOnlyOnceAndExcludesMarkedUnit()
        {
            StateDefinition lMarker = CreateState("solidarity-marker", pPassiveMarker: true);
            StateDefinition lMobility = CreateState("shared-mobility", pDuration: 1, pMaxStacks: 2, pMobility: 1);
            PassiveDefinition lPassive = CreateObservedPassive(
                "solidarity", lMarker, lMobility,
                PassiveHealthLossReactionTarget.MarkedUnitAlliesExceptMarked, false);
            SkillDefinition lMultiImpactSkill = CreateDamagePushSkill("multi-impact", 10, 1);
            UnitDefinition lEnemyDefinition = CreateUnit("enemy", Team.TeamB, new[] { lMultiImpactSkill });
            SetField(lEnemyDefinition, "_PushDamageBonus", 5);
            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit("owner", Team.TeamA, pPassive: lPassive), 4, 4, 0),
                Spawn(lEnemyDefinition, 1, 0, 0),
                Spawn(CreateUnit("marked", Team.TeamA), 0, 0, 1),
                Spawn(CreateUnit("beneficiary", Team.TeamA), 5, 5, 2));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lOwner);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            lBattle.TryGetUnit(new UnitId(3), out UnitRuntime lMarked);
            lBattle.TryGetUnit(new UnitId(4), out UnitRuntime lBeneficiary);
            lMarked.ApplyDirectHealthLoss(50);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lBattle.EndTurn(lOwner.Id).IsSuccess, Is.True);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(
                lEnemy.Id,
                new SkillId(lMultiImpactSkill.Id),
                SkillTarget.ForCell(lMarked.Position));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            Assert.That(lMarked.CurrentHealth, Is.EqualTo(30), "Primary and collision damage must both resolve.");
            Assert.That(lOwner.GetStateStacks(lMobility), Is.EqualTo(1));
            Assert.That(lBeneficiary.GetStateStacks(lMobility), Is.EqualTo(1));
            Assert.That(lMarked.GetStateStacks(lMobility), Is.Zero);
        }

        [Test]
        public void StateTicksKeepApplierIdentityAndAggregateBySourceForPassiveReaction()
        {
            StateDefinition lMarker = CreateState("aegis-marker", pPassiveMarker: true);
            StateDefinition lResistance = CreateState("deep-resistance", pMaxStacks: 3, pResistance: 10);
            StateDefinition lPoisonA = CreateState("poison-a", pDuration: 2, pBeginTurnDamage: 4);
            StateDefinition lPoisonB = CreateState("poison-b", pDuration: 2, pBeginTurnDamage: 6);
            PassiveDefinition lPassive = CreateObservedPassive(
                "aegis", lMarker, lResistance, PassiveHealthLossReactionTarget.MarkedUnit, true);
            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit("owner", Team.TeamA, pPassive: lPassive), 4, 4, 0),
                Spawn(CreateUnit("enemy", Team.TeamB), 7, 7, 0),
                Spawn(CreateUnit("marked", Team.TeamA), 2, 2, 1));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lOwner);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            lBattle.TryGetUnit(new UnitId(3), out UnitRuntime lMarked);
            lMarked.ApplyDirectHealthLoss(50);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lMarked.TryApplyState(lPoisonA, 1, 2, lEnemy.Id, lEnemy.Team, "poison"), Is.True);
            Assert.That(lMarked.TryApplyState(lPoisonB, 1, 2, lEnemy.Id, lEnemy.Team, "poison"), Is.True);
            Assert.That(FindState(lMarked, lPoisonA)?.SourceUnitId, Is.EqualTo(lEnemy.Id));
            Assert.That(lBattle.EndTurn(lOwner.Id).IsSuccess, Is.True);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lBattle.EndTurn(lEnemy.Id).IsSuccess, Is.True);

            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lMarked.CurrentHealth, Is.EqualTo(40));
            Assert.That(lMarked.GetStateStacks(lResistance), Is.EqualTo(1), "Two states from one source are one tick resolution.");
            BattleStateRuntime lResistanceRuntime = FindState(lMarked, lResistance);
            Assert.That(lResistanceRuntime?.SourceUnitId, Is.EqualTo(lOwner.Id));

            Assert.That(lBattle.EndTurn(lMarked.Id).IsSuccess, Is.True);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lMarked.GetStateStacks(lResistance), Is.Zero, "Owner renewal must clean its reaction state.");
        }

        [Test]
        public void SourceScopedStateKeepsOneCappedInstancePerApplier()
        {
            StateDefinition lState = CreateState("source-scoped", pDuration: 2, pMaxStacks: 1);
            SetField(lState, "_UsesSourceScopedInstances", true);
            UnitDefinition lDefinition = CreateUnit("target", Team.TeamA);
            UnitRuntime lTarget = new UnitRuntime(new UnitId(1), lDefinition, new GridCoord(0, 0));

            Assert.That(lTarget.TryApplyState(lState, 1, 2, new UnitId(10), Team.TeamB, "first"), Is.True);
            Assert.That(lTarget.TryApplyState(lState, 1, 2, new UnitId(11), Team.TeamB, "second"), Is.True);

            Assert.That(lTarget.GetStateStacks(lState), Is.EqualTo(2));
            Assert.That(lTarget.ActiveStates, Has.Exactly(1).Matches<BattleStateRuntime>(pState => pState.SourceUnitId == new UnitId(10)));
            Assert.That(lTarget.ActiveStates, Has.Exactly(1).Matches<BattleStateRuntime>(pState => pState.SourceUnitId == new UnitId(11)));
            Assert.That(
                lTarget.GetStateByKey("temporary::source-scoped::source:10::context:first"),
                Is.Not.Null,
                "The source-scoped key must be stable and inspectable.");

            Assert.That(lTarget.RemoveStatesBySource(new UnitId(10), "first", lState), Is.EqualTo(1));
            Assert.That(lTarget.GetStateStacks(lState), Is.EqualTo(1));
            Assert.That(lTarget.ActiveStates[0].SourceUnitId, Is.EqualTo(new UnitId(11)));
        }

        [Test]
        public void StateSourceIdentityChangesBattleChecksum()
        {
            StateDefinition lState = CreateState("checksum-source", pDuration: 2);
            SetField(lState, "_UsesSourceScopedInstances", true);
            UnitDefinition lDefinition = CreateUnit("checksum-target", Team.TeamA);
            UnitDefinition lEnemyDefinition = CreateUnit("checksum-enemy", Team.TeamB);
            IBattleService lFirstBattle = CreateBattle(
                Spawn(lDefinition, 0, 0, 0),
                Spawn(lEnemyDefinition, 7, 7, 0));
            IBattleService lSecondBattle = CreateBattle(
                Spawn(lDefinition, 0, 0, 0),
                Spawn(lEnemyDefinition, 7, 7, 0));
            lFirstBattle.TryGetUnit(new UnitId(1), out UnitRuntime lFirstTarget);
            lSecondBattle.TryGetUnit(new UnitId(1), out UnitRuntime lSecondTarget);
            lFirstTarget.TryApplyState(lState, 1, 2, new UnitId(10), Team.TeamB, "same-context");
            lSecondTarget.TryApplyState(lState, 1, 2, new UnitId(11), Team.TeamB, "same-context");

            Assert.That(BattleStateChecksum.Compute(lFirstBattle), Is.Not.EqualTo(BattleStateChecksum.Compute(lSecondBattle)));
        }

        private PassiveDefinition CreateObservedPassive(
            string pId,
            StateDefinition pMarker,
            StateDefinition pReaction,
            PassiveHealthLossReactionTarget pTargets,
            bool pClearOnOwnerTurn)
        {
            PassiveDefinition lPassive = Track(ScriptableObject.CreateInstance<PassiveDefinition>());
            SetField(lPassive, "_Id", pId);
            SetField(lPassive, "_AutomaticTargetSelection", PassiveAutomaticTargetSelection.LowestHealthPercentageAlly);
            SetField(lPassive, "_AutomaticTargetMarker", pMarker);
            SetField(lPassive, "_AutomaticTargetUnitType", UnitTargetType.CharactersOnly);
            SetField(lPassive, "_HealthLossSourceRule", PassiveHealthLossSourceRule.EnemiesOnly);
            SetField(lPassive, "_HealthLossReactionTarget", pTargets);
            SetField(lPassive, "_HealthLossReactionState", pReaction);
            SetField(lPassive, "_HealthLossReactionStateStacks", 1);
            SetField(lPassive, "_ClearReactionStateOnOwnerBeginTurn", pClearOnOwnerTurn);
            return lPassive;
        }

        private SkillDefinition CreateDamagePushSkill(string pId, int pDamage, int pPushDistance)
        {
            SkillDefinition lSkill = Track(ScriptableObject.CreateInstance<SkillDefinition>());
            SetField(lSkill, "_Id", pId);
            SetField(lSkill, "_Category", SkillCategory.MeleeAtk);
            SetField(lSkill, "_PrimaryEffectType", SkillPrimaryEffectType.Damage);
            SetField(lSkill, "_AdditionalEffectType", SkillAdditionalEffectType.Push);
            SetField(lSkill, "_Power", pDamage);
            SetField(lSkill, "_PushDistance", pPushDistance);
            SetField(lSkill, "_RangeMin", 1);
            SetField(lSkill, "_RangeMax", 1);
            SetField(lSkill, "_TargetType", SkillTargetType.Unit);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.EnemiesOnly);
            SetField(lSkill, "_EnergyCost", 0);
            SetField(lSkill, "_CanAffectCaster", false);
            return lSkill;
        }

        private UnitDefinition CreateUnit(
            string pId,
            Team pTeam,
            IReadOnlyList<SkillDefinition> pSkills = null,
            PassiveDefinition pPassive = null)
        {
            UnitDefinition lUnit = Track(ScriptableObject.CreateInstance<UnitDefinition>());
            SetField(lUnit, "_Id", pId);
            SetField(lUnit, "_Team", pTeam);
            SetField(lUnit, "_MaxHealth", 100);
            SetField(lUnit, "_MobilityPerTurn", 3);
            SetField(lUnit, "_EnergyPerTurn", 10);
            SetField(lUnit, "_Velocity", pTeam == Team.TeamA ? 100 : 1);
            SetField(lUnit, "_Skills", pSkills != null ? new List<SkillDefinition>(pSkills) : new List<SkillDefinition>());
            SetField(lUnit, "_Passives", pPassive != null ? new List<PassiveDefinition> { pPassive } : new List<PassiveDefinition>());
            SetField(lUnit, "_DefaultPassive", pPassive);
            return lUnit;
        }

        private StateDefinition CreateState(
            string pId,
            int pDuration = 0,
            int pMaxStacks = 1,
            int pMobility = 0,
            int pResistance = 0,
            int pBeginTurnDamage = 0,
            bool pPassiveMarker = false)
        {
            StateDefinition lState = Track(ScriptableObject.CreateInstance<StateDefinition>());
            SetField(lState, "_Id", pId);
            SetField(lState, "_DurationTurns", pDuration);
            SetField(lState, "_MaxStacks", pMaxStacks);
            SetField(lState, "_MobilityModifierPerStack", pMobility);
            SetField(lState, "_GeneralResistancePercentPerStack", pResistance);
            SetField(lState, "_BeginTurnDamagePerStack", pBeginTurnDamage);
            SetField(lState, "_IsPassiveMarker", pPassiveMarker);
            return lState;
        }

        private IBattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime(
                "passive-health-tests", "Passive Health Tests", 8, 8, 1, null, pSpawns));
            IBattleService lBattle = BattleRuntimeCompositionRoot.CreateDefaultBattleService();
            lBattle.Initialize(lScenario);
            return lBattle;
        }

        private static UnitSpawnDefinition Spawn(UnitDefinition pUnit, int pX, int pY, int pSlot) => new UnitSpawnDefinition
        {
            Unit = pUnit,
            StartCoordinate = new SerializableGridCoord(pX, pY),
            TeamSlotIndex = pSlot
        };

        private static BattleStateRuntime FindState(UnitRuntime pUnit, StateDefinition pState)
        {
            for (int lIndex = 0; lIndex < pUnit.ActiveStates.Count; lIndex++)
            {
                if (pUnit.ActiveStates[lIndex]?.Definition == pState)
                    return pUnit.ActiveStates[lIndex];
            }

            return null;
        }

        private T Track<T>(T pObject) where T : Object
        {
            _Objects.Add(pObject);
            return pObject;
        }

        private static void SetField<TTarget, TValue>(TTarget pTarget, string pName, TValue pValue)
        {
            FieldInfo lField = typeof(TTarget).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lField, Is.Not.Null, $"Missing field {typeof(TTarget).Name}.{pName}");
            lField.SetValue(pTarget, pValue);
        }
    }
}
