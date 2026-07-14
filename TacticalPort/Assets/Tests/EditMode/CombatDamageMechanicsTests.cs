using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class CombatDamageMechanicsTests
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
        public void GeneralStatsCombineWithTypedStatsAndAllowVulnerability()
        {
            UnitDefinition lAttackerDefinition = CreateUnitDefinition(Team.TeamA, 100, 120, 0, 10, 0);
            UnitDefinition lDefenderDefinition = CreateUnitDefinition(Team.TeamB, 100, 100, 20, 0, 10);
            UnitRuntime lAttacker = new UnitRuntime(new UnitId(1), lAttackerDefinition, new GridCoord(0, 0));
            UnitRuntime lDefender = new UnitRuntime(new UnitId(2), lDefenderDefinition, new GridCoord(1, 0));
            StateDefinition lDamageState = CreateState(pGeneralDamage: 5);
            StateDefinition lVulnerabilityState = CreateState(pGeneralResistance: -40);

            lAttacker.TryApplyState(lDamageState);
            lDefender.TryApplyState(lVulnerabilityState);

            Assert.That(lAttacker.ResolveOutgoingDamage(100, DamageRangeType.Melee), Is.EqualTo(135));
            Assert.That(lDefender.ResolveIncomingDamage(100, DamageRangeType.Melee), Is.EqualTo(110));

            UnitDefinition lCappedDefinition = CreateUnitDefinition(Team.TeamB, 100, 100, 60, 0, 50);
            UnitRuntime lCappedDefender = new UnitRuntime(new UnitId(3), lCappedDefinition, new GridCoord(2, 0));
            Assert.That(lCappedDefender.ResolveIncomingDamage(100, DamageRangeType.Melee), Is.Zero);
        }

        [Test]
        public void SkillWearAccumulatesFractionsAndNonSkillDamageDoesNotErode()
        {
            UnitRuntime lUnit = new UnitRuntime(
                new UnitId(1),
                CreateUnitDefinition(Team.TeamB, 100),
                new GridCoord(0, 0));

            Assert.That(lUnit.ApplySkillDamage(10), Is.EqualTo(10));
            Assert.That(lUnit.CurrentMaxHealth, Is.EqualTo(100));

            Assert.That(lUnit.ApplySkillDamage(10), Is.EqualTo(10));
            Assert.That(lUnit.CurrentMaxHealth, Is.EqualTo(99));

            lUnit.ApplyDamage(10);
            Assert.That(lUnit.CurrentMaxHealth, Is.EqualTo(99));
        }

        [Test]
        public void WearStateIsAdditiveCappedAndUsesActualHealthRemoved()
        {
            UnitRuntime lUnit = new UnitRuntime(
                new UnitId(1),
                CreateUnitDefinition(Team.TeamB, 100),
                new GridCoord(0, 0));
            lUnit.TryApplyState(CreateState(pWear: 100));

            Assert.That(lUnit.EffectiveWearPercent, Is.EqualTo(50));
            lUnit.ApplySkillDamage(10);
            Assert.That(lUnit.CurrentMaxHealth, Is.EqualTo(95));

            lUnit.ApplyDamage(84);
            Assert.That(lUnit.CurrentHealth, Is.EqualTo(6));
            lUnit.ApplySkillDamage(100);
            Assert.That(lUnit.CurrentMaxHealth, Is.EqualTo(92));
        }

        [Test]
        public void WearCannotReduceMaximumHealthBelowOne()
        {
            UnitRuntime lUnit = new UnitRuntime(
                new UnitId(1),
                CreateUnitDefinition(Team.TeamB, 10),
                new GridCoord(0, 0));
            lUnit.TryApplyState(CreateState(pWear: 100));

            while (lUnit.CurrentMaxHealth > 1 && lUnit.IsAlive)
            {
                lUnit.ApplySkillDamage(2);
                lUnit.RestoreHealth(100);
            }

            Assert.That(lUnit.CurrentMaxHealth, Is.EqualTo(1));
        }

        [Test]
        public void NewSerializedFieldsRoundTripWithNeutralDefaults()
        {
            UnitDefinition lSourceUnit = CreateUnitDefinition(Team.TeamA, 100, pGeneralDamage: 12, pGeneralResistance: 7);
            UnitDefinition lRoundTripUnit = Track(ScriptableObject.CreateInstance<UnitDefinition>());
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(lSourceUnit), lRoundTripUnit);
            Assert.That(lRoundTripUnit.GeneralDamagePercent, Is.EqualTo(12));
            Assert.That(lRoundTripUnit.GeneralResistancePercent, Is.EqualTo(7));

            StateDefinition lSourceState = CreateState(pGeneralResistance: 9, pWear: 10);
            StateDefinition lRoundTripState = Track(ScriptableObject.CreateInstance<StateDefinition>());
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(lSourceState), lRoundTripState);
            Assert.That(lRoundTripState.GeneralResistancePercentPerStack, Is.EqualTo(9));
            Assert.That(lRoundTripState.WearPercentPerStack, Is.EqualTo(10));

            SkillDefinition lSourceSkill = CreateDamageSkill("serialized-life-steal", 10, true, null);
            SkillDefinition lRoundTripSkill = Track(ScriptableObject.CreateInstance<SkillDefinition>());
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(lSourceSkill), lRoundTripSkill);
            Assert.That(lRoundTripSkill.HasLifeSteal, Is.True);
        }

        [Test]
        public void LifeStealAggregatesEnemyAoeDamageAndExcludesAlliesAndOverkill()
        {
            SkillDefinition lSkill = CreateDamageSkill("life-steal", 10, true, null, SkillAoeShape.Circle, 1);
            UnitDefinition lActorDefinition = CreateUnitDefinition(Team.TeamA, 100, pVelocity: 100, pSkills: new[] { lSkill });
            UnitDefinition lAllyDefinition = CreateUnitDefinition(Team.TeamA, 100, pVelocity: 1);
            UnitDefinition lEnemyOneDefinition = CreateUnitDefinition(Team.TeamB, 100, pVelocity: 1);
            UnitDefinition lEnemyTwoDefinition = CreateUnitDefinition(Team.TeamB, 100, pVelocity: 1);
            IBattleService lBattle = CreateBattle(
                Spawn(lActorDefinition, 0, 0, 0),
                Spawn(lAllyDefinition, 1, 0, 1),
                Spawn(lEnemyOneDefinition, 2, 0, 0),
                Spawn(lEnemyTwoDefinition, 2, 1, 1));

            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lAlly);
            lBattle.TryGetUnit(new UnitId(3), out UnitRuntime lEnemyOne);
            lBattle.TryGetUnit(new UnitId(4), out UnitRuntime lEnemyTwo);
            lActor.ApplyDamage(50);
            lEnemyOne.ApplyDamage(95);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lEnemyOne.Position));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            Assert.That(lActor.CurrentHealth, Is.EqualTo(57));
            Assert.That(lAlly.CurrentHealth, Is.EqualTo(90));
            Assert.That(lEnemyOne.CurrentHealth, Is.Zero);
            Assert.That(lEnemyTwo.CurrentHealth, Is.EqualTo(90));
            Assert.That((lResult.OutcomeFlags & BattleActionOutcomeFlags.Heal) != 0, Is.True);
        }

        [Test]
        public void WearStateAppliedByDamageSkillAffectsOnlyFollowingImpact()
        {
            StateDefinition lWearState = CreateState(pWear: 10);
            SkillDefinition lSkill = CreateDamageSkill("wear-hit", 20, false, lWearState);
            UnitDefinition lActorDefinition = CreateUnitDefinition(Team.TeamA, 100, pVelocity: 100, pSkills: new[] { lSkill });
            UnitDefinition lEnemyDefinition = CreateUnitDefinition(Team.TeamB, 100, pVelocity: 1);
            IBattleService lBattle = CreateBattle(
                Spawn(lActorDefinition, 0, 0, 0),
                Spawn(lEnemyDefinition, 1, 0, 0));

            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lEnemy.Position)).IsSuccess, Is.True);
            Assert.That(lEnemy.CurrentMaxHealth, Is.EqualTo(99));
            Assert.That(lEnemy.EffectiveWearPercent, Is.EqualTo(15));

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lEnemy.Position)).IsSuccess, Is.True);
            Assert.That(lEnemy.CurrentMaxHealth, Is.EqualTo(96));
        }

        [Test]
        public void WearStateProducesDeterministicChecksum()
        {
            UnitDefinition lActorDefinition = CreateUnitDefinition(Team.TeamA, 100, pVelocity: 100);
            UnitDefinition lEnemyDefinition = CreateUnitDefinition(Team.TeamB, 100, pVelocity: 1);
            IBattleService lFirst = CreateBattle(Spawn(lActorDefinition, 0, 0, 0), Spawn(lEnemyDefinition, 1, 0, 0));
            IBattleService lSecond = CreateBattle(Spawn(lActorDefinition, 0, 0, 0), Spawn(lEnemyDefinition, 1, 0, 0));
            lFirst.TryGetUnit(new UnitId(2), out UnitRuntime lFirstEnemy);
            lSecond.TryGetUnit(new UnitId(2), out UnitRuntime lSecondEnemy);

            int lInitialChecksum = BattleStateChecksum.Compute(lFirst);
            lFirstEnemy.ApplySkillDamage(10);
            Assert.That(BattleStateChecksum.Compute(lFirst), Is.Not.EqualTo(lInitialChecksum));

            lSecondEnemy.ApplySkillDamage(10);

            Assert.That(BattleStateChecksum.Compute(lFirst), Is.EqualTo(BattleStateChecksum.Compute(lSecond)));
        }

        private UnitDefinition CreateUnitDefinition(
            Team pTeam,
            int pMaxHealth,
            int pMeleeDamage = 100,
            int pMeleeResistance = 0,
            int pGeneralDamage = 0,
            int pGeneralResistance = 0,
            int pVelocity = 10,
            IReadOnlyList<SkillDefinition> pSkills = null)
        {
            UnitDefinition lDefinition = Track(ScriptableObject.CreateInstance<UnitDefinition>());
            SetField(lDefinition, "_Team", pTeam);
            SetField(lDefinition, "_MaxHealth", pMaxHealth);
            SetField(lDefinition, "_MeleeDamagePercent", pMeleeDamage);
            SetField(lDefinition, "_MeleeResistancePercent", pMeleeResistance);
            SetField(lDefinition, "_GeneralDamagePercent", pGeneralDamage);
            SetField(lDefinition, "_GeneralResistancePercent", pGeneralResistance);
            SetField(lDefinition, "_Velocity", pVelocity);
            SetField(lDefinition, "_EnergyPerTurn", 10);
            SetField(lDefinition, "_Skills", pSkills != null ? new List<SkillDefinition>(pSkills) : new List<SkillDefinition>());
            return lDefinition;
        }

        private StateDefinition CreateState(int pGeneralDamage = 0, int pGeneralResistance = 0, int pWear = 0)
        {
            StateDefinition lState = Track(ScriptableObject.CreateInstance<StateDefinition>());
            SetField(lState, "_Id", $"state-{_ObjectsToDestroy.Count}");
            SetField(lState, "_DamageModifierPerStack", pGeneralDamage);
            SetField(lState, "_GeneralResistancePercentPerStack", pGeneralResistance);
            SetField(lState, "_WearPercentPerStack", pWear);
            return lState;
        }

        private SkillDefinition CreateDamageSkill(
            string pId,
            int pPower,
            bool pHasLifeSteal,
            StateDefinition pAppliedState,
            SkillAoeShape pAoeShape = SkillAoeShape.Single,
            int pAoeSize = 0)
        {
            SkillDefinition lSkill = Track(ScriptableObject.CreateInstance<SkillDefinition>());
            SetField(lSkill, "_Id", pId);
            SetField(lSkill, "_PrimaryEffectType", SkillPrimaryEffectType.Damage);
            SetField(lSkill, "_Category", SkillCategory.RangedAtk);
            SetField(lSkill, "_Power", pPower);
            SetField(lSkill, "_HasLifeSteal", pHasLifeSteal);
            SetField(lSkill, "_RangeMax", 4);
            SetField(lSkill, "_CanAffectCaster", false);
            SetField(lSkill, "_AoeShape", pAoeShape);
            SetField(lSkill, "_AoeSize", pAoeSize);
            SetField(lSkill, "_EnergyCost", 0);
            SetField(lSkill, "_AppliedState", pAppliedState);
            return lSkill;
        }

        private IBattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime(
                "damage-tests",
                "Damage Tests",
                6,
                6,
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

        private static void SetField<TTarget, TValue>(TTarget pTarget, string pFieldName, TValue pValue) =>
            typeof(TTarget).GetField(pFieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pTarget, pValue);
    }
}
