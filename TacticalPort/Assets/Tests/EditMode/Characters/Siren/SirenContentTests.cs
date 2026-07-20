using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.UI;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.State.Tests.Characters.Siren
{
    public sealed class SirenContentTests
    {
        private const string Root = "Assets/AITests/Siren/Data";
        private readonly List<Object> _Objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int lIndex = _Objects.Count - 1; lIndex >= 0; lIndex--)
                if (_Objects[lIndex] != null) Object.DestroyImmediate(_Objects[lIndex]);
            _Objects.Clear();
        }

        [Test]
        public void UnitContainsExactPlayablePoolPassivesAndStats()
        {
            UnitDefinition lSiren = Load<UnitDefinition>("Units/Siren.asset");
            Assert.That(lSiren.Id, Is.EqualTo("siren"));
            Assert.That(lSiren.EnglishDisplayName, Is.EqualTo("Corsair Siren"));
            Assert.That(lSiren.Team, Is.EqualTo(Team.TeamA));
            Assert.That(lSiren.MaxHealth, Is.EqualTo(400));
            Assert.That(lSiren.MobilityPerTurn, Is.EqualTo(3));
            Assert.That(lSiren.EnergyPerTurn, Is.EqualTo(6));
            Assert.That(lSiren.Velocity, Is.EqualTo(65));
            Assert.That(lSiren.MeleeDamagePercent, Is.EqualTo(100));
            Assert.That(lSiren.RangedDamagePercent, Is.EqualTo(105));
            Assert.That(lSiren.MeleeResistancePercent, Is.EqualTo(-5));
            Assert.That(lSiren.RangedResistancePercent, Is.EqualTo(5));
            Assert.That(lSiren.StatPointBudget, Is.EqualTo(120));
            Assert.That(lSiren.ParticipatesInTurnOrder, Is.True);
            Assert.That(lSiren.CountsForVictory, Is.True);
            Assert.That(lSiren.BlocksVisibility, Is.True);
            Assert.That(lSiren.CombatViewPrefabComponent, Is.Not.Null);
            AssertBlockout(lSiren, "Assets/AITests/Siren/Prefabs/Siren_Blockout.prefab");
            Assert.That(lSiren.DisplaySprite, Is.Not.Null);
            Assert.That(lSiren.Skills.Select(pSkill => pSkill.Id), Is.EqualTo(new[]
            {
                "nourishing_coral", "transverse_surge", "gift_of_the_depths", "momentum_song",
                "siphoning_shell", "horizon_conch", "abyssal_maelstrom", "unravel_charms",
                "mending_melody", "salt_erosion"
            }));
            Assert.That(lSiren.Passives.Select(pPassive => pPassive.Id), Is.EqualTo(new[] { "solidarity_tide", "deep_aegis" }));
            Assert.That(lSiren.DefaultPassive, Is.SameAs(lSiren.Passives[0]));
            Assert.That(lSiren.Skills, Has.All.Matches<SkillDefinition>(pSkill => pSkill.Variant == null));
        }

        [Test]
        public void SkillsExposeEffectiveTargetingAndEffects()
        {
            Dictionary<string, SkillDefinition> lSkills = Load<UnitDefinition>("Units/Siren.asset").Skills.ToDictionary(pSkill => pSkill.Id);
            AssertSkill(lSkills["nourishing_coral"], SkillCategory.Utility, 3, 1, 4, 0, SkillAdditionalEffectType.Summon);
            Assert.That(lSkills["nourishing_coral"].SummonUnit.Id, Is.EqualTo("siren_nourishing_coral"));
            AssertSkill(lSkills["transverse_surge"], SkillCategory.RangedAtk, 4, 1, 8, 24, SkillAdditionalEffectType.Push);
            Assert.That(lSkills["transverse_surge"].AoeShape, Is.EqualTo(SkillAoeShape.PerpendicularLine));
            Assert.That(lSkills["transverse_surge"].EffectTargetRelation, Is.EqualTo(SkillTargetRelation.EnemiesOnly));
            Assert.That(lSkills["gift_of_the_depths"].TargetScope, Is.EqualTo(SkillTargetScope.AllMatchingUnits));
            Assert.That(lSkills["gift_of_the_depths"].TargetUnitType, Is.EqualTo(UnitTargetType.CharactersOnly));
            Assert.That(lSkills["gift_of_the_depths"].Power, Is.EqualTo(32));
            Assert.That(lSkills["gift_of_the_depths"].CasterAppliedState.BlocksHealing, Is.True);
            Assert.That(lSkills["gift_of_the_depths"].CasterAppliedStateDurationTurns, Is.EqualTo(3));
            Assert.That(lSkills["momentum_song"].AppliedState.EnergyModifierPerStack, Is.EqualTo(2));
            Assert.That(lSkills["siphoning_shell"].SummonUnit.Id, Is.EqualTo("siren_siphoning_shell"));
            Assert.That(lSkills["horizon_conch"].SummonUnit.Id, Is.EqualTo("siren_horizon_conch"));
            Assert.That(lSkills["abyssal_maelstrom"].UseAoeDamageFalloff, Is.True);
            Assert.That(lSkills["abyssal_maelstrom"].AoeSize, Is.EqualTo(2));
            Assert.That(lSkills["unravel_charms"].AdditionalEffectType, Is.EqualTo(SkillAdditionalEffectType.ReduceStateDurations));
            Assert.That(lSkills["unravel_charms"].StateDurationReduction, Is.EqualTo(2));
            AssertSkill(lSkills["mending_melody"], SkillCategory.Heal, 2, 0, 6, 22, SkillAdditionalEffectType.None);
            Assert.That(lSkills["mending_melody"].UsePerTurn, Is.EqualTo(3));
            Assert.That(lSkills["mending_melody"].UsePerTarget, Is.EqualTo(2));
            Assert.That(lSkills["salt_erosion"].AppliedState.GeneralResistancePercentPerStack, Is.EqualTo(-5));
            Assert.That(lSkills["salt_erosion"].AppliedStateDurationTurns, Is.EqualTo(2));
        }

        [Test]
        public void PassivesGlyphsAndConstructsUseTheGenericModels()
        {
            PassiveDefinition lSolidarity = Load<PassiveDefinition>("Passives/SolidarityTide.asset");
            PassiveDefinition lAegis = Load<PassiveDefinition>("Passives/DeepAegis.asset");
            Assert.That(lSolidarity.AutomaticTargetSelection, Is.EqualTo(PassiveAutomaticTargetSelection.LowestHealthPercentageAlly));
            Assert.That(lSolidarity.AutomaticTargetUnitType, Is.EqualTo(UnitTargetType.CharactersOnly));
            Assert.That(lSolidarity.HealthLossReactionTarget, Is.EqualTo(PassiveHealthLossReactionTarget.MarkedUnitAlliesExceptMarked));
            Assert.That(lSolidarity.HealthLossReactionState.MobilityModifierPerStack, Is.EqualTo(1));
            Assert.That(lSolidarity.ClearReactionStateOnOwnerBeginTurn, Is.False);
            Assert.That(lAegis.HealthLossReactionTarget, Is.EqualTo(PassiveHealthLossReactionTarget.MarkedUnit));
            Assert.That(lAegis.HealthLossReactionState.GeneralResistancePercentPerStack, Is.EqualTo(10));
            Assert.That(lAegis.ClearReactionStateOnOwnerBeginTurn, Is.True);

            AssertGlyph("Units/SirenNourishingCoral.asset", "Siren_NourishingCoral_Blockout", GlyphTriggerTiming.TurnEnd, 1, GlyphEffectType.Heal, true);
            AssertGlyph("Units/SirenSiphoningShell.asset", "Siren_SiphoningShell_Blockout", GlyphTriggerTiming.TurnStart, 2, GlyphEffectType.ApplyState, false);
            AssertGlyph("Units/SirenHorizonConch.asset", "Siren_HorizonConch_Blockout", GlyphTriggerTiming.PersistentPresence, 2, GlyphEffectType.ApplyState, true);
        }

        [Test]
        public void AttachedGlyphUsesSirenAsEffectSourceAndConstructAsLifetimeSource()
        {
            UnitDefinition lSirenDefinition = Load<UnitDefinition>("Units/Siren.asset");
            UnitDefinition lEnemyDefinition = CreateEnemy();
            IBattleService lBattle = CreateBattle(Spawn(lSirenDefinition, 0, 0, 0), Spawn(lEnemyDefinition, 7, 7, 0));
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lSiren), Is.True);

            BattleActionResult lResult = lBattle.UseSkill(lSiren.Id, new SkillId("nourishing_coral"), SkillTarget.ForCell(new GridCoord(1, 0)));

            Assert.That(lResult.IsSuccess, Is.True, lResult.Message);
            UnitRuntime lConstruct = lBattle.Units.Single(pUnit => pUnit.OwnerUnitId == lSiren.Id);
            Assert.That(lConstruct.Definition.ParticipatesInTurnOrder, Is.False);
            Assert.That(lBattle.TurnOrder, Has.None.Matches<UnitRuntime>(pUnit => pUnit.Id == lConstruct.Id));
            Assert.That(lBattle.GetActiveGlyphs(), Is.Not.Empty);
            Assert.That(lBattle.GetActiveGlyphs(), Has.All.Matches<GridGlyphRuntime>(pGlyph =>
                pGlyph.SourceUnitId == lSiren.Id && pGlyph.LifetimeSourceUnitId == lConstruct.Id));
        }

        [Test]
        public void SirenDeathDestroysAllOwnedConstructsAndTheirGlyphs()
        {
            UnitDefinition lSirenDefinition = Load<UnitDefinition>("Units/Siren.asset");
            IBattleService lBattle = CreateBattle(Spawn(lSirenDefinition, 0, 0, 0), Spawn(CreateEnemy(), 7, 7, 0));
            lBattle.TryStartNextTurn(out _); lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lSiren);
            Assert.That(lBattle.UseSkill(lSiren.Id, new SkillId("nourishing_coral"), SkillTarget.ForCell(new GridCoord(1, 0))).IsSuccess, Is.True);
            Assert.That(lBattle.UseSkill(lSiren.Id, new SkillId("siphoning_shell"), SkillTarget.ForCell(new GridCoord(0, 1))).IsSuccess, Is.True);
            Assert.That(lBattle.EndTurn(lSiren.Id).IsSuccess, Is.True);
            lBattle.TryStartNextTurn(out _); UnitRuntime lEnemy = lBattle.ActiveUnit;
            Assert.That(lBattle.EndTurn(lEnemy.Id).IsSuccess, Is.True);
            lBattle.TryStartNextTurn(out _);
            Assert.That(lBattle.UseSkill(lSiren.Id, new SkillId("horizon_conch"), SkillTarget.ForCell(new GridCoord(1, 1))).IsSuccess, Is.True);
            UnitRuntime[] lConstructs = lBattle.Units.Where(pUnit => pUnit.OwnerUnitId == lSiren.Id).ToArray();
            Assert.That(lConstructs, Has.Length.EqualTo(3));
            Assert.That(lBattle.GetActiveGlyphs(), Is.Not.Empty);

            lSiren.ApplyDirectHealthLoss(lSiren.CurrentHealth);
            Assert.That(lBattle.EndTurn(lSiren.Id).IsSuccess, Is.True);

            Assert.That(lConstructs, Has.All.Matches<UnitRuntime>(pUnit => !pUnit.IsAlive));
            Assert.That(lBattle.GetActiveGlyphs(), Is.Empty);
        }

        [Test]
        public void SirenIsAvailableInEveryPlayableRosterPrefab()
        {
            UnitDefinition lSiren = Load<UnitDefinition>("Units/Siren.asset");
            AssertRoster<MainMenuTeamDeckbuildingController>("Assets/Prefabs/UI/UI_Menu_Main.prefab", lSiren);
            AssertRoster<DeckbuildingPanelController>("Assets/Prefabs/UI/UI_Panel_Deckbuilding.prefab", lSiren);
            AssertRoster<TeamSelectionController>("Assets/Prefabs/UI/UI_Popup_TeamSelection.prefab", lSiren);
        }

        private static void AssertSkill(SkillDefinition pSkill, SkillCategory pCategory, int pEnergy, int pMin, int pMax, int pPower, SkillAdditionalEffectType pAdditional)
        {
            Assert.That(pSkill.Category, Is.EqualTo(pCategory)); Assert.That(pSkill.EnergyCost, Is.EqualTo(pEnergy));
            Assert.That(pSkill.RangeMin, Is.EqualTo(pMin)); Assert.That(pSkill.RangeMax, Is.EqualTo(pMax));
            Assert.That(pSkill.Power, Is.EqualTo(pPower)); Assert.That(pSkill.AdditionalEffectType, Is.EqualTo(pAdditional));
        }

        private static void AssertGlyph(string pUnitPath, string pPrefabName, GlyphTriggerTiming pTiming, int pSize, GlyphEffectType pEffect, bool pAlly)
        {
            UnitDefinition lUnit = Load<UnitDefinition>(pUnitPath); GlyphDefinition lGlyph = lUnit.AttachedGlyph;
            Assert.That(lUnit.ParticipatesInTurnOrder, Is.False); Assert.That(lUnit.CountsForVictory, Is.False);
            Assert.That(lUnit.BlocksVisibility, Is.False);
            AssertBlockout(lUnit, $"Assets/AITests/Siren/Prefabs/{pPrefabName}.prefab");
            Assert.That(lUnit.MaxHealth, Is.EqualTo(150)); Assert.That(lGlyph.TriggerTiming, Is.EqualTo(pTiming));
            Assert.That(lGlyph.TargetUnitType, Is.EqualTo(GlyphTargetUnitType.CharactersOnly)); Assert.That(lGlyph.Size, Is.EqualTo(pSize));
            IReadOnlyList<GlyphEffectDefinition> lEffects = pAlly ? lGlyph.AllyEffects : lGlyph.EnemyEffects;
            Assert.That(lEffects, Is.Not.Empty); Assert.That(lEffects[0].Type, Is.EqualTo(pEffect));
        }

        private static void AssertBlockout(UnitDefinition pUnit, string pExpectedPath)
        {
            Assert.That(pUnit.ModelPrefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(pUnit.ModelPrefab), Is.EqualTo(pExpectedPath));
            Renderer[] lRenderers = pUnit.ModelPrefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(lRenderers, Is.Not.Empty);
            Assert.That(lRenderers.SelectMany(pRenderer => pRenderer.sharedMaterials), Has.All.Not.Null);
            Assert.That(pUnit.ModelPrefab.GetComponentInChildren<Animator>(true), Is.Null);
            Assert.That(pUnit.ModelPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Null);
        }

        private static void AssertRoster<T>(string pPath, UnitDefinition pSiren) where T : Component
        {
            GameObject lPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath); T lController = lPrefab.GetComponentInChildren<T>(true);
            SerializedObject lObject = new SerializedObject(lController); SerializedProperty lUnits = lObject.FindProperty("_AvailableUnits");
            Assert.That(Enumerable.Range(0, lUnits.arraySize).Any(pIndex => lUnits.GetArrayElementAtIndex(pIndex).objectReferenceValue == pSiren), Is.True, pPath);
        }

        private UnitDefinition CreateEnemy()
        {
            UnitDefinition lUnit = Track(ScriptableObject.CreateInstance<UnitDefinition>()); SetField(lUnit, "_Id", "siren-test-enemy");
            SetField(lUnit, "_Team", Team.TeamB); SetField(lUnit, "_MaxHealth", 500); SetField(lUnit, "_MobilityPerTurn", 3);
            SetField(lUnit, "_EnergyPerTurn", 6); SetField(lUnit, "_Velocity", 1); return lUnit;
        }
        private IBattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime("siren-tests", "Siren Tests", 10, 10, 1, null, pSpawns));
            IBattleService lBattle = BattleRuntimeCompositionRoot.CreateDefaultBattleService(); lBattle.Initialize(lScenario); return lBattle;
        }
        private static UnitSpawnDefinition Spawn(UnitDefinition pUnit, int pX, int pY, int pSlot) => new UnitSpawnDefinition
        { Unit = pUnit, StartCoordinate = new SerializableGridCoord(pX, pY), TeamSlotIndex = pSlot };
        private static T Load<T>(string pPath) where T : Object
        {
            T lAsset = AssetDatabase.LoadAssetAtPath<T>($"{Root}/{pPath}"); Assert.That(lAsset, Is.Not.Null, pPath); return lAsset;
        }
        private T Track<T>(T pObject) where T : Object { _Objects.Add(pObject); return pObject; }
        private static void SetField<TTarget, TValue>(TTarget pTarget, string pName, TValue pValue)
        {
            FieldInfo lField = typeof(TTarget).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lField, Is.Not.Null, pName); lField.SetValue(pTarget, pValue);
        }
    }
}
