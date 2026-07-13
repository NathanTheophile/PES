#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TacticalPort.AITests.Forban.Editor
{
    public static class ForbanAssetBuilder
    {
        #region _____________________________/ CONSTANTS

        private const string Root = "Assets/AITests/Forban";
        private const string UnitsFolder = Root + "/Data/Units";
        private const string NormalSkillsFolder = Root + "/Data/Skills/Normal";
        private const string PactoleSkillsFolder = Root + "/Data/Skills/Pactole";
        private const string StatesFolder = Root + "/Data/States";
        private const string PassivesFolder = Root + "/Data/Passives";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string UiFolder = Root + "/UI";
        private const string IconsFolder = Root + "/Icons";
        private const string MaterialsFolder = Root + "/Materials";
        private const string ScriptsFolder = Root + "/Scripts";
        private const string TestsFolder = Root + "/Tests";

        #endregion

        #region _____________________________| MENU

        [MenuItem("AITests/Forban/Rebuild Assets")]
        public static void RebuildAssets()
        {
            BuildContext lContext = new BuildContext();
            EnsureFolders();

            lContext.Icons = CreateIcons();
            lContext.Materials = CreateMaterials();
            lContext.States = CreateStates(lContext.Icons);
            lContext.Passives = CreatePassives(lContext.States, lContext.Icons);
            lContext.Prefabs = CreateVisualPrefabs(lContext.Materials);
            lContext.UiPrefab = CreateTreasureUiPrefab(lContext.Icons);
            lContext.Decoys = CreateDecoys(lContext.Prefabs, lContext.Icons);
            CreateSkills(lContext);
            lContext.Forban = CreateForban(lContext);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateGeneratedAssets(false);
            Debug.Log("[Forban] Assets rebuilt in Assets/AITests/Forban.");
        }

        [MenuItem("AITests/Forban/Validate Generated Assets")]
        public static void ValidateGeneratedAssetsMenu() => ValidateGeneratedAssets(true);

        #endregion

        #region _____________________________| BUILD

        private static void EnsureFolders()
        {
            string[] lFolders =
            {
                Root,
                Root + "/Data",
                UnitsFolder,
                Root + "/Data/Skills",
                NormalSkillsFolder,
                PactoleSkillsFolder,
                StatesFolder,
                PassivesFolder,
                PrefabsFolder,
                UiFolder,
                IconsFolder,
                MaterialsFolder,
                ScriptsFolder,
                TestsFolder
            };

            for (int lIndex = 0; lIndex < lFolders.Length; lIndex++)
                EnsureFolder(lFolders[lIndex]);
        }

        private static Dictionary<string, Sprite> CreateIcons()
        {
            Dictionary<string, Color> lColors = new Dictionary<string, Color>
            {
                ["forban"] = new Color(0.08f, 0.24f, 0.48f, 1f),
                ["coffre"] = new Color(1f, 0.72f, 0.18f, 1f),
                ["treasure_stack"] = new Color(1f, 0.84f, 0.25f, 1f),
                ["avarice_full"] = new Color(0.35f, 0.72f, 0.28f, 1f),
                ["fragile"] = new Color(0.84f, 0.22f, 0.18f, 1f),
                ["ebloui"] = new Color(0.95f, 0.9f, 0.42f, 1f),
                ["bravade"] = new Color(0.88f, 0.34f, 0.14f, 1f),
                ["rhum_fortifie"] = new Color(0.38f, 0.74f, 0.78f, 1f),
                ["pactole_rapide"] = new Color(0.96f, 0.62f, 0.18f, 1f),
                ["avarice"] = new Color(0.18f, 0.58f, 0.26f, 1f),
                ["coup_de_crosse"] = new Color(0.52f, 0.32f, 0.18f, 1f),
                ["sabre_crochet"] = new Color(0.68f, 0.18f, 0.18f, 1f),
                ["tir_de_pistolet"] = new Color(0.16f, 0.28f, 0.52f, 1f),
                ["pluie_de_doublons"] = new Color(0.92f, 0.68f, 0.12f, 1f),
                ["canon_de_poche"] = new Color(0.22f, 0.22f, 0.24f, 1f),
                ["bourse_piegee"] = new Color(0.44f, 0.2f, 0.56f, 1f),
                ["pas_de_cote"] = new Color(0.18f, 0.64f, 0.74f, 1f),
                ["grappin_malin"] = new Color(0.36f, 0.36f, 0.42f, 1f),
                ["leurre_dore"] = new Color(0.95f, 0.78f, 0.22f, 1f),
                ["rhum_de_contrebande"] = new Color(0.48f, 0.22f, 0.12f, 1f)
            };

            Dictionary<string, Sprite> lSprites = new Dictionary<string, Sprite>();
            foreach (KeyValuePair<string, Color> lPair in lColors)
                lSprites[lPair.Key] = CreateIconSprite(lPair.Key, lPair.Value);

            return lSprites;
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            Dictionary<string, Material> lMaterials = new Dictionary<string, Material>
            {
                ["forban"] = CreateMaterial("MAT_Forban_Placeholder", new Color(0.05f, 0.22f, 0.46f, 1f)),
                ["coat"] = CreateMaterial("MAT_Forban_Coat", new Color(0.74f, 0.14f, 0.12f, 1f)),
                ["gold"] = CreateMaterial("MAT_Forban_Gold", new Color(1f, 0.72f, 0.16f, 1f)),
                ["heavy_gold"] = CreateMaterial("MAT_Forban_HeavyGold", new Color(0.95f, 0.58f, 0.08f, 1f)),
                ["dark"] = CreateMaterial("MAT_Forban_DarkMetal", new Color(0.08f, 0.08f, 0.1f, 1f))
            };

            return lMaterials;
        }

        private static Dictionary<string, StateDefinition> CreateStates(IReadOnlyDictionary<string, Sprite> pIcons)
        {
            Dictionary<string, StateDefinition> lStates = new Dictionary<string, StateDefinition>
            {
                ["treasure_stack"] = CreateState("State_TreasureStack", "forban_treasure_stack", "TreasureStack", "Forban treasure resource marker.", 0, 5, true, pIcons["treasure_stack"]),
                ["avarice_full"] = CreateState("State_AvariceFull", "forban_avarice_full", "AvariceFull", "Applied while Avarice has a full chest.", 0, 1, false, pIcons["avarice_full"], 10, 0, 0, 10, 10, 0, 0, 1),
                ["fragile"] = CreateState("State_Fragile", "forban_fragile", "Fragile", "Reduces melee and ranged resistance.", 1, 2, false, pIcons["fragile"], 0, 0, 0, -10, -10),
                ["ebloui"] = CreateState("State_Ebloui", "forban_ebloui", "\u00C9bloui", "Reduces range.", 1, 1, false, pIcons["ebloui"], 0, 0, 0, 0, 0, -1),
                ["bravade"] = CreateState("State_Bravade", "forban_bravade", "Bravade", "Increases damage.", 1, 1, false, pIcons["bravade"], 15),
                ["rhum_fortifie"] = CreateState("State_RhumFortifie", "forban_rhum_fortifie", "Rhum Fortifi\u00E9", "Increases melee and ranged resistance.", 1, 1, false, pIcons["rhum_fortifie"], 0, 0, 0, 15, 15)
            };

            return lStates;
        }

        private static Dictionary<string, PassiveDefinition> CreatePassives(
            IReadOnlyDictionary<string, StateDefinition> pStates,
            IReadOnlyDictionary<string, Sprite> pIcons)
        {
            Dictionary<string, PassiveDefinition> lPassives = new Dictionary<string, PassiveDefinition>
            {
                ["pactole_rapide"] = CreatePassive(
                    "Passive_PactoleRapide",
                    "forban_pactole_rapide",
                    "Pactole Rapide",
                    "Pactole refunds one treasure after hitting an enemy.",
                    pIcons["pactole_rapide"],
                    pStates["treasure_stack"],
                    null,
                    true),
                ["avarice"] = CreatePassive(
                    "Passive_Avarice",
                    "forban_avarice",
                    "Avarice",
                    "Full chest grants tempo stats until Pactole is spent.",
                    pIcons["avarice"],
                    pStates["treasure_stack"],
                    pStates["avarice_full"],
                    false)
            };

            return lPassives;
        }

        private static Dictionary<string, GameObject> CreateVisualPrefabs(IReadOnlyDictionary<string, Material> pMaterials)
        {
            Dictionary<string, GameObject> lPrefabs = new Dictionary<string, GameObject>
            {
                ["forban"] = SaveModelPrefab("PF_Forban_Placeholder", pMaterials["forban"], pMaterials["coat"], PrimitiveType.Capsule),
                ["gold_decoy"] = SaveModelPrefab("PF_GoldDecoy_Placeholder", pMaterials["gold"], pMaterials["dark"], PrimitiveType.Cube),
                ["heavy_gold_decoy"] = SaveModelPrefab("PF_HeavyGoldDecoy_Placeholder", pMaterials["heavy_gold"], pMaterials["dark"], PrimitiveType.Cube, 1.18f)
            };

            return lPrefabs;
        }

        private static Dictionary<string, UnitDefinition> CreateDecoys(
            IReadOnlyDictionary<string, GameObject> pPrefabs,
            IReadOnlyDictionary<string, Sprite> pIcons)
        {
            Dictionary<string, UnitDefinition> lUnits = new Dictionary<string, UnitDefinition>
            {
                ["gold_decoy"] = CreateUnit(
                    "Unit_GoldDecoy",
                    "forban_gold_decoy",
                    "GoldDecoy",
                    "Forban light decoy. Blocks occupation and line of sight through normal unit occupancy.",
                    Team.Neutral,
                    60,
                    0,
                    0,
                    0,
                    100,
                    100,
                    0,
                    0,
                    0,
                    false,
                    false,
                    pPrefabs["gold_decoy"],
                    pIcons["leurre_dore"],
                    null,
                    null),
                ["heavy_gold_decoy"] = CreateUnit(
                    "Unit_HeavyGoldDecoy",
                    "forban_heavy_gold_decoy",
                    "HeavyGoldDecoy",
                    "Forban heavy decoy. Blocks occupation and line of sight through normal unit occupancy.",
                    Team.Neutral,
                    100,
                    0,
                    0,
                    0,
                    100,
                    100,
                    0,
                    0,
                    0,
                    false,
                    false,
                    pPrefabs["heavy_gold_decoy"],
                    pIcons["leurre_dore"],
                    null,
                    null)
            };

            return lUnits;
        }

        private static void CreateSkills(BuildContext pContext)
        {
            SkillDefinition lCoupDeCrossePactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_CoupDeCrosse",
                "forban_coup_de_crosse",
                "Coup de Crosse Pactole",
                SkillCategory.MeleeAtk,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.Push,
                34,
                2,
                1,
                1,
                true,
                SkillTargetAlignment.Orthogonal,
                SkillAoeShape.Single,
                0,
                false,
                2,
                1,
                0,
                3,
                pContext.Icons["coup_de_crosse"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["fragile"],
                1,
                1,
                true);

            SkillDefinition lSabreCrochetPactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_SabreCrochet",
                "forban_sabre_crochet",
                "Sabre-Crochet Pactole",
                SkillCategory.MeleeAtk,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.SwitchPositions,
                42,
                3,
                1,
                1,
                true,
                SkillTargetAlignment.Any,
                SkillAoeShape.Single,
                0,
                false,
                1,
                1,
                0,
                0,
                pContext.Icons["sabre_crochet"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["fragile"],
                2,
                1,
                true);

            SkillDefinition lTirPactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_TirDePistolet",
                "forban_tir_de_pistolet",
                "Tir de Pistolet Pactole",
                SkillCategory.RangedAtk,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.None,
                42,
                3,
                2,
                6,
                false,
                SkillTargetAlignment.Any,
                SkillAoeShape.Single,
                0,
                false,
                2,
                1,
                0,
                0,
                pContext.Icons["tir_de_pistolet"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                true);

            SkillDefinition lPluiePactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_PluieDeDoublons",
                "forban_pluie_de_doublons",
                "Pluie de Doublons Pactole",
                SkillCategory.RangedAtk,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.None,
                28,
                3,
                1,
                4,
                true,
                SkillTargetAlignment.Orthogonal,
                SkillAoeShape.Cone,
                3,
                true,
                1,
                0,
                1,
                0,
                pContext.Icons["pluie_de_doublons"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["ebloui"],
                1,
                1,
                true);

            SkillDefinition lCanonPactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_CanonDePoche",
                "forban_canon_de_poche",
                "Canon de Poche Pactole",
                SkillCategory.RangedAtk,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.Push,
                44,
                4,
                2,
                4,
                true,
                SkillTargetAlignment.Orthogonal,
                SkillAoeShape.Cross,
                1,
                true,
                1,
                0,
                1,
                3,
                pContext.Icons["canon_de_poche"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                true);

            SkillDefinition lBoursePactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_BoursePiegee",
                "forban_bourse_piegee",
                "Bourse Pi\u00E9g\u00E9e Pactole",
                SkillCategory.RangedAtk,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.CreateGlyph,
                32,
                3,
                2,
                4,
                true,
                SkillTargetAlignment.Any,
                SkillAoeShape.Circle,
                1,
                false,
                1,
                0,
                2,
                0,
                pContext.Icons["bourse_piegee"],
                null,
                SkillSummonTeamRule.Definition,
                2,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["fragile"],
                1,
                1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["fragile"],
                1,
                1,
                true);

            SkillDefinition lPasDeCotePactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_PasDeCote",
                "forban_pas_de_cote",
                "Pas de C\u00F4t\u00E9 Pactole",
                SkillCategory.Utility,
                SkillPrimaryEffectType.None,
                SkillAdditionalEffectType.Teleport,
                0,
                2,
                1,
                5,
                false,
                SkillTargetAlignment.Any,
                SkillAoeShape.Single,
                0,
                false,
                1,
                0,
                1,
                0,
                pContext.Icons["pas_de_cote"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["bravade"],
                1,
                1,
                true,
                true);

            SkillDefinition lGrappinPactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_GrappinMalin",
                "forban_grappin_malin",
                "Grappin Malin Pactole",
                SkillCategory.Utility,
                SkillPrimaryEffectType.None,
                SkillAdditionalEffectType.SwitchPositions,
                0,
                3,
                1,
                6,
                false,
                SkillTargetAlignment.Any,
                SkillAoeShape.Single,
                0,
                false,
                1,
                0,
                2,
                0,
                pContext.Icons["grappin_malin"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                true,
                false);

            SkillDefinition lLeurrePactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_LeurreDore",
                "forban_leurre_dore",
                "Leurre Dor\u00E9 Pactole",
                SkillCategory.Utility,
                SkillPrimaryEffectType.None,
                SkillAdditionalEffectType.Summon,
                0,
                3,
                1,
                5,
                true,
                SkillTargetAlignment.Any,
                SkillAoeShape.Single,
                0,
                false,
                1,
                0,
                2,
                0,
                pContext.Icons["leurre_dore"],
                pContext.Decoys["heavy_gold_decoy"],
                SkillSummonTeamRule.Caster,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                pContext.States["ebloui"],
                1,
                1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                true);

            SkillDefinition lRhumPactole = CreateSkill(
                PactoleSkillsFolder,
                "Skill_Pactole_RhumDeContrebande",
                "forban_rhum_de_contrebande",
                "Rhum de Contrebande Pactole",
                SkillCategory.Heal,
                SkillPrimaryEffectType.Heal,
                SkillAdditionalEffectType.None,
                52,
                3,
                0,
                4,
                true,
                SkillTargetAlignment.Any,
                SkillAoeShape.Single,
                0,
                false,
                1,
                1,
                2,
                0,
                pContext.Icons["rhum_de_contrebande"],
                null,
                SkillSummonTeamRule.Definition,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                null,
                1,
                -1,
                null,
                1,
                -1,
                1,
                SkillGlyphTargetRule.EnemiesOnly,
                pContext.States["rhum_fortifie"],
                1,
                1,
                true);

            pContext.NormalSkills = new List<SkillDefinition>
            {
                CreateSkill(NormalSkillsFolder, "Skill_CoupDeCrosse", "forban_coup_de_crosse", "Coup de Crosse", SkillCategory.MeleeAtk, SkillPrimaryEffectType.Damage, SkillAdditionalEffectType.Push, 24, 2, 1, 1, true, SkillTargetAlignment.Orthogonal, SkillAoeShape.Single, 0, false, 2, 1, 0, 1, pContext.Icons["coup_de_crosse"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lCoupDeCrossePactole),
                CreateSkill(NormalSkillsFolder, "Skill_SabreCrochet", "forban_sabre_crochet", "Sabre-Crochet", SkillCategory.MeleeAtk, SkillPrimaryEffectType.Damage, SkillAdditionalEffectType.None, 32, 3, 1, 1, true, SkillTargetAlignment.Any, SkillAoeShape.Single, 0, false, 1, 1, 0, 0, pContext.Icons["sabre_crochet"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, pContext.States["fragile"], 1, 1, false, true, lSabreCrochetPactole),
                CreateSkill(NormalSkillsFolder, "Skill_TirDePistolet", "forban_tir_de_pistolet", "Tir de Pistolet", SkillCategory.RangedAtk, SkillPrimaryEffectType.Damage, SkillAdditionalEffectType.None, 30, 3, 2, 5, true, SkillTargetAlignment.Any, SkillAoeShape.Single, 0, false, 2, 1, 0, 0, pContext.Icons["tir_de_pistolet"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lTirPactole),
                CreateSkill(NormalSkillsFolder, "Skill_PluieDeDoublons", "forban_pluie_de_doublons", "Pluie de Doublons", SkillCategory.RangedAtk, SkillPrimaryEffectType.Damage, SkillAdditionalEffectType.None, 20, 3, 1, 3, true, SkillTargetAlignment.Orthogonal, SkillAoeShape.Cone, 2, true, 1, 0, 1, 0, pContext.Icons["pluie_de_doublons"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lPluiePactole),
                CreateSkill(NormalSkillsFolder, "Skill_CanonDePoche", "forban_canon_de_poche", "Canon de Poche", SkillCategory.RangedAtk, SkillPrimaryEffectType.Damage, SkillAdditionalEffectType.Push, 34, 4, 2, 4, true, SkillTargetAlignment.Orthogonal, SkillAoeShape.Single, 0, false, 1, 0, 1, 2, pContext.Icons["canon_de_poche"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lCanonPactole),
                CreateSkill(NormalSkillsFolder, "Skill_BoursePiegee", "forban_bourse_piegee", "Bourse Pi\u00E9g\u00E9e", SkillCategory.RangedAtk, SkillPrimaryEffectType.Damage, SkillAdditionalEffectType.CreateGlyph, 22, 3, 2, 4, true, SkillTargetAlignment.Any, SkillAoeShape.Single, 0, false, 1, 0, 2, 0, pContext.Icons["bourse_piegee"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, pContext.States["fragile"], 1, 1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lBoursePactole),
                CreateSkill(NormalSkillsFolder, "Skill_PasDeCote", "forban_pas_de_cote", "Pas de C\u00F4t\u00E9", SkillCategory.Utility, SkillPrimaryEffectType.None, SkillAdditionalEffectType.Teleport, 0, 2, 1, 3, false, SkillTargetAlignment.Any, SkillAoeShape.Single, 0, false, 1, 0, 2, 0, pContext.Icons["pas_de_cote"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lPasDeCotePactole),
                CreateSkill(NormalSkillsFolder, "Skill_GrappinMalin", "forban_grappin_malin", "Grappin Malin", SkillCategory.Utility, SkillPrimaryEffectType.None, SkillAdditionalEffectType.SwitchPositions, 0, 3, 1, 4, true, SkillTargetAlignment.Orthogonal, SkillAoeShape.Single, 0, false, 1, 0, 3, 0, pContext.Icons["grappin_malin"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, false, lGrappinPactole),
                CreateSkill(NormalSkillsFolder, "Skill_LeurreDore", "forban_leurre_dore", "Leurre Dor\u00E9", SkillCategory.Utility, SkillPrimaryEffectType.None, SkillAdditionalEffectType.Summon, 0, 3, 1, 3, true, SkillTargetAlignment.Any, SkillAoeShape.Single, 0, false, 1, 0, 3, 0, pContext.Icons["leurre_dore"], pContext.Decoys["gold_decoy"], SkillSummonTeamRule.Caster, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lLeurrePactole),
                CreateSkill(NormalSkillsFolder, "Skill_RhumDeContrebande", "forban_rhum_de_contrebande", "Rhum de Contrebande", SkillCategory.Heal, SkillPrimaryEffectType.Heal, SkillAdditionalEffectType.None, 30, 3, 0, 3, true, SkillTargetAlignment.Any, SkillAoeShape.Single, 0, false, 1, 1, 2, 0, pContext.Icons["rhum_de_contrebande"], null, SkillSummonTeamRule.Definition, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, null, 1, -1, 1, SkillGlyphTargetRule.EnemiesOnly, null, 1, -1, false, true, lRhumPactole)
            };
        }

        private static UnitDefinition CreateForban(BuildContext pContext)
        {
            return CreateUnit(
                "Unit_Forban",
                "forban",
                "Forban",
                "DPS tactique opportuniste autour de Coffre et Pactole.",
                Team.TeamA,
                420,
                3,
                6,
                55,
                100,
                105,
                0,
                0,
                15,
                true,
                true,
                pContext.Prefabs["forban"],
                pContext.Icons["forban"],
                pContext.NormalSkills,
                new List<PassiveDefinition> { pContext.Passives["pactole_rapide"], pContext.Passives["avarice"] },
                pContext.Passives["pactole_rapide"]);
        }

        #endregion

        #region _____________________________| VALIDATION

        private static void ValidateGeneratedAssets(bool pThrowOnFailure)
        {
            List<string> lFailures = new List<string>();

            UnitDefinition lForban = Load<UnitDefinition>(UnitsFolder + "/Unit_Forban.asset");
            UnitDefinition lGoldDecoy = Load<UnitDefinition>(UnitsFolder + "/Unit_GoldDecoy.asset");
            UnitDefinition lHeavyGoldDecoy = Load<UnitDefinition>(UnitsFolder + "/Unit_HeavyGoldDecoy.asset");
            StateDefinition lFragile = Load<StateDefinition>(StatesFolder + "/State_Fragile.asset");
            StateDefinition lEbloui = Load<StateDefinition>(StatesFolder + "/State_Ebloui.asset");
            StateDefinition lAvariceFull = Load<StateDefinition>(StatesFolder + "/State_AvariceFull.asset");
            StateDefinition lRhumFortifie = Load<StateDefinition>(StatesFolder + "/State_RhumFortifie.asset");
            PassiveDefinition lAvarice = Load<PassiveDefinition>(PassivesFolder + "/Passive_Avarice.asset");

            Check(lForban != null, "Forban unit asset exists.", lFailures);
            if (lForban == null)
            {
                ReportValidation(lFailures, pThrowOnFailure);
                return;
            }

            Check(lForban.Skills.Count == 10, "Forban has 10 normal skills.", lFailures);
            for (int lIndex = 0; lIndex < lForban.Skills.Count; lIndex++)
                Check(lForban.Skills[lIndex] != null && lForban.Skills[lIndex].PactoleVariant != null, $"Skill {lIndex + 1} has a Pactole variant.", lFailures);

            Check(lForban.MaxHealth == 420 && lForban.MoveRange == 3 && lForban.ActionPointsPerTurn == 6, "Forban base resources are correct.", lFailures);
            Check(lForban.MeleeDamagePercent == 100 && lForban.RangedDamagePercent == 105, "Forban damage percentages are correct.", lFailures);
            Check(lForban.PushDamageBonus == 15, "Forban push damage bonus is correct.", lFailures);
            Check(lFragile != null && lFragile.MeleeResistancePercentPerStack == -10 && lFragile.RangedResistancePercentPerStack == -10, "Fragile applies negative resistance modifiers.", lFailures);
            Check(lGoldDecoy != null && !lGoldDecoy.ParticipatesInTurnOrder && !lGoldDecoy.CountsForVictory, "GoldDecoy does not play turns or count for victory.", lFailures);
            Check(lHeavyGoldDecoy != null && !lHeavyGoldDecoy.ParticipatesInTurnOrder && !lHeavyGoldDecoy.CountsForVictory, "HeavyGoldDecoy does not play turns or count for victory.", lFailures);

            SkillDefinition lRhum = FindSkill(lForban, "forban_rhum_de_contrebande");
            Check(lRhum != null && lRhum.PactoleVariant != null && lRhum.PactoleVariant.AppliedState == lRhumFortifie, "Rhum Pactole applies RhumFortifie.", lFailures);

            SkillDefinition lLeurre = FindSkill(lForban, "forban_leurre_dore");
            Check(lLeurre != null && lLeurre.SummonUnit == lGoldDecoy, "Leurre Dore summons GoldDecoy.", lFailures);
            Check(lLeurre != null && lLeurre.PactoleVariant != null && lLeurre.PactoleVariant.SummonUnit == lHeavyGoldDecoy, "Leurre Dore Pactole summons HeavyGoldDecoy.", lFailures);
            Check(lLeurre != null && lLeurre.PactoleVariant != null && lLeurre.PactoleVariant.SummonSpawnState == lEbloui, "HeavyGoldDecoy applies Ebloui on spawn.", lFailures);

            ValidateRuntimeRules(lForban, lFragile, lAvariceFull, lAvarice, lEbloui, lFailures);
            ReportValidation(lFailures, pThrowOnFailure);
        }

        private static void ValidateRuntimeRules(
            UnitDefinition pForban,
            StateDefinition pFragile,
            StateDefinition pAvariceFull,
            PassiveDefinition pAvarice,
            StateDefinition pEbloui,
            ICollection<string> pFailures)
        {
            UnitRuntime lRuntime = new UnitRuntime(new UnitId(1), pForban, new GridCoord(0, 0));
            Check(lRuntime.AddTreasure(1) == 1 && lRuntime.TreasureCount == 1, "Normal treasure gain increments the chest.", pFailures);
            Check(lRuntime.AddTreasure(4) == 1 && lRuntime.TreasureCount == 2, "Treasure gain is capped at 2 per turn.", pFailures);
            lRuntime.BeginTurn();
            Check(lRuntime.AddTreasure(5, false) == 3 && lRuntime.TreasureCount == 5, "Chest caps at 5 treasures.", pFailures);

            SkillDefinition lTir = FindSkill(pForban, "forban_tir_de_pistolet");
            Check(lRuntime.ResolveSkillForExecution(lTir) == lTir.PactoleVariant, "Full chest resolves to Pactole variant.", pFailures);
            Check(lRuntime.TryConsumePactoleTreasure() && lRuntime.TreasureCount == 0, "Pactole consumption removes 5 treasures.", pFailures);

            UnitRuntime lFragileTarget = new UnitRuntime(new UnitId(2), pForban, new GridCoord(1, 0), Team.TeamB);
            lFragileTarget.TryApplyState(pFragile, 1, 1);
            Check(lFragileTarget.GetMeleeResistancePercent() == -10 && lFragileTarget.GetRangedResistancePercent() == -10, "Negative resistance modifiers remain negative at runtime.", pFailures);

            Check(lRuntime.TrySetActivePassive(pAvarice), "Avarice can be selected on Forban.", pFailures);
            lRuntime.AddTreasure(5, false);
            Check(lRuntime.HasState(pAvariceFull), "AvariceFull applies while chest is full.", pFailures);
            lRuntime.TryConsumePactoleTreasure();
            Check(!lRuntime.HasState(pAvariceFull), "AvariceFull is removed after treasure consumption.", pFailures);

            ValidateBattleTreasureGain(pForban, pFailures);
            ValidatePactoleBattleFlow(pForban, pEbloui, pFailures);
        }

        private static void ValidateBattleTreasureGain(UnitDefinition pForban, ICollection<string> pFailures)
        {
            SkillDefinition lTir = FindSkill(pForban, "forban_tir_de_pistolet");
            BattleService lBattle = CreateBattle(
                Spawn(pForban, 0, 0),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamB), 3, 0));

            lBattle.TryStartNextTurn(out _);
            BattleActionResult lResult = lBattle.UseSkill(new UnitId(1), new SkillId(lTir.Id), SkillTarget.ForCell(new GridCoord(3, 0)));
            lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lForbanRuntime);
            Check(lResult.IsSuccess && lForbanRuntime.TreasureCount == 1, "Normal RangedAtk hit grants 1 treasure.", pFailures);

            SkillDefinition lPluie = FindSkill(pForban, "forban_pluie_de_doublons");
            BattleService lAoeBattle = CreateBattle(
                Spawn(pForban, 0, 0),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamA), 6, 6),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamB), 1, 0),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamB), 2, 0));

            lAoeBattle.TryStartNextTurn(out _);
            BattleActionResult lAoeResult = lAoeBattle.UseSkill(new UnitId(1), new SkillId(lPluie.Id), SkillTarget.ForCell(new GridCoord(1, 0)));
            lAoeBattle.TryGetUnit(new UnitId(1), out UnitRuntime lAoeForban);
            Check(lAoeResult.IsSuccess && lAoeForban.TreasureCount == 1, "AoE hit grants only 1 treasure.", pFailures);

            SkillDefinition lPasDeCote = FindSkill(pForban, "forban_pas_de_cote");
            BattleService lUtilityBattle = CreateBattle(
                Spawn(pForban, 0, 0),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamB), 4, 4));

            lUtilityBattle.TryStartNextTurn(out _);
            BattleActionResult lUtilityResult = lUtilityBattle.UseSkill(new UnitId(1), new SkillId(lPasDeCote.Id), SkillTarget.ForCell(new GridCoord(0, 1)));
            lUtilityBattle.TryGetUnit(new UnitId(1), out UnitRuntime lUtilityForban);
            Check(lUtilityResult.IsSuccess && lUtilityForban.TreasureCount == 0, "Utility skill does not grant treasure.", pFailures);
        }

        private static void ValidatePactoleBattleFlow(UnitDefinition pForban, StateDefinition pEbloui, ICollection<string> pFailures)
        {
            SkillDefinition lTir = FindSkill(pForban, "forban_tir_de_pistolet");
            BattleService lPactoleBattle = CreateBattle(
                Spawn(pForban, 0, 0),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamB), 3, 0));

            lPactoleBattle.TryStartNextTurn(out _);
            lPactoleBattle.TryGetUnit(new UnitId(1), out UnitRuntime lForbanRuntime);
            lForbanRuntime.AddTreasure(5, false);
            BattleActionResult lPactoleResult = lPactoleBattle.UseSkill(new UnitId(1), new SkillId(lTir.Id), SkillTarget.ForCell(new GridCoord(3, 0)));
            Check(lPactoleResult.IsSuccess && lForbanRuntime.TreasureCount == 1, "Pactole Rapide refunds 1 treasure after Pactole hits an enemy.", pFailures);

            SkillDefinition lLeurre = FindSkill(pForban, "forban_leurre_dore");
            BattleService lSummonBattle = CreateBattle(
                Spawn(pForban, 0, 0),
                Spawn(UnitDefinition.CreateRuntimeClone(pForban, Team.TeamB), 2, 1));

            lSummonBattle.TryStartNextTurn(out _);
            lSummonBattle.TryGetUnit(new UnitId(1), out UnitRuntime lSummoner);
            lSummoner.AddTreasure(5, false);
            BattleActionResult lSummonResult = lSummonBattle.UseSkill(new UnitId(1), new SkillId(lLeurre.Id), SkillTarget.ForCell(new GridCoord(2, 0)));
            lSummonBattle.TryGetUnit(new UnitId(2), out UnitRuntime lEnemy);
            Check(lSummonResult.IsSuccess && lEnemy.HasState(pEbloui), "HeavyGoldDecoy applies Ebloui to adjacent enemies on spawn.", pFailures);
        }

        #endregion

        #region _____________________________| ASSET HELPERS

        private static SkillDefinition CreateSkill(
            string pFolder,
            string pFileName,
            string pId,
            string pDisplayName,
            SkillCategory pCategory,
            SkillPrimaryEffectType pPrimaryEffect,
            SkillAdditionalEffectType pAdditionalEffect,
            int pPower,
            int pActionPointCost,
            int pRangeMin,
            int pRangeMax,
            bool pRequiresLineOfSight,
            SkillTargetAlignment pAlignment,
            SkillAoeShape pAoeShape,
            int pAoeSize,
            bool pUseAoeFalloff,
            int pUsePerTurn,
            int pUsePerTarget,
            int pCooldown,
            int pPushDistance,
            Sprite pIcon,
            UnitDefinition pSummonUnit,
            SkillSummonTeamRule pSummonTeamRule,
            int pGlyphDurationTurns,
            SkillGlyphTargetRule pGlyphTargetRule,
            StateDefinition pGlyphState,
            int pGlyphStateStacks,
            int pGlyphStateDurationTurns,
            StateDefinition pSummonSpawnState,
            int pSummonSpawnStateStacks,
            int pSummonSpawnStateDurationTurns,
            int pSummonSpawnStateAreaSize,
            SkillGlyphTargetRule pSummonSpawnStateTargetRule,
            StateDefinition pAppliedState,
            int pAppliedStateStacks,
            int pAppliedStateDurationTurns,
            bool pIsPactoleVariant,
            bool pCanAffectCaster = true,
            SkillDefinition pPactoleVariant = null)
        {
            SkillDefinition lAsset = LoadOrCreate<SkillDefinition>($"{pFolder}/{pFileName}.asset");
            SerializedObject lSerializedObject = new SerializedObject(lAsset);
            SetString(lSerializedObject, "_Id", pId);
            SetString(lSerializedObject, "_DisplayName", pDisplayName);
            SetString(lSerializedObject, "_Description", string.Empty);
            SetObject(lSerializedObject, "_Icon", pIcon);
            SetEnum(lSerializedObject, "_Category", (int)pCategory);
            SetEnum(lSerializedObject, "_PrimaryEffectType", (int)pPrimaryEffect);
            SetEnum(lSerializedObject, "_AdditionalEffectType", (int)pAdditionalEffect);
            SetBool(lSerializedObject, "_CanAffectCaster", pCanAffectCaster);
            SetInt(lSerializedObject, "_RangeMin", pRangeMin);
            SetInt(lSerializedObject, "_RangeMax", pRangeMax);
            SetEnum(lSerializedObject, "_TargetAlignment", (int)pAlignment);
            SetBool(lSerializedObject, "_RequiresLineOfSight", pRequiresLineOfSight);
            SetEnum(lSerializedObject, "_AoeShape", (int)pAoeShape);
            SetInt(lSerializedObject, "_AoeSize", pAoeShape == SkillAoeShape.Single ? 0 : pAoeSize);
            SetInt(lSerializedObject, "_AoeDamageFalloffPercentPerCell", pUseAoeFalloff ? SkillDefinition.FixedAoeDamageFalloffPercentPerCell : 0);
            SetInt(lSerializedObject, "_ActionPointCost", pActionPointCost);
            SetInt(lSerializedObject, "_UsePerTurn", pUsePerTurn);
            SetInt(lSerializedObject, "_UsePerTarget", pUsePerTarget);
            SetInt(lSerializedObject, "_CooldownTurns", pCooldown);
            SetBool(lSerializedObject, "_IsPactoleVariant", pIsPactoleVariant);
            SetObject(lSerializedObject, "_PactoleVariant", pIsPactoleVariant ? null : pPactoleVariant);
            SetInt(lSerializedObject, "_Power", pPower);
            SetInt(lSerializedObject, "_PushDistance", pPushDistance);
            SetObject(lSerializedObject, "_SummonUnit", pSummonUnit);
            SetEnum(lSerializedObject, "_SummonTeamRule", (int)pSummonTeamRule);
            SetInt(lSerializedObject, "_GlyphDurationTurns", pGlyphDurationTurns);
            SetEnum(lSerializedObject, "_GlyphTargetRule", (int)pGlyphTargetRule);
            SetObject(lSerializedObject, "_GlyphAppliedState", pGlyphState);
            SetInt(lSerializedObject, "_GlyphAppliedStateStacks", pGlyphStateStacks);
            SetInt(lSerializedObject, "_GlyphAppliedStateDurationTurns", pGlyphStateDurationTurns);
            SetObject(lSerializedObject, "_SummonSpawnState", pSummonSpawnState);
            SetInt(lSerializedObject, "_SummonSpawnStateStacks", pSummonSpawnStateStacks);
            SetInt(lSerializedObject, "_SummonSpawnStateDurationTurns", pSummonSpawnStateDurationTurns);
            SetInt(lSerializedObject, "_SummonSpawnStateAreaSize", pSummonSpawnStateAreaSize);
            SetEnum(lSerializedObject, "_SummonSpawnStateTargetRule", (int)pSummonSpawnStateTargetRule);
            SetObject(lSerializedObject, "_AppliedState", pAppliedState);
            SetInt(lSerializedObject, "_AppliedStateStacks", pAppliedStateStacks);
            SetInt(lSerializedObject, "_AppliedStateDurationTurns", pAppliedStateDurationTurns);
            Apply(lSerializedObject, lAsset);
            return lAsset;
        }

        private static StateDefinition CreateState(
            string pFileName,
            string pId,
            string pDisplayName,
            string pDescription,
            int pDurationTurns,
            int pMaxStacks,
            bool pPassiveMarker,
            Sprite pIcon,
            int pDamageModifier = 0,
            int pMeleeDamageModifier = 0,
            int pRangedDamageModifier = 0,
            int pMeleeResistanceModifier = 0,
            int pRangedResistanceModifier = 0,
            int pRangeModifier = 0,
            int pActionPointModifier = 0,
            int pMovementModifier = 0)
        {
            StateDefinition lAsset = LoadOrCreate<StateDefinition>($"{StatesFolder}/{pFileName}.asset");
            SerializedObject lSerializedObject = new SerializedObject(lAsset);
            SetString(lSerializedObject, "_Id", pId);
            SetString(lSerializedObject, "_DisplayName", pDisplayName);
            SetString(lSerializedObject, "_Description", pDescription);
            SetObject(lSerializedObject, "_Icon", pIcon);
            SetInt(lSerializedObject, "_DurationTurns", pDurationTurns);
            SetInt(lSerializedObject, "_MaxStacks", pMaxStacks);
            SetBool(lSerializedObject, "_IsPassiveMarker", pPassiveMarker);
            SetInt(lSerializedObject, "_DamageModifierPerStack", pDamageModifier);
            SetInt(lSerializedObject, "_MeleeDamageModifierPerStack", pMeleeDamageModifier);
            SetInt(lSerializedObject, "_RangedDamageModifierPerStack", pRangedDamageModifier);
            SetInt(lSerializedObject, "_MeleeResistancePercentPerStack", pMeleeResistanceModifier);
            SetInt(lSerializedObject, "_RangedResistancePercentPerStack", pRangedResistanceModifier);
            SetInt(lSerializedObject, "_RangeModifierPerStack", pRangeModifier);
            SetInt(lSerializedObject, "_ActionPointModifierPerStack", pActionPointModifier);
            SetInt(lSerializedObject, "_MovementModifierPerStack", pMovementModifier);
            Apply(lSerializedObject, lAsset);
            return lAsset;
        }

        private static PassiveDefinition CreatePassive(
            string pFileName,
            string pId,
            string pDisplayName,
            string pDescription,
            Sprite pIcon,
            StateDefinition pTreasureStackState,
            StateDefinition pFullTreasureState,
            bool pRefundTreasureOnPactoleHit)
        {
            PassiveDefinition lAsset = LoadOrCreate<PassiveDefinition>($"{PassivesFolder}/{pFileName}.asset");
            SerializedObject lSerializedObject = new SerializedObject(lAsset);
            SetString(lSerializedObject, "_Id", pId);
            SetString(lSerializedObject, "_DisplayName", pDisplayName);
            SetString(lSerializedObject, "_Description", pDescription);
            SetObject(lSerializedObject, "_Icon", pIcon);
            SetBool(lSerializedObject, "_UsesTreasureResource", true);
            SetInt(lSerializedObject, "_MaxTreasure", 5);
            SetObject(lSerializedObject, "_TreasureStackState", pTreasureStackState);
            SetObject(lSerializedObject, "_FullTreasureState", pFullTreasureState);
            SetBool(lSerializedObject, "_RefundTreasureOnPactoleHit", pRefundTreasureOnPactoleHit);
            Apply(lSerializedObject, lAsset);
            return lAsset;
        }

        private static UnitDefinition CreateUnit(
            string pFileName,
            string pId,
            string pDisplayName,
            string pDescription,
            Team pTeam,
            int pMaxHealth,
            int pMoveRange,
            int pActionPoints,
            int pInitiative,
            int pMeleeDamage,
            int pRangedDamage,
            int pMeleeResistance,
            int pRangedResistance,
            int pPushDamageBonus,
            bool pParticipatesInTurnOrder,
            bool pCountsForVictory,
            GameObject pModelPrefab,
            Sprite pIcon,
            IReadOnlyList<SkillDefinition> pSkills,
            IReadOnlyList<PassiveDefinition> pPassives,
            PassiveDefinition pDefaultPassive = null)
        {
            UnitDefinition lAsset = LoadOrCreate<UnitDefinition>($"{UnitsFolder}/{pFileName}.asset");
            SerializedObject lSerializedObject = new SerializedObject(lAsset);
            SetString(lSerializedObject, "_Id", pId);
            SetString(lSerializedObject, "_DisplayName", pDisplayName);
            SetString(lSerializedObject, "_Description", pDescription);
            SetEnum(lSerializedObject, "_Team", (int)pTeam);
            SetInt(lSerializedObject, "_MaxHealth", pMaxHealth);
            SetInt(lSerializedObject, "_MoveRange", pMoveRange);
            SetInt(lSerializedObject, "_ActionPointsPerTurn", pActionPoints);
            SetInt(lSerializedObject, "_Initiative", pInitiative);
            SetInt(lSerializedObject, "_MeleeDamagePercent", pMeleeDamage);
            SetInt(lSerializedObject, "_RangedDamagePercent", pRangedDamage);
            SetInt(lSerializedObject, "_MeleeResistancePercent", pMeleeResistance);
            SetInt(lSerializedObject, "_RangedResistancePercent", pRangedResistance);
            SetInt(lSerializedObject, "_PushDamageBonus", pPushDamageBonus);
            SetInt(lSerializedObject, "_FootprintWidth", 1);
            SetInt(lSerializedObject, "_FootprintHeight", 1);
            SetBool(lSerializedObject, "_ParticipatesInTurnOrder", pParticipatesInTurnOrder);
            SetBool(lSerializedObject, "_CountsForVictory", pCountsForVictory);
            SetObject(lSerializedObject, "_ModelPrefab", pModelPrefab);
            SetObject(lSerializedObject, "_PreviewSprite", pIcon);
            SetObject(lSerializedObject, "_Portrait", pIcon);
            SetColor(lSerializedObject, "_Tint", Color.white);
            SetObjectList(lSerializedObject, "_Skills", pSkills);
            SetObjectList(lSerializedObject, "_Passives", pPassives);
            SetObject(lSerializedObject, "_DefaultPassive", pDefaultPassive);
            Apply(lSerializedObject, lAsset);
            return lAsset;
        }

        private static GameObject CreateTreasureUiPrefab(IReadOnlyDictionary<string, Sprite> pIcons)
        {
            string lPath = UiFolder + "/PF_Forban_CoffreUI.prefab";
            GameObject lRoot = new GameObject("PF_Forban_CoffreUI", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(TreasureResourceView));
            RectTransform lRootTransform = lRoot.GetComponent<RectTransform>();
            lRootTransform.sizeDelta = new Vector2(240f, 32f);

            HorizontalLayoutGroup lLayout = lRoot.GetComponent<HorizontalLayoutGroup>();
            lLayout.spacing = 6f;
            lLayout.childAlignment = TextAnchor.MiddleLeft;
            lLayout.childControlWidth = false;
            lLayout.childControlHeight = false;

            GameObject lLabelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lLabelObject.transform.SetParent(lRoot.transform, false);
            TextMeshProUGUI lLabel = lLabelObject.GetComponent<TextMeshProUGUI>();
            lLabel.text = "Coffre 0/5";
            lLabel.fontSize = 18f;
            lLabel.color = Color.white;
            lLabel.alignment = TextAlignmentOptions.MidlineLeft;
            lLabel.rectTransform.sizeDelta = new Vector2(92f, 28f);

            Image[] lSlots = new Image[5];
            for (int lIndex = 0; lIndex < lSlots.Length; lIndex++)
            {
                GameObject lSlotObject = new GameObject($"Slot_{lIndex + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lSlotObject.transform.SetParent(lRoot.transform, false);
                RectTransform lSlotTransform = lSlotObject.GetComponent<RectTransform>();
                lSlotTransform.sizeDelta = new Vector2(20f, 20f);
                Image lImage = lSlotObject.GetComponent<Image>();
                lImage.sprite = pIcons["coffre"];
                lImage.color = new Color(0.18f, 0.18f, 0.18f, 0.75f);
                lSlots[lIndex] = lImage;
            }

            SerializedObject lSerializedObject = new SerializedObject(lRoot.GetComponent<TreasureResourceView>());
            SetObject(lSerializedObject, "_Label", lLabel);
            SerializedProperty lSlotsProperty = lSerializedObject.FindProperty("_Slots");
            lSlotsProperty.arraySize = lSlots.Length;
            for (int lIndex = 0; lIndex < lSlots.Length; lIndex++)
                lSlotsProperty.GetArrayElementAtIndex(lIndex).objectReferenceValue = lSlots[lIndex];
            lSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject lSaved = PrefabUtility.SaveAsPrefabAsset(lRoot, lPath);
            Object.DestroyImmediate(lRoot);
            return lSaved;
        }

        private static GameObject SaveModelPrefab(string pName, Material pPrimaryMaterial, Material pSecondaryMaterial, PrimitiveType pPrimitive, float pScale = 1f)
        {
            string lPath = $"{PrefabsFolder}/{pName}.prefab";
            GameObject lRoot = new GameObject(pName);

            GameObject lBody = GameObject.CreatePrimitive(pPrimitive);
            lBody.name = "Body";
            lBody.transform.SetParent(lRoot.transform, false);
            lBody.transform.localScale = new Vector3(0.7f * pScale, 1.1f * pScale, 0.7f * pScale);
            SetRendererMaterial(lBody, pPrimaryMaterial);
            RemoveCollider(lBody);

            GameObject lMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lMarker.name = "Marker";
            lMarker.transform.SetParent(lRoot.transform, false);
            lMarker.transform.localPosition = new Vector3(0f, 0.72f * pScale, 0f);
            lMarker.transform.localScale = new Vector3(0.55f * pScale, 0.16f * pScale, 0.55f * pScale);
            SetRendererMaterial(lMarker, pSecondaryMaterial);
            RemoveCollider(lMarker);

            GameObject lSaved = PrefabUtility.SaveAsPrefabAsset(lRoot, lPath);
            Object.DestroyImmediate(lRoot);
            return lSaved;
        }

        private static Material CreateMaterial(string pName, Color pColor)
        {
            string lPath = $"{MaterialsFolder}/{pName}.mat";
            Material lMaterial = AssetDatabase.LoadAssetAtPath<Material>(lPath);
            if (lMaterial == null)
            {
                Shader lShader = Shader.Find("Universal Render Pipeline/Lit");
                if (lShader == null)
                    lShader = Shader.Find("Standard");

                lMaterial = new Material(lShader);
                AssetDatabase.CreateAsset(lMaterial, lPath);
            }

            lMaterial.color = pColor;
            if (lMaterial.HasProperty("_BaseColor"))
                lMaterial.SetColor("_BaseColor", pColor);
            EditorUtility.SetDirty(lMaterial);
            return lMaterial;
        }

        private static Sprite CreateIconSprite(string pName, Color pColor)
        {
            string lPath = $"{IconsFolder}/Icon_{pName}.png";
            string lAbsolutePath = ToAbsoluteAssetPath(lPath);
            if (!File.Exists(lAbsolutePath))
            {
                Texture2D lTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                Color lInner = pColor;
                Color lBorder = new Color(Mathf.Clamp01(pColor.r * 0.45f), Mathf.Clamp01(pColor.g * 0.45f), Mathf.Clamp01(pColor.b * 0.45f), 1f);

                for (int lY = 0; lY < 64; lY++)
                {
                    for (int lX = 0; lX < 64; lX++)
                    {
                        bool lIsBorder = lX < 5 || lX > 58 || lY < 5 || lY > 58;
                        lTexture.SetPixel(lX, lY, lIsBorder ? lBorder : lInner);
                    }
                }

                lTexture.Apply();
                File.WriteAllBytes(lAbsolutePath, lTexture.EncodeToPNG());
                Object.DestroyImmediate(lTexture);
                AssetDatabase.ImportAsset(lPath);
            }

            TextureImporter lImporter = AssetImporter.GetAtPath(lPath) as TextureImporter;
            if (lImporter != null && lImporter.textureType != TextureImporterType.Sprite)
            {
                lImporter.textureType = TextureImporterType.Sprite;
                lImporter.spritePixelsPerUnit = 64f;
                lImporter.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(lPath);
        }

        private static T LoadOrCreate<T>(string pPath) where T : ScriptableObject
        {
            T lAsset = AssetDatabase.LoadAssetAtPath<T>(pPath);
            if (lAsset != null)
                return lAsset;

            EnsureFolder(Path.GetDirectoryName(pPath)?.Replace("\\", "/"));
            lAsset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(lAsset, pPath);
            return lAsset;
        }

        private static T Load<T>(string pPath) where T : Object => AssetDatabase.LoadAssetAtPath<T>(pPath);

        private static void EnsureFolder(string pFolderPath)
        {
            if (string.IsNullOrWhiteSpace(pFolderPath) || AssetDatabase.IsValidFolder(pFolderPath))
                return;

            string[] lParts = pFolderPath.Split('/');
            string lCurrentPath = lParts[0];
            for (int lIndex = 1; lIndex < lParts.Length; lIndex++)
            {
                string lNextPath = $"{lCurrentPath}/{lParts[lIndex]}";
                if (!AssetDatabase.IsValidFolder(lNextPath))
                    AssetDatabase.CreateFolder(lCurrentPath, lParts[lIndex]);
                lCurrentPath = lNextPath;
            }
        }

        private static string ToAbsoluteAssetPath(string pAssetPath)
        {
            string lRelativePath = pAssetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? pAssetPath.Substring("Assets/".Length)
                : pAssetPath;
            return Path.Combine(Application.dataPath, lRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private static void SetRendererMaterial(GameObject pObject, Material pMaterial)
        {
            Renderer lRenderer = pObject.GetComponent<Renderer>();
            if (lRenderer != null)
                lRenderer.sharedMaterial = pMaterial;
        }

        private static void RemoveCollider(GameObject pObject)
        {
            Collider lCollider = pObject.GetComponent<Collider>();
            if (lCollider != null)
                Object.DestroyImmediate(lCollider);
        }

        private static void Apply(SerializedObject pSerializedObject, Object pAsset)
        {
            pSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pAsset);
        }

        private static void SetString(SerializedObject pObject, string pName, string pValue) => pObject.FindProperty(pName).stringValue = pValue ?? string.Empty;
        private static void SetInt(SerializedObject pObject, string pName, int pValue) => pObject.FindProperty(pName).intValue = pValue;
        private static void SetBool(SerializedObject pObject, string pName, bool pValue) => pObject.FindProperty(pName).boolValue = pValue;
        private static void SetEnum(SerializedObject pObject, string pName, int pValue) => pObject.FindProperty(pName).enumValueIndex = pValue;
        private static void SetColor(SerializedObject pObject, string pName, Color pValue) => pObject.FindProperty(pName).colorValue = pValue;
        private static void SetObject(SerializedObject pObject, string pName, Object pValue) => pObject.FindProperty(pName).objectReferenceValue = pValue;

        private static void SetObjectList<T>(SerializedObject pObject, string pName, IReadOnlyList<T> pValues) where T : Object
        {
            SerializedProperty lProperty = pObject.FindProperty(pName);
            int lCount = pValues != null ? pValues.Count : 0;
            lProperty.arraySize = lCount;
            for (int lIndex = 0; lIndex < lCount; lIndex++)
                lProperty.GetArrayElementAtIndex(lIndex).objectReferenceValue = pValues[lIndex];
        }

        #endregion

        #region _____________________________| RUNTIME HELPERS

        private static SkillDefinition FindSkill(UnitDefinition pUnit, string pSkillId)
        {
            if (pUnit?.Skills == null)
                return null;

            for (int lIndex = 0; lIndex < pUnit.Skills.Count; lIndex++)
            {
                SkillDefinition lSkill = pUnit.Skills[lIndex];
                if (lSkill != null && lSkill.Id == pSkillId)
                    return lSkill;
            }

            return null;
        }

        private static UnitSpawnDefinition Spawn(UnitDefinition pUnit, int pX, int pY) => new UnitSpawnDefinition
        {
            Unit = pUnit,
            StartCoordinate = new SerializableGridCoord(pX, pY)
        };

        private static BattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = BattleScenarioDefinition.CreateRuntime("forban_runtime_test", "Forban Runtime Test", 8, 8, 1, null, pSpawns);
            GridService lGridService = new GridService();
            TurnSystem lTurnSystem = new TurnSystem();
            BattleService lBattleService = new BattleService(lGridService, new PathService(lGridService), lTurnSystem, new SimpleSkillExecutor());
            lBattleService.Initialize(lScenario);
            return lBattleService;
        }

        private static void Check(bool pCondition, string pMessage, ICollection<string> pFailures)
        {
            if (!pCondition)
                pFailures.Add(pMessage);
        }

        private static void ReportValidation(IReadOnlyCollection<string> pFailures, bool pThrowOnFailure)
        {
            if (pFailures.Count == 0)
            {
                Debug.Log("[Forban] Validation passed.");
                return;
            }

            string lMessage = "[Forban] Validation failed:\n- " + string.Join("\n- ", pFailures);
            if (pThrowOnFailure)
                throw new InvalidOperationException(lMessage);

            Debug.LogError(lMessage);
        }

        #endregion

        #region _____________________________/ TYPES

        private sealed class BuildContext
        {
            public Dictionary<string, Sprite> Icons;
            public Dictionary<string, Material> Materials;
            public Dictionary<string, StateDefinition> States;
            public Dictionary<string, PassiveDefinition> Passives;
            public Dictionary<string, GameObject> Prefabs;
            public Dictionary<string, UnitDefinition> Decoys;
            public GameObject UiPrefab;
            public List<SkillDefinition> NormalSkills;
            public UnitDefinition Forban;
        }

        #endregion
    }
}
