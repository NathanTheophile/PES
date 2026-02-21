#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PES.Presentation.Configuration;

namespace PES.Presentation.Editor
{
    public static class VerticalSliceTestProfileGenerator
    {
        private const string RootPath = "Assets/_Project/Presentation/Configuration/TestProfiles";

        [MenuItem("PES/Tools/Generate Vertical Slice Test Profiles")]
        public static void Generate()
        {
            EnsureFolder("Assets/_Project/Presentation/Configuration", "TestProfiles");
            EnsureFolder(RootPath, "Skills");
            EnsureFolder(RootPath, "Archetypes");
            EnsureFolder(RootPath, "Runtime");
            EnsureFolder(RootPath, "Profiles");

            var strike = CreateSkill("Skills/Skill_Strike.asset", 100, "Strike", 1, 2, 12, 90, 0, 0, 0, 0);
            var poison = CreateSkill("Skills/Skill_PoisonDart.asset", 101, "Poison Dart", 2, 4, 7, 85, 1, 1, 2, 3);
            var burst = CreateSkill("Skills/Skill_Burst.asset", 102, "Burst", 1, 3, 10, 80, 1, 1, 1, 0, splashRadius: 1, splashPct: 50);

            var bruiser = CreateArchetype("Archetypes/Archetype_Bruiser.asset", 52, 6, 3, new[] { strike, burst });
            var ranger = CreateArchetype("Archetypes/Archetype_Ranger.asset", 42, 7, 4, new[] { strike, poison });
            var caster = CreateArchetype("Archetypes/Archetype_Caster.asset", 36, 5, 7, new[] { poison, burst });

            var baseline = CreateRuntimeConfig("Runtime/Runtime_Baseline.asset", 3, 1, 12, 80);
            var fast = CreateRuntimeConfig("Runtime/Runtime_FastPaced.asset", 5, 1, 10, 85);

            CreateProfile("Profiles/Profile_Bruiser_vs_Ranger.asset", "Bruiser vs Ranger", baseline, bruiser, ranger);
            CreateProfile("Profiles/Profile_Ranger_vs_Caster.asset", "Ranger vs Caster", baseline, ranger, caster);
            CreateProfile("Profiles/Profile_Bruiser_vs_Caster_Fast.asset", "Bruiser vs Caster (Fast)", fast, bruiser, caster);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PES] Vertical slice test profiles generated/updated.");
        }

        private static SkillDefinitionAsset CreateSkill(string relativePath, int id, string displayName, int minRange, int maxRange, int damage, int hitChance, int resourceCost, int cooldown, int periodicDamage, int periodicDuration, int splashRadius = 0, int splashPct = 0)
        {
            var path = $"{RootPath}/{relativePath}";
            var asset = LoadOrCreate<SkillDefinitionAsset>(path);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_skillId").intValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_minRange").intValue = minRange;
            serialized.FindProperty("_maxRange").intValue = maxRange;
            serialized.FindProperty("_baseDamage").intValue = damage;
            serialized.FindProperty("_baseHitChance").intValue = hitChance;
            serialized.FindProperty("_resourceCost").intValue = resourceCost;
            serialized.FindProperty("_cooldownTurns").intValue = cooldown;
            serialized.FindProperty("_periodicDamage").intValue = periodicDamage;
            serialized.FindProperty("_periodicDurationTurns").intValue = periodicDuration;
            serialized.FindProperty("_splashRadiusXZ").intValue = splashRadius;
            serialized.FindProperty("_splashDamagePercent").intValue = splashPct;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static EntityArchetypeAsset CreateArchetype(string relativePath, int hp, int movePoints, int resource, SkillDefinitionAsset[] skills)
        {
            var path = $"{RootPath}/{relativePath}";
            var asset = LoadOrCreate<EntityArchetypeAsset>(path);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_startHitPoints").intValue = hp;
            serialized.FindProperty("_startMovementPoints").intValue = movePoints;
            serialized.FindProperty("_startSkillResource").intValue = resource;

            var skillsProperty = serialized.FindProperty("_skills");
            skillsProperty.arraySize = skills.Length;
            for (var i = 0; i < skills.Length; i++)
            {
                skillsProperty.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombatRuntimeConfigAsset CreateRuntimeConfig(string relativePath, int maxMoveCost, int maxVerticalStep, int baseDamage, int hitChance)
        {
            var path = $"{RootPath}/{relativePath}";
            var asset = LoadOrCreate<CombatRuntimeConfigAsset>(path);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_maxMovementCostPerAction").intValue = maxMoveCost;
            serialized.FindProperty("_maxVerticalStepPerTile").intValue = maxVerticalStep;
            serialized.FindProperty("_baseDamage").intValue = baseDamage;
            serialized.FindProperty("_baseHitChance").intValue = hitChance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static VerticalSliceTestProfileAsset CreateProfile(string relativePath, string profileName, CombatRuntimeConfigAsset runtimeConfig, EntityArchetypeAsset unitA, EntityArchetypeAsset unitB)
        {
            var path = $"{RootPath}/{relativePath}";
            var asset = LoadOrCreate<VerticalSliceTestProfileAsset>(path);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_profileName").stringValue = profileName;
            serialized.FindProperty("_runtimeConfig").objectReferenceValue = runtimeConfig;
            serialized.FindProperty("_unitAArchetype").objectReferenceValue = unitA;
            serialized.FindProperty("_unitBArchetype").objectReferenceValue = unitB;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
