using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class GenericCharacterSystemsTests
    {
        private readonly List<Object> _ObjectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int lIndex = _ObjectsToDestroy.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_ObjectsToDestroy[lIndex] != null)
                    Object.DestroyImmediate(_ObjectsToDestroy[lIndex]);
            }

            _ObjectsToDestroy.Clear();
        }

        [Test]
        public void VariantUsesEffectiveRulesAndBaseUsageIdentityThenConsumesEnabler()
        {
            StateDefinition lReplacement = CreateState("replacement");
            StateDefinition lEnabler = CreateState("enabler");
            SetField(lEnabler, "_EnablesSkillVariant", true);
            SetField(lEnabler, "_VariantConsumedReplacementState", lReplacement);

            SkillDefinition lVariant = CreateSkill("variant", 20, 4, 2);
            SetField(lVariant, "_TargetType", SkillTargetType.Unit);
            SetField(lVariant, "_TargetRelation", SkillTargetRelation.EnemiesOnly);
            SkillDefinition lBase = CreateSkill("base", 1, 1, 1);
            SetField(lBase, "_Variant", lVariant);
            SetField(lBase, "_CooldownTurns", 2);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lBase }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 3, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            lActor.SetPersistentState("resource", lEnabler);
            SkillResolutionReport lReport = null;
            lBattle.SkillResolved += pReport => lReport = pReport;
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(lActor.Id, new SkillId(lBase.Id), SkillTarget.ForCell(lEnemy.Position));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            Assert.That(lEnemy.CurrentHealth, Is.EqualTo(80));
            Assert.That(lActor.RemainingEnergy, Is.EqualTo(8));
            Assert.That(lActor.HasState(lEnabler), Is.False);
            Assert.That(lActor.HasState(lReplacement), Is.True);
            Assert.That(lActor.GetRemainingCooldown(lBase), Is.EqualTo(3));
            Assert.That(lActor.GetRemainingCooldown(lVariant), Is.Zero);
            Assert.That(lReport?.BaseSkill, Is.SameAs(lBase));
            Assert.That(lReport?.EffectiveSkill, Is.SameAs(lVariant));
            Assert.That(lReport?.GetHealthLost(lEnemy.Id), Is.EqualTo(20));
        }

        [Test]
        public void FailedVariantValidationDoesNotConsumeEnabler()
        {
            StateDefinition lEnabler = CreateState("enabler");
            SetField(lEnabler, "_EnablesSkillVariant", true);
            SkillDefinition lVariant = CreateSkill("variant", 20, 1, 0);
            SetField(lVariant, "_TargetType", SkillTargetType.Unit);
            SetField(lVariant, "_TargetRelation", SkillTargetRelation.EnemiesOnly);
            SkillDefinition lBase = CreateSkill("base", 1, 1, 0);
            SetField(lBase, "_Variant", lVariant);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lBase }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 3, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lActor.SetPersistentState("resource", lEnabler);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lValidation = lBattle.ValidateSkill(lActor.Id, new SkillId(lBase.Id), SkillTarget.ForCell(new GridCoord(3, 0)));

            Assert.That(lValidation.IsSuccess, Is.False);
            Assert.That(lActor.HasState(lEnabler), Is.True);
        }

        [Test]
        public void OffensivePassiveProgressesOnceForAoeAndCanIgnoreOwnerVariant()
        {
            StateDefinition lLevelOne = CreateState("level-one");
            StateDefinition lLevelTwo = CreateState("level-two");
            StateProgressionDefinition lProgression = CreateProgression("treasure", lLevelOne, lLevelTwo);
            PassiveDefinition lPassive = CreatePassive(
                "offense",
                PassiveTrigger.OwnerDealtSkillDamageToEnemy,
                lProgression,
                false);
            SkillDefinition lSkill = CreateSkill("aoe", 10, 3, 0);
            SetField(lSkill, "_AoeShape", SkillAoeShape.Circle);
            SetField(lSkill, "_AoeSize", 1);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.EnemiesOnly);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lSkill }, lPassive), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 2, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 2, 1, 1));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(new GridCoord(2, 0))).IsSuccess, Is.True);
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(0));

            StateDefinition lEnabler = CreateState("variant-enabler");
            SetField(lEnabler, "_EnablesSkillVariant", true);
            SetField(lEnabler, "_VariantConsumedReplacementState", lLevelOne);
            SkillDefinition lVariant = CreateSkill("variant", 10, 3, 0);
            SetField(lVariant, "_TargetRelation", SkillTargetRelation.EnemiesOnly);
            SetField(lSkill, "_Variant", lVariant);
            lActor.SetPersistentState("variant-resource", lEnabler);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(new GridCoord(2, 0))).IsSuccess, Is.True);
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(0));
        }

        [Test]
        public void DefensivePassiveOnlyProgressesFromEnemySkillHealthLoss()
        {
            StateProgressionDefinition lProgression = CreateProgression("defense", CreateState("d1"), CreateState("d2"));
            PassiveDefinition lPassive = CreatePassive(
                "defense-passive",
                PassiveTrigger.OwnerTookSkillDamageFromEnemy,
                lProgression,
                true);
            SkillDefinition lAttack = CreateSkill("attack", 12, 2, 0);
            SetField(lAttack, "_TargetType", SkillTargetType.Unit);
            SetField(lAttack, "_TargetRelation", SkillTargetRelation.EnemiesOnly);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lAttack }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1, null, lPassive), 1, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lDefender);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lAttack.Id), SkillTarget.ForCell(lDefender.Position)).IsSuccess, Is.True);
            Assert.That(lDefender.GetStateProgressionIndex(lProgression), Is.EqualTo(0));
        }

        [Test]
        public void PassiveDoesNotProgressWhenSkillCausesNoHealthLoss()
        {
            StateProgressionDefinition lProgression = CreateProgression("no-damage", CreateState("level"));
            PassiveDefinition lPassive = CreatePassive(
                "offense-passive",
                PassiveTrigger.OwnerDealtSkillDamageToEnemy,
                lProgression,
                true);
            SkillDefinition lSkill = CreateSkill("zero-damage", 0, 2, 0);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.EnemiesOnly);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lSkill }, lPassive), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 1, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(new GridCoord(1, 0))).IsSuccess, Is.True);
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(-1));
        }

        [Test]
        public void UtilitySkillAdvancesActivePassiveSeveralLevelsAndStopsAtLastLevel()
        {
            StateProgressionDefinition lProgression = CreateProgression(
                "utility",
                CreateState("u1"),
                CreateState("u2"),
                CreateState("u3"));
            PassiveDefinition lPassive = CreatePassive("utility-passive", PassiveTrigger.None, lProgression, true);
            SkillDefinition lSkill = CreateSkill("advance", 0, 0, 0, SkillPrimaryEffectType.None);
            SetField(lSkill, "_AdditionalEffectType", SkillAdditionalEffectType.AdvanceActivePassiveProgression);
            SetField(lSkill, "_PassiveProgressionSteps", 2);
            SetField(lSkill, "_TargetType", SkillTargetType.Self);
            SetField(lSkill, "_CanAffectCaster", true);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lSkill }, lPassive), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 4, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lActor.Position)).IsSuccess, Is.True);
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(1));
            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lActor.Position)).IsSuccess, Is.True);
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(2));
        }

        [Test]
        public void AllMatchingEnemiesExcludesNeutralUnitsAndCaster()
        {
            StateDefinition lDebuff = CreateState("enemy-debuff", pEnergy: -1);
            SkillDefinition lSkill = CreateSkill("all-enemies", 0, 0, 0, SkillPrimaryEffectType.None);
            SetField(lSkill, "_TargetType", SkillTargetType.Self);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.EnemiesOnly);
            SetField(lSkill, "_TargetScope", SkillTargetScope.AllMatchingUnits);
            SetField(lSkill, "_CanAffectCaster", false);
            SetField(lSkill, "_AppliedState", lDebuff);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lSkill }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 2, 0, 0),
                Spawn(CreateUnit(Team.Neutral, 100, 1), 3, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            lBattle.TryGetUnit(new UnitId(3), out UnitRuntime lNeutral);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lActor.Position)).IsSuccess, Is.True);
            Assert.That(lActor.HasState(lDebuff), Is.False);
            Assert.That(lEnemy.HasState(lDebuff), Is.True);
            Assert.That(lNeutral.HasState(lDebuff), Is.False);
        }

        [Test]
        public void AllMatchingAlliesAppliesStatesInFilteredScopeIncludingCaster()
        {
            StateDefinition lBuff = CreateState("ally-buff", pMobility: 1);
            SkillDefinition lSkill = CreateSkill("all-allies", 0, 0, 0, SkillPrimaryEffectType.None);
            SetField(lSkill, "_TargetType", SkillTargetType.Self);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.AlliesOnly);
            SetField(lSkill, "_TargetScope", SkillTargetScope.AllMatchingUnits);
            SetField(lSkill, "_CanAffectCaster", true);
            SetField(lSkill, "_AppliedState", lBuff);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lSkill }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamA, 100, 1), 3, 0, 1),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 4, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lAlly);
            lBattle.TryGetUnit(new UnitId(3), out UnitRuntime lEnemy);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lActor.Position));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            Assert.That(lActor.HasState(lBuff), Is.True);
            Assert.That(lAlly.HasState(lBuff), Is.True);
            Assert.That(lEnemy.HasState(lBuff), Is.False);
            Assert.That(lResult.AffectedUnitIds, Is.Ordered.By("Value"));
        }

        [Test]
        public void MobilityAndEnergyStateDeltasApplyImmediatelyOnlyDuringActivation()
        {
            UnitRuntime lUnit = new UnitRuntime(
                new UnitId(1),
                CreateUnit(Team.TeamA, 100, 10),
                new GridCoord(0, 0));
            StateDefinition lBoost = CreateState("boost", pMobility: 1, pEnergy: 2);
            StateDefinition lDebuff = CreateState("debuff", pMobility: -2, pEnergy: -3);

            Assert.That(lUnit.TryApplyState(lBoost), Is.True);
            Assert.That(lUnit.RemainingMobility, Is.Zero);
            Assert.That(lUnit.RemainingEnergy, Is.Zero);
            lUnit.BeginTurn();
            Assert.That(lUnit.RemainingMobility, Is.EqualTo(4));
            Assert.That(lUnit.RemainingEnergy, Is.EqualTo(12));

            Assert.That(lUnit.TryApplyState(lDebuff), Is.True);
            Assert.That(lUnit.RemainingMobility, Is.EqualTo(2));
            Assert.That(lUnit.RemainingEnergy, Is.EqualTo(9));
            Assert.That(lUnit.RemoveTemporaryState(lDebuff), Is.True);
            Assert.That(lUnit.RemainingMobility, Is.EqualTo(4));
            Assert.That(lUnit.RemainingEnergy, Is.EqualTo(12));
        }

        [Test]
        public void LinkedTargetAndCasterStatesModelImmediateStatSteal()
        {
            StateDefinition lTargetDebuff = CreateState("target-debuff", pMobility: -1);
            StateDefinition lCasterBuff = CreateState("caster-buff", pMobility: 1);
            SkillDefinition lSkill = CreateSkill("steal", 0, 2, 0, SkillPrimaryEffectType.None);
            SetField(lSkill, "_TargetType", SkillTargetType.Unit);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.EnemiesOnly);
            SetField(lSkill, "_AppliedState", lTargetDebuff);
            SetField(lSkill, "_CasterAppliedState", lCasterBuff);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lSkill }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 1, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lEnemy.Position)).IsSuccess, Is.True);
            Assert.That(lActor.GetMobilityModifier(), Is.EqualTo(1));
            Assert.That(lActor.RemainingMobility, Is.EqualTo(4));
            Assert.That(lEnemy.GetMobilityModifier(), Is.EqualTo(-1));
            Assert.That(lEnemy.RemainingMobility, Is.Zero);
        }

        [Test]
        public void PullMovesTowardCasterAndStopsBeforeOccupiedCasterCellWithoutDamage()
        {
            SkillDefinition lPull = CreateSkill("pull", 0, 4, 0, SkillPrimaryEffectType.None);
            SetField(lPull, "_AdditionalEffectType", SkillAdditionalEffectType.Pull);
            SetField(lPull, "_PushDistance", 3);
            SetField(lPull, "_TargetType", SkillTargetType.Unit);
            SetField(lPull, "_TargetRelation", SkillTargetRelation.EnemiesOnly);

            IBattleService lBattle = CreateBattle(
                Spawn(CreateUnit(Team.TeamA, 100, 100, new[] { lPull }), 0, 0, 0),
                Spawn(CreateUnit(Team.TeamB, 100, 1), 3, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lPull.Id), SkillTarget.ForCell(lEnemy.Position)).IsSuccess, Is.True);
            Assert.That(lEnemy.Position, Is.EqualTo(new GridCoord(1, 0)));
            Assert.That(lEnemy.CurrentHealth, Is.EqualTo(100));
        }

        private UnitDefinition CreateUnit(
            Team pTeam,
            int pHealth,
            int pVelocity,
            IReadOnlyList<SkillDefinition> pSkills = null,
            PassiveDefinition pPassive = null)
        {
            UnitDefinition lUnit = Track(ScriptableObject.CreateInstance<UnitDefinition>());
            SetField(lUnit, "_Id", $"unit-{_ObjectsToDestroy.Count}");
            SetField(lUnit, "_Team", pTeam);
            SetField(lUnit, "_MaxHealth", pHealth);
            SetField(lUnit, "_MobilityPerTurn", 3);
            SetField(lUnit, "_EnergyPerTurn", 10);
            SetField(lUnit, "_Velocity", pVelocity);
            SetField(lUnit, "_Skills", pSkills != null ? new List<SkillDefinition>(pSkills) : new List<SkillDefinition>());
            SetField(lUnit, "_Passives", pPassive != null ? new List<PassiveDefinition> { pPassive } : new List<PassiveDefinition>());
            SetField(lUnit, "_DefaultPassive", pPassive);
            return lUnit;
        }

        private SkillDefinition CreateSkill(
            string pId,
            int pPower,
            int pRange,
            int pEnergy,
            SkillPrimaryEffectType pPrimaryEffect = SkillPrimaryEffectType.Damage)
        {
            SkillDefinition lSkill = Track(ScriptableObject.CreateInstance<SkillDefinition>());
            SetField(lSkill, "_Id", pId);
            SetField(lSkill, "_PrimaryEffectType", pPrimaryEffect);
            SetField(lSkill, "_Category", SkillCategory.RangedAtk);
            SetField(lSkill, "_Power", pPower);
            SetField(lSkill, "_RangeMax", pRange);
            SetField(lSkill, "_EnergyCost", pEnergy);
            SetField(lSkill, "_CanAffectCaster", false);
            return lSkill;
        }

        private StateDefinition CreateState(string pId, int pMobility = 0, int pEnergy = 0)
        {
            StateDefinition lState = Track(ScriptableObject.CreateInstance<StateDefinition>());
            SetField(lState, "_Id", pId);
            SetField(lState, "_MobilityModifierPerStack", pMobility);
            SetField(lState, "_EnergyModifierPerStack", pEnergy);
            return lState;
        }

        private StateProgressionDefinition CreateProgression(string pId, params StateDefinition[] pStates)
        {
            StateProgressionDefinition lProgression = Track(ScriptableObject.CreateInstance<StateProgressionDefinition>());
            SetField(lProgression, "_Id", pId);
            SetField(lProgression, "_States", new List<StateDefinition>(pStates));
            return lProgression;
        }

        private PassiveDefinition CreatePassive(
            string pId,
            PassiveTrigger pTrigger,
            StateProgressionDefinition pProgression,
            bool pTriggerOnVariant)
        {
            PassiveDefinition lPassive = Track(ScriptableObject.CreateInstance<PassiveDefinition>());
            SetField(lPassive, "_Id", pId);
            SetField(lPassive, "_Trigger", pTrigger);
            SetField(lPassive, "_StateProgression", pProgression);
            SetField(lPassive, "_ProgressionStepsPerTrigger", 1);
            SetField(lPassive, "_TriggerOnOwnerVariantExecutions", pTriggerOnVariant);
            return lPassive;
        }

        private IBattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime(
                "generic-character-systems",
                "Generic Character Systems",
                8,
                8,
                1,
                null,
                pSpawns));
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

        private T Track<T>(T pObject) where T : Object
        {
            _ObjectsToDestroy.Add(pObject);
            return pObject;
        }

        private static void SetField<TTarget, TValue>(TTarget pTarget, string pFieldName, TValue pValue)
        {
            FieldInfo lField = typeof(TTarget).GetField(pFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lField, Is.Not.Null, $"Missing field {typeof(TTarget).Name}.{pFieldName}");
            lField.SetValue(pTarget, pValue);
        }
    }
}
