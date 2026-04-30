#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using UnityEditor;
using UnityEngine;
using TacticalPort.Shared;

namespace TacticalPort.EditorTools
{
    internal static class SkillAiOverrideEditorUtility
    {
        private const string SkillPropertyName = "_Skill";
        private const string PriorityPropertyName = "_Priority";
        private const string TargetTeamPropertyName = "_TargetTeam";
        private const string TargetModePropertyName = "_TargetMode";
        private const string AllowSelfCastPropertyName = "_AllowSelfCast";
        private const string PreferSelfCastPropertyName = "_PreferSelfCastWhenValid";
        private const string MaxCasterHealthPercentPropertyName = "_MaxCasterHealthPercent";
        private const string MinTargetMissingHealthPropertyName = "_MinTargetMissingHealth";
        private const string KiteAfterUsePropertyName = "_KiteAfterUse";

        public static void SyncFromSkills(SerializedProperty pOverridesProperty, IReadOnlyList<SkillDefinition> pSkills)
        {
            if (pOverridesProperty == null)
                return;

            for (int lIndex = pOverridesProperty.arraySize - 1; lIndex >= 0; lIndex--)
            {
                SerializedProperty lSkillProperty = FindSkillProperty(pOverridesProperty.GetArrayElementAtIndex(lIndex));
                if (lSkillProperty == null || !ContainsSkill(pSkills, lSkillProperty.objectReferenceValue as SkillDefinition))
                    pOverridesProperty.DeleteArrayElementAtIndex(lIndex);
            }

            if (pSkills == null)
                return;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                if (lSkill == null || FindOverrideIndex(pOverridesProperty, lSkill) >= 0)
                    continue;

                AddSkillOverride(pOverridesProperty, lSkill);
            }
        }

        public static void SyncFromSkills(SerializedProperty pOverridesProperty, SerializedProperty pSkillsProperty)
        {
            if (pOverridesProperty == null)
                return;

            for (int lIndex = pOverridesProperty.arraySize - 1; lIndex >= 0; lIndex--)
            {
                SerializedProperty lSkillProperty = FindSkillProperty(pOverridesProperty.GetArrayElementAtIndex(lIndex));
                if (lSkillProperty == null || !ContainsSkill(pSkillsProperty, lSkillProperty.objectReferenceValue))
                    pOverridesProperty.DeleteArrayElementAtIndex(lIndex);
            }

            if (pSkillsProperty == null)
                return;

            for (int lIndex = 0; lIndex < pSkillsProperty.arraySize; lIndex++)
            {
                Object lSkill = pSkillsProperty.GetArrayElementAtIndex(lIndex).objectReferenceValue;
                if (lSkill == null || FindOverrideIndex(pOverridesProperty, lSkill) >= 0)
                    continue;

                AddSkillOverride(pOverridesProperty, lSkill);
            }
        }

        public static void DrawSkillBoundOverrides(SerializedProperty pOverridesProperty, IReadOnlyList<SkillDefinition> pSkills, string pEmptyMessage)
        {
            bool lHasSkill = false;
            int lSkillCount = pSkills != null ? pSkills.Count : 0;
            for (int lIndex = 0; lIndex < lSkillCount; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                if (lSkill == null)
                    continue;

                lHasSkill = true;
                DrawSkillOverride(pOverridesProperty, lSkill);
            }

            if (!lHasSkill)
                EditorGUILayout.HelpBox(pEmptyMessage, MessageType.None);
        }

        public static void DrawSkillBoundOverrides(SerializedProperty pOverridesProperty, SerializedProperty pSkillsProperty, string pEmptyMessage)
        {
            bool lHasSkill = false;
            int lSkillCount = pSkillsProperty != null ? pSkillsProperty.arraySize : 0;
            for (int lIndex = 0; lIndex < lSkillCount; lIndex++)
            {
                SkillDefinition lSkill = pSkillsProperty.GetArrayElementAtIndex(lIndex).objectReferenceValue as SkillDefinition;
                if (lSkill == null)
                    continue;

                lHasSkill = true;
                DrawSkillOverride(pOverridesProperty, lSkill);
            }

            if (!lHasSkill)
                EditorGUILayout.HelpBox(pEmptyMessage, MessageType.None);
        }

        private static void AddSkillOverride(SerializedProperty pOverridesProperty, Object pSkill)
        {
            int lNewIndex = pOverridesProperty.arraySize;
            pOverridesProperty.InsertArrayElementAtIndex(lNewIndex);
            SerializedProperty lSkillProperty = FindSkillProperty(pOverridesProperty.GetArrayElementAtIndex(lNewIndex));
            if (lSkillProperty != null)
                lSkillProperty.objectReferenceValue = pSkill;
        }

        private static void DrawSkillOverride(SerializedProperty pOverridesProperty, SkillDefinition pSkill)
        {
            int lOverrideIndex = FindOverrideIndex(pOverridesProperty, pSkill);
            if (lOverrideIndex < 0)
            {
                EditorGUILayout.HelpBox($"{pSkill.DisplayName}: no override entry yet. Use Sync AI Overrides From Skills.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Skill", pSkill, typeof(SkillDefinition), false);

            DrawOverrideFieldsWithoutSkill(pOverridesProperty.GetArrayElementAtIndex(lOverrideIndex));
            EditorGUILayout.EndVertical();
        }

        private static void DrawOverrideFieldsWithoutSkill(SerializedProperty pEntryProperty)
        {
            bool lPreviousEnabled = GUI.enabled;
            GUI.enabled = true;

            DrawRelativeProperty(pEntryProperty, PriorityPropertyName, "Priority");
            DrawRelativeProperty(pEntryProperty, TargetTeamPropertyName, "Target Team");
            DrawRelativeProperty(pEntryProperty, TargetModePropertyName, "Target Mode");
            DrawRelativeProperty(pEntryProperty, AllowSelfCastPropertyName, "Allow Self Cast");
            DrawRelativeProperty(pEntryProperty, PreferSelfCastPropertyName, "Prefer Self Cast When Valid");
            DrawRelativeProperty(pEntryProperty, MaxCasterHealthPercentPropertyName, "Max Caster Health Percent");
            DrawRelativeProperty(pEntryProperty, MinTargetMissingHealthPropertyName, "Min Target Missing Health");
            DrawRelativeProperty(pEntryProperty, KiteAfterUsePropertyName, "Kite After Use");

            GUI.enabled = lPreviousEnabled;
        }

        private static void DrawRelativeProperty(SerializedProperty pEntryProperty, string pPropertyName, string pLabel)
        {
            SerializedProperty lProperty = pEntryProperty.FindPropertyRelative(pPropertyName);
            if (lProperty != null)
                EditorGUILayout.PropertyField(lProperty, new GUIContent(pLabel), true);
        }

        private static int FindOverrideIndex(SerializedProperty pOverridesProperty, Object pSkill)
        {
            if (pOverridesProperty == null)
                return -1;

            for (int lIndex = 0; lIndex < pOverridesProperty.arraySize; lIndex++)
            {
                SerializedProperty lSkillProperty = FindSkillProperty(pOverridesProperty.GetArrayElementAtIndex(lIndex));
                if (lSkillProperty != null && lSkillProperty.objectReferenceValue == pSkill)
                    return lIndex;
            }

            return -1;
        }

        private static SerializedProperty FindSkillProperty(SerializedProperty pEntryProperty)
        {
            return pEntryProperty.FindPropertyRelative(SkillPropertyName);
        }

        private static bool ContainsSkill(IReadOnlyList<SkillDefinition> pSkills, SkillDefinition pSkill)
        {
            if (pSkills == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                if (pSkills[lIndex] == pSkill)
                    return true;
            }

            return false;
        }

        private static bool ContainsSkill(SerializedProperty pSkillsProperty, Object pSkill)
        {
            if (pSkillsProperty == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < pSkillsProperty.arraySize; lIndex++)
            {
                if (pSkillsProperty.GetArrayElementAtIndex(lIndex).objectReferenceValue == pSkill)
                    return true;
            }

            return false;
        }
    }
}
