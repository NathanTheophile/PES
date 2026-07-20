using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.State.Tests.Characters.Freebooter
{
    public sealed class FreebooterContentTests
    {
        private const string Root = "Assets/AITests/Freebooter/Data";
        private readonly List<Object> _ObjectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int lIndex = _ObjectsToDestroy.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_ObjectsToDestroy[lIndex] != null && !AssetDatabase.Contains(_ObjectsToDestroy[lIndex]))
                    Object.DestroyImmediate(_ObjectsToDestroy[lIndex]);
            }

            _ObjectsToDestroy.Clear();
        }

        [Test]
        public void UnitContainsExactlyTheFreebooterPoolAndSharedPresentation()
        {
            UnitDefinition lFreebooter = Load<UnitDefinition>("Units/Freebooter.asset");

                Assert.That(lFreebooter.Id, Is.EqualTo("freebooter"));
                Assert.That(lFreebooter.EnglishDisplayName, Is.EqualTo("Freebooter"));
                Assert.That(lFreebooter.Team, Is.EqualTo(Team.TeamA));
                Assert.That(lFreebooter.MaxHealth, Is.EqualTo(420));
                Assert.That(lFreebooter.MobilityPerTurn, Is.EqualTo(3));
                Assert.That(lFreebooter.EnergyPerTurn, Is.EqualTo(6));
                Assert.That(lFreebooter.Velocity, Is.EqualTo(55));
                Assert.That(lFreebooter.MeleeDamagePercent, Is.EqualTo(100));
                Assert.That(lFreebooter.RangedDamagePercent, Is.EqualTo(105));
                Assert.That(lFreebooter.StatPointBudget, Is.EqualTo(120));
                Assert.That(lFreebooter.PushDamageBonus, Is.EqualTo(15));
                Assert.That(lFreebooter.FootprintWidth, Is.EqualTo(1));
                Assert.That(lFreebooter.FootprintHeight, Is.EqualTo(1));
                Assert.That(lFreebooter.ParticipatesInTurnOrder, Is.True);
                Assert.That(lFreebooter.CountsForVictory, Is.True);
                Assert.That(lFreebooter.CombatViewPrefabComponent, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(lFreebooter.CombatViewPrefabComponent), Is.EqualTo("Assets/Prefabs/Actors/P_ActorBase.prefab"));
                Assert.That(lFreebooter.ModelPrefab, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(lFreebooter.ModelPrefab), Is.EqualTo("Assets/AITests/Freebooter/Prefabs/Freebooter_Blockout.prefab"));
                Assert.That(lFreebooter.ModelPrefab.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
                Assert.That(lFreebooter.ModelPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(pRenderer => pRenderer.sharedMaterials), Has.All.Not.Null);
                Assert.That(lFreebooter.DisplaySprite, Is.Not.Null);

            string[] lExpectedSkills =
            {
                "boarding_harpoon", "hooked_sabre", "pistol_shot", "doubloon_rain", "hull_breaker",
                "net_cast", "clever_grapple", "share_the_treasure", "sapping_shot", "plunder_shot"
            };
            Assert.That(lFreebooter.Skills.Select(pSkill => pSkill.Id), Is.EqualTo(lExpectedSkills));
            Assert.That(lFreebooter.Skills, Has.Count.EqualTo(10));
            Assert.That(lFreebooter.Passives.Select(pPassive => pPassive.Id), Is.EqualTo(new[] { "thirst_for_wealth", "buccaneers_hide" }));
            Assert.That(lFreebooter.DefaultPassive.Id, Is.EqualTo("thirst_for_wealth"));

            SkillDefinition[] lVariants = lFreebooter.Skills.Where(pSkill => pSkill.Variant != null).Select(pSkill => pSkill.Variant).ToArray();
            Assert.That(lVariants, Has.Length.EqualTo(5));
            Assert.That(lVariants.Intersect(lFreebooter.Skills), Is.Empty);
            Assert.That(lVariants.Select(pSkill => pSkill.Id), Is.EquivalentTo(new[]
            {
                "boarding_harpoon_variant", "hooked_sabre_variant", "pistol_shot_variant", "hull_breaker_variant", "plunder_shot_variant"
            }));
        }

        [Test]
        public void SkillsAndVariantsExposeTheirEffectiveRuntimeRules()
        {
            UnitDefinition lFreebooter = Load<UnitDefinition>("Units/Freebooter.asset");
            Dictionary<string, SkillDefinition> lSkills = lFreebooter.Skills.ToDictionary(pSkill => pSkill.Id);

            AssertSkill(lSkills["boarding_harpoon"], SkillCategory.RangedAtk, 2, 1, 3, 24, SkillAdditionalEffectType.Pull, 1, 2, 1, 0);
            AssertSkill(lSkills["boarding_harpoon"].Variant, SkillCategory.RangedAtk, 2, 1, 5, 34, SkillAdditionalEffectType.Pull, 2, 2, 1, 0);
            AssertSkill(lSkills["hooked_sabre"], SkillCategory.MeleeAtk, 3, 1, 1, 32, SkillAdditionalEffectType.None, 0, 1, 1, 0);
            Assert.That(lSkills["hooked_sabre"].AppliedStateStacks, Is.EqualTo(1));
            Assert.That(lSkills["hooked_sabre"].Variant.AppliedStateStacks, Is.EqualTo(2));
            AssertSkill(lSkills["pistol_shot"], SkillCategory.RangedAtk, 3, 2, 5, 30, SkillAdditionalEffectType.None, 0, 2, 1, 0);
            AssertSkill(lSkills["pistol_shot"].Variant, SkillCategory.RangedAtk, 3, 2, 6, 42, SkillAdditionalEffectType.None, 0, 2, 1, 0);
            Assert.That(lSkills["pistol_shot"].Variant.RequiresVisibility, Is.False);
            Assert.That(lSkills["pistol_shot"].Variant.CasterAppliedState, Is.Not.Null);
            AssertSkill(lSkills["doubloon_rain"], SkillCategory.Utility, 3, 0, 0, 0, SkillAdditionalEffectType.AdvanceActivePassiveProgression, 0, 0, 0, 2);
            AssertSkill(lSkills["hull_breaker"], SkillCategory.MeleeAtk, 4, 1, 1, 48, SkillAdditionalEffectType.None, 0, 1, 1, 2);
            Assert.That(lSkills["hull_breaker"].Variant.HasLifeSteal, Is.True);
            AssertSkill(lSkills["net_cast"], SkillCategory.Utility, 3, 0, 0, 0, SkillAdditionalEffectType.None, 0, 0, 0, 2);
            Assert.That(lSkills["net_cast"].AoeShape, Is.EqualTo(SkillAoeShape.Circle));
            Assert.That(lSkills["net_cast"].AoeSize, Is.EqualTo(2));
            Assert.That(lSkills["net_cast"].AppliedStateStacks, Is.EqualTo(2));
            AssertSkill(lSkills["clever_grapple"], SkillCategory.Utility, 3, 1, 4, 0, SkillAdditionalEffectType.SwitchPositions, 0, 0, 0, 3);
            AssertSkill(lSkills["share_the_treasure"], SkillCategory.Utility, 2, 0, 0, 0, SkillAdditionalEffectType.None, 0, 0, 0, 3);
            Assert.That(lSkills["share_the_treasure"].TargetScope, Is.EqualTo(SkillTargetScope.AllMatchingUnits));
            AssertSkill(lSkills["sapping_shot"], SkillCategory.RangedAtk, 3, 1, 4, 20, SkillAdditionalEffectType.None, 0, 0, 0, 0);
            Assert.That(lSkills["sapping_shot"].AppliedStateDurationTurns, Is.EqualTo(2));
            AssertSkill(lSkills["plunder_shot"], SkillCategory.RangedAtk, 3, 1, 6, 26, SkillAdditionalEffectType.None, 0, 0, 0, 0);
            Assert.That(lSkills["plunder_shot"].HasLifeSteal, Is.True);
            Assert.That(lSkills["plunder_shot"].Variant.RangeMax, Is.EqualTo(8));
            Assert.That(lSkills["plunder_shot"].Variant.Power, Is.EqualTo(32));
        }

        [Test]
        public void StatesAndPassivesMatchTheTwoSelectableProgressions()
        {
            StateDefinition lTreasureDamage = Load<StateDefinition>("States/TreasureDamage.asset");
            StateDefinition lPactoleDamage = Load<StateDefinition>("States/JackpotDamage.asset");
            StateDefinition lTreasureResistance = Load<StateDefinition>("States/TreasureResistance.asset");
            StateDefinition lPactoleResistance = Load<StateDefinition>("States/JackpotResistance.asset");
            StateProgressionDefinition lDamage = Load<StateProgressionDefinition>("Progressions/FreebooterDamageProgression.asset");
            StateProgressionDefinition lResistance = Load<StateProgressionDefinition>("Progressions/FreebooterResistanceProgression.asset");
            PassiveDefinition lThirst = Load<PassiveDefinition>("Passives/ThirstForWealth.asset");
            PassiveDefinition lHide = Load<PassiveDefinition>("Passives/BuccaneersHide.asset");

                Assert.That(lTreasureDamage.MaxStacks, Is.EqualTo(4));
                Assert.That(lTreasureDamage.DamageModifierPerStack, Is.EqualTo(5));
                Assert.That(lPactoleDamage.DamageModifierPerStack, Is.EqualTo(25));
                Assert.That(lPactoleDamage.EnablesSkillVariant, Is.True);
                Assert.That(lPactoleDamage.VariantConsumedReplacementState, Is.SameAs(lTreasureDamage));
                Assert.That(lTreasureResistance.MeleeResistancePercentPerStack, Is.EqualTo(5));
                Assert.That(lTreasureResistance.RangedResistancePercentPerStack, Is.EqualTo(5));
                Assert.That(lPactoleResistance.MeleeResistancePercentPerStack, Is.EqualTo(25));
                Assert.That(lPactoleResistance.RangedResistancePercentPerStack, Is.EqualTo(25));
                Assert.That(lPactoleResistance.EnablesSkillVariant, Is.True);
                Assert.That(lPactoleResistance.VariantConsumedReplacementState, Is.SameAs(lTreasureResistance));
                Assert.That(lThirst.Trigger, Is.EqualTo(PassiveTrigger.OwnerDealtSkillDamageToEnemy));
                Assert.That(lThirst.StateProgression, Is.SameAs(lDamage));
                Assert.That(lThirst.TriggerOnOwnerVariantExecutions, Is.False);
                Assert.That(lHide.Trigger, Is.EqualTo(PassiveTrigger.OwnerTookSkillDamageFromEnemy));
                Assert.That(lHide.StateProgression, Is.SameAs(lResistance));
                Assert.That(lHide.TriggerOnOwnerVariantExecutions, Is.False);

            AssertProgression(lDamage, lTreasureDamage, lPactoleDamage);
            AssertProgression(lResistance, lTreasureResistance, lPactoleResistance);
        }

        [Test]
        public void StackProgressionSupportsRepeatedStateAndStopsAtPactole()
        {
            UnitRuntime lActor = CreateFreebooterRuntime();
            StateProgressionDefinition lProgression = Load<StateProgressionDefinition>("Progressions/FreebooterDamageProgression.asset");
            StateDefinition lTreasure = Load<StateDefinition>("States/TreasureDamage.asset");
            StateDefinition lPactole = Load<StateDefinition>("States/JackpotDamage.asset");

            for (int lExpectedStacks = 1; lExpectedStacks <= 4; lExpectedStacks++)
            {
                Assert.That(lActor.AdvanceStateProgression(lProgression), Is.True);
                Assert.That(lActor.GetStateStacks(lTreasure), Is.EqualTo(lExpectedStacks));
                Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(lExpectedStacks - 1));
            }

            Assert.That(lActor.AdvanceStateProgression(lProgression), Is.True);
            Assert.That(lActor.GetStateStacks(lTreasure), Is.Zero);
            Assert.That(lActor.GetStateStacks(lPactole), Is.EqualTo(1));
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(4));
            Assert.That(lActor.AdvanceStateProgression(lProgression), Is.False);
        }

        [Test]
        public void LegacyStateListStillActsAsOneStackPerLevel()
        {
            StateDefinition lFirst = Track(ScriptableObject.CreateInstance<StateDefinition>());
            StateDefinition lSecond = Track(ScriptableObject.CreateInstance<StateDefinition>());
            StateProgressionDefinition lLegacy = Track(ScriptableObject.CreateInstance<StateProgressionDefinition>());
            SetField(lLegacy, "_Id", "legacy-progression");
            SetField(lLegacy, "_States", new List<StateDefinition> { lFirst, lSecond });
            UnitRuntime lActor = CreateFreebooterRuntime();

            Assert.That(lLegacy.LevelCount, Is.EqualTo(2));
            Assert.That(lLegacy.GetLevelStacks(0), Is.EqualTo(1));
            Assert.That(lActor.AdvanceStateProgression(lLegacy), Is.True);
            Assert.That(lActor.GetStateStacks(lFirst), Is.EqualTo(1));
            Assert.That(lActor.AdvanceStateProgression(lLegacy), Is.True);
            Assert.That(lActor.GetStateStacks(lSecond), Is.EqualTo(1));
        }

        [TestCase("Progressions/FreebooterDamageProgression.asset", "States/JackpotDamage.asset", "States/TreasureDamage.asset")]
        [TestCase("Progressions/FreebooterResistanceProgression.asset", "States/JackpotResistance.asset", "States/TreasureResistance.asset")]
        public void JackpotConsumptionKeepsProgressionKeyAndReturnsToTreasureOne(string pProgressionPath, string pJackpotPath, string pTreasurePath)
        {
            UnitRuntime lActor = CreateFreebooterRuntime();
            StateProgressionDefinition lProgression = Load<StateProgressionDefinition>(pProgressionPath);
            StateDefinition lJackpot = Load<StateDefinition>(pJackpotPath);
            StateDefinition lTreasure = Load<StateDefinition>(pTreasurePath);
            Assert.That(lActor.AdvanceStateProgression(lProgression, 5), Is.True);
            string lKey = lActor.ActiveStates.Single(pState => pState.Definition == lJackpot).Key;

            Assert.That(lActor.ConsumeSkillVariantEnablers(), Is.EqualTo(1));

            BattleStateRuntime lReplacement = lActor.ActiveStates.Single(pState => pState.Definition == lTreasure);
            Assert.That(lReplacement.Key, Is.EqualTo(lKey));
            Assert.That(lReplacement.Stacks, Is.EqualTo(1));
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(0));
        }

        [Test]
        public void OffensivePassiveAdvancesOnceOnlyAfterRealEnemySkillDamage()
        {
            UnitDefinition lFreebooter = Load<UnitDefinition>("Units/Freebooter.asset");
            SkillDefinition lHarpon = lFreebooter.Skills.Single(pSkill => pSkill.Id == "boarding_harpoon");
            UnitDefinition lEnemy = CreateUnit(Team.TeamB, 100, 1, 0);
            IBattleService lBattle = CreateBattle(Spawn(lFreebooter, 0, 0, 0), Spawn(lEnemy, 2, 0, 0));
            Assert.That(lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor), Is.True);
            Assert.That(lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lTarget), Is.True);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lHarpon.Id), SkillTarget.ForCell(lTarget.Position)).IsSuccess, Is.True);
            Assert.That(lActor.GetStateProgressionIndex(lActor.ActivePassive.StateProgression), Is.EqualTo(0));

            UnitDefinition lImmuneEnemy = CreateUnit(Team.TeamB, 100, 1, 100);
            IBattleService lImmuneBattle = CreateBattle(Spawn(lFreebooter, 0, 0, 0), Spawn(lImmuneEnemy, 2, 0, 0));
            lImmuneBattle.TryGetUnit(new UnitId(1), out UnitRuntime lImmuneActor);
            lImmuneBattle.TryGetUnit(new UnitId(2), out UnitRuntime lImmuneTarget);
            Assert.That(lImmuneBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lImmuneBattle.UseSkill(lImmuneActor.Id, new SkillId(lHarpon.Id), SkillTarget.ForCell(lImmuneTarget.Position)).IsSuccess, Is.True);
            Assert.That(lImmuneActor.GetStateProgressionIndex(lImmuneActor.ActivePassive.StateProgression), Is.EqualTo(-1));
        }

        [Test]
        public void VariantExecutionConsumesPactoleWithoutAdditionalPassiveProgress()
        {
            UnitDefinition lFreebooter = Load<UnitDefinition>("Units/Freebooter.asset");
            UnitDefinition lEnemy = CreateUnit(Team.TeamB, 200, 1, 0);
            IBattleService lBattle = CreateBattle(Spawn(lFreebooter, 0, 0, 0), Spawn(lEnemy, 2, 0, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            lBattle.TryGetUnit(new UnitId(2), out UnitRuntime lTarget);
            StateProgressionDefinition lProgression = lActor.ActivePassive.StateProgression;
            StateDefinition lTreasure = Load<StateDefinition>("States/TreasureDamage.asset");
            Assert.That(lActor.AdvanceStateProgression(lProgression, 5), Is.True);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(lActor.Id, new SkillId("boarding_harpoon"), SkillTarget.ForCell(lTarget.Position));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            Assert.That(lActor.GetStateStacks(lTreasure), Is.EqualTo(1));
            Assert.That(lActor.GetStateProgressionIndex(lProgression), Is.EqualTo(0));
        }

        [Test]
        public void ChecksumIncludesProgressionStacksAndActivePassive()
        {
            UnitDefinition lFreebooter = Load<UnitDefinition>("Units/Freebooter.asset");
            UnitDefinition lEnemy = CreateUnit(Team.TeamB, 100, 1, 0);
            IBattleService lFirst = CreateBattle(Spawn(lFreebooter, 0, 0, 0), Spawn(lEnemy, 5, 5, 0));
            IBattleService lSecond = CreateBattle(Spawn(lFreebooter, 0, 0, 0), Spawn(lEnemy, 5, 5, 0));
            lFirst.TryGetUnit(new UnitId(1), out UnitRuntime lFirstActor);
            lSecond.TryGetUnit(new UnitId(1), out UnitRuntime lSecondActor);

            Assert.That(lFirstActor.AdvanceStateProgression(lFirstActor.ActivePassive.StateProgression, 1), Is.True);
            Assert.That(lSecondActor.AdvanceStateProgression(lSecondActor.ActivePassive.StateProgression, 2), Is.True);
            Assert.That(BattleStateChecksum.Compute(lFirst), Is.Not.EqualTo(BattleStateChecksum.Compute(lSecond)));

            Assert.That(lSecondActor.TrySetActivePassive(lFreebooter.Passives[1]), Is.True);
            Assert.That(BattleStateChecksum.Compute(lFirst), Is.Not.EqualTo(BattleStateChecksum.Compute(lSecond)));
        }

        [Test]
        public void DoubloonRainAdvancesTwoLevelsAndNoOpsAtJackpot()
        {
            UnitDefinition lFreebooter = Load<UnitDefinition>("Units/Freebooter.asset");
            UnitDefinition lEnemy = CreateUnit(Team.TeamB, 100, 1, 0);
            IBattleService lBattle = CreateBattle(Spawn(lFreebooter, 0, 0, 0), Spawn(lEnemy, 5, 5, 0));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(lActor.Id, new SkillId("doubloon_rain"), SkillTarget.ForCell(lActor.Position));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            Assert.That(lActor.GetStateProgressionIndex(lActor.ActivePassive.StateProgression), Is.EqualTo(1));

            UnitRuntime lPactoleActor = CreateFreebooterRuntime();
            Assert.That(lPactoleActor.AdvanceStateProgression(lPactoleActor.ActivePassive.StateProgression, 5), Is.True);
            Assert.That(lPactoleActor.AdvanceStateProgression(lPactoleActor.ActivePassive.StateProgression, 2), Is.False);
            Assert.That(lPactoleActor.GetStateProgressionIndex(lPactoleActor.ActivePassive.StateProgression), Is.EqualTo(4));
        }

        private static void AssertSkill(SkillDefinition pSkill, SkillCategory pCategory, int pEnergy, int pRangeMin, int pRangeMax, int pPower, SkillAdditionalEffectType pAdditional, int pDistance, int pUsesPerTurn, int pUsesPerTarget, int pCooldown)
        {
                Assert.That(pSkill.Category, Is.EqualTo(pCategory), pSkill.Id);
                Assert.That(pSkill.EnergyCost, Is.EqualTo(pEnergy), pSkill.Id);
                Assert.That(pSkill.RangeMin, Is.EqualTo(pRangeMin), pSkill.Id);
                Assert.That(pSkill.RangeMax, Is.EqualTo(pRangeMax), pSkill.Id);
                Assert.That(pSkill.Power, Is.EqualTo(pPower), pSkill.Id);
                Assert.That(pSkill.AdditionalEffectType, Is.EqualTo(pAdditional), pSkill.Id);
                Assert.That(pSkill.UsePerTurn, Is.EqualTo(pUsesPerTurn), pSkill.Id);
                Assert.That(pSkill.UsePerTarget, Is.EqualTo(pUsesPerTarget), pSkill.Id);
                Assert.That(pSkill.CooldownTurns, Is.EqualTo(pCooldown), pSkill.Id);
                if (pAdditional == SkillAdditionalEffectType.Pull)
                    Assert.That(pSkill.PushDistance, Is.EqualTo(pDistance), pSkill.Id);
        }

        private static void AssertProgression(StateProgressionDefinition pProgression, StateDefinition pTreasure, StateDefinition pPactole)
        {
            Assert.That(pProgression.LevelCount, Is.EqualTo(5));
            for (int lIndex = 0; lIndex < 4; lIndex++)
            {
                Assert.That(pProgression.GetLevelState(lIndex), Is.SameAs(pTreasure));
                Assert.That(pProgression.GetLevelStacks(lIndex), Is.EqualTo(lIndex + 1));
            }
            Assert.That(pProgression.GetLevelState(4), Is.SameAs(pPactole));
            Assert.That(pProgression.GetLevelStacks(4), Is.EqualTo(1));
        }

        private UnitRuntime CreateFreebooterRuntime() => new UnitRuntime(new UnitId(1), Load<UnitDefinition>("Units/Freebooter.asset"), new GridCoord(0, 0));

        private UnitDefinition CreateUnit(Team pTeam, int pHealth, int pVelocity, int pResistance)
        {
            UnitDefinition lUnit = Track(ScriptableObject.CreateInstance<UnitDefinition>());
            SetField(lUnit, "_Id", $"test-unit-{_ObjectsToDestroy.Count}");
            SetField(lUnit, "_Team", pTeam);
            SetField(lUnit, "_MaxHealth", pHealth);
            SetField(lUnit, "_MobilityPerTurn", 3);
            SetField(lUnit, "_EnergyPerTurn", 10);
            SetField(lUnit, "_Velocity", pVelocity);
            SetField(lUnit, "_MeleeResistancePercent", pResistance);
            SetField(lUnit, "_RangedResistancePercent", pResistance);
            return lUnit;
        }

        private IBattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime("freebooter-tests", "Freebooter Tests", 8, 8, 1, null, pSpawns));
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

        private static T Load<T>(string pRelativePath) where T : Object
        {
            T lAsset = AssetDatabase.LoadAssetAtPath<T>($"{Root}/{pRelativePath}");
            Assert.That(lAsset, Is.Not.Null, pRelativePath);
            return lAsset;
        }

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
