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
    [CustomEditor(typeof(UnitDefinition))]
    public class CustomInspector_BattleUnit : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 25f;

        private static readonly string[] Tabs = { "Metadata", "Stats", "Skills", "States", "AI" };

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
                    DrawStatsTab();
                    break;

                case 2:
                    DrawSection("Skills", "_Skills");
                    break;

                case 3:
                    DrawSection("Base States", "_BaseStates");
                    DrawSection("Phase States", "_PhaseStates");
                    break;

                case 4:
                    DrawAiTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMetadataTab()
        {
            DrawSection("Naming", "_DisplayName", "_Id", "_Description");
            DrawSection("Presentation", "_CombatViewPrefab", "_ModelPrefab", "_PreviewSprite", "_Portrait", "_Tint");
        }

        private void DrawStatsTab()
        {
            DrawSection(
                "Core Stats",
                "_Team",
                "_MaxHealth",
                "_MoveRange",
                "_ActionPointsPerTurn",
                "_Initiative",
                "_PushDamageBonus");

            DrawSection("Footprint", "_FootprintWidth", "_FootprintHeight");
        }

        private void DrawAiTab()
        {
            DrawSection("Enemy AI", "_EnemyAiProfile");
            DrawSkillAiOverrides();
        }

        private void DrawSkillAiOverrides()
        {
            SerializedProperty lSkillsProperty = serializedObject.FindProperty("_Skills");
            SerializedProperty lOverridesProperty = serializedObject.FindProperty("_SkillAiOverrides");

            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField("Skill AI Overrides", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);

            if (lOverridesProperty == null)
            {
                EditorGUILayout.HelpBox("Per-skill AI overrides will appear here once UnitDefinition exposes _SkillAiOverrides. Skills remain the only source of the unit loadout.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Overrides are synced from the Skills tab, so the skill kit is not entered twice.", MessageType.Info);

            if (GUILayout.Button("Sync AI Overrides From Skills"))
                SkillAiOverrideEditorUtility.SyncFromSkills(lOverridesProperty, lSkillsProperty);

            SkillAiOverrideEditorUtility.DrawSkillBoundOverrides(
                lOverridesProperty,
                lSkillsProperty,
                "Add skills in the Skills tab before configuring AI overrides.");
        }

        private void DrawSection(string pHeader, params string[] pPropertyNames)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(pHeader, EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);

            for (int lIndex = 0; lIndex < pPropertyNames.Length; lIndex++)
            {
                SerializedProperty lProperty = serializedObject.FindProperty(pPropertyNames[lIndex]);
                if (lProperty == null)
                    continue;

                EditorGUILayout.PropertyField(lProperty, true);
                EditorGUILayout.Space(ItemSpacing);
            }
        }

        #endregion
    }
}
