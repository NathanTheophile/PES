#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    [CustomEditor(typeof(SkillDefinition))]
    public class CustomInspector_Skill : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 25f;

        private static readonly string[] Tabs = { "Metadata", "Casting", "Usage", "Effects" };

        private int _CurrentTabIndex;

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _CurrentTabIndex = GUILayout.Toolbar(_CurrentTabIndex, Tabs);

            switch (_CurrentTabIndex)
            {
                case 0:
                    DrawMetadataTab();
                    break;

                case 1:
                    DrawCastingTab();
                    break;

                case 2:
                    DrawUsageTab();
                    break;

                case 3:
                    DrawEffectsTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMetadataTab()
        {
            DrawSection("Naming", "_DisplayName", "_Id");
            DrawSection("Misc", "_Description", "_Icon");
        }

        private void DrawCastingTab()
        {
            DrawSection("Definition", "_PrimaryEffectType", "_AdditionalEffectType", "_CanAffectCaster");
            DrawSection("Range", "_RangeMin", "_RangeMax");
            DrawSection("Targeting", "_TargetAlignment", "_RequiresLineOfSight");
            DrawSection("Area", "_AoeShape", "_AoeSize");
            DrawAoeDamageFalloffToggle();
        }

        private void DrawUsageTab()
        {
            DrawSection("Rules", "_ActionPointCost", "_UsePerTurn");
            DrawProperty("_UsePerTarget", "Use Per Target Per Turn");
            DrawProperty("_CooldownTurns");
        }

        private void DrawEffectsTab()
        {
            DrawSection("Power", "_Power");
            DrawSection("Push & Grab", "_PushDistance");
            DrawSection("Summoning", "_SummonUnit", "_SummonTeamRule");
            DrawSection("Glyph", "_GlyphDurationTurns", "_GlyphTargetRule");
            DrawSection("States", "_AppliedState", "_AppliedStateStacks", "_AppliedStateDurationTurns");
        }

        private void DrawSection(string pHeader, params string[] pPropertyNames)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(pHeader, EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);

            for (int lIndex = 0; lIndex < pPropertyNames.Length; lIndex++)
                DrawProperty(pPropertyNames[lIndex]);
        }

        private void DrawProperty(string pPropertyName, string pLabel = null)
        {
            SerializedProperty lProperty = serializedObject.FindProperty(pPropertyName);
            if (lProperty == null)
                return;

            if (string.IsNullOrWhiteSpace(pLabel))
                EditorGUILayout.PropertyField(lProperty);
            else
                EditorGUILayout.PropertyField(lProperty, new GUIContent(pLabel));

            EditorGUILayout.Space(ItemSpacing);
        }

        private void DrawAoeDamageFalloffToggle()
        {
            SerializedProperty lShapeProperty = serializedObject.FindProperty("_AoeShape");
            SerializedProperty lFalloffProperty = serializedObject.FindProperty("_AoeDamageFalloffPercentPerCell");
            if (lShapeProperty == null || lFalloffProperty == null)
                return;

            bool lIsAreaSkill = (SkillAoeShape)lShapeProperty.enumValueIndex != SkillAoeShape.Single;
            using (new EditorGUI.DisabledScope(!lIsAreaSkill))
            {
                bool lUseFalloff = lIsAreaSkill && lFalloffProperty.intValue > 0;
                bool lNextValue = EditorGUILayout.Toggle("Use AoE Damage Falloff", lUseFalloff);
                lFalloffProperty.intValue = lIsAreaSkill && lNextValue ? SkillDefinition.FixedAoeDamageFalloffPercentPerCell : 0;
            }

            EditorGUILayout.Space(ItemSpacing);
        }

        #endregion
    }
}
