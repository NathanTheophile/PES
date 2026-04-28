#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using TacticalPort.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Editors
{
    [CustomEditor(typeof(UnitDefinition))]
    public class CustomInspector_BattleUnit : Editor
    {
        #region _____________________________/ EDITOR SETTINGS

        private const uint TITLE_SPACING = 8;
        private const uint ITEM_SPACING = 1;
        private const uint SECTION_SPACING = 25;

        private const string METADATA_HEADER = "Metadata";
        private const string STATS_HEADER = "Stats";
        private const string SKILLS_HEADER = "Skills";
        private const string STATES_HEADER = "States";
        private const string AI_HEADER = "AI";

        #endregion

        #region _____________________________/ OBJECT PROPERTIES

        #region _____________________________/ METADATA
        private const string UNIT_ID = "_Id";
        private const string UNIT_DISPLAY_NAME = "_DisplayName";
        private const string UNIT_DESCRIPTION = "_Description";
        private const string UNIT_VIEW_PREFAB = "_UnitViewPrefab";
        private const string UNIT_PORTRAIT = "_Portrait";
        private const string UNIT_TINT = "_Tint";
        #endregion

        #region _____________________________/ STATS
        private const string UNIT_TEAM = "_Team";
        private const string UNIT_MAX_HEALTH = "_MaxHealth";
        private const string UNIT_MOVE_RANGE = "_MoveRange";
        private const string UNIT_ACTION_POINTS_PER_TURN = "_ActionPointsPerTurn";
        private const string UNIT_INITIATIVE = "_Initiative";
        private const string UNIT_PUSH_DAMAGE_BONUS = "_PushDamageBonus";
        private const string UNIT_FOOTPRINT_WIDTH = "_FootprintWidth";
        private const string UNIT_FOOTPRINT_HEIGHT = "_FootprintHeight";
        #endregion

        #region _____________________________/ SKILLS
        private const string UNIT_SKILLS = "_Skills";
        #endregion

        #region _____________________________/ STATES
        private const string UNIT_BASE_STATES = "_BaseStates";
        private const string UNIT_PHASE_STATES = "_PhaseStates";
        #endregion

        #region _____________________________/ AI
        private const string UNIT_ENEMY_AI_PROFILE = "_EnemyAiProfile";
        private const string UNIT_SKILL_AI_OVERRIDES = "_SkillAiOverrides";
        #endregion

        #endregion

        #region _____________________________/ TAB MANAGEMENT

        private const string TAB_A_NAME = METADATA_HEADER;
        private const string TAB_B_NAME = STATS_HEADER;
        private const string TAB_C_NAME = SKILLS_HEADER;
        private const string TAB_D_NAME = STATES_HEADER;
        private const string TAB_E_NAME = AI_HEADER;

        private int currentTabIndex;
        private readonly string[] tabs = new string[] { TAB_A_NAME, TAB_B_NAME, TAB_C_NAME, TAB_D_NAME, TAB_E_NAME };

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabs);

            switch (currentTabIndex)
            {
                case 0:
                    DrawMetadataTab();
                    break;

                case 1:
                    DrawStatsTab();
                    break;

                case 2:
                    DrawSkillsTab();
                    break;

                case 3:
                    DrawStatesTab();
                    break;

                case 4:
                    DrawAiTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMetadataTab()
        {
            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| NAMING |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_DISPLAY_NAME));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_ID));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_DESCRIPTION));

            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| PRESENTATION |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_VIEW_PREFAB));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_PORTRAIT));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_TINT));
        }

        private void DrawStatsTab()
        {
            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| CORE STATS |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_TEAM));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_MAX_HEALTH));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_MOVE_RANGE));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_ACTION_POINTS_PER_TURN));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_INITIATIVE));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_PUSH_DAMAGE_BONUS));

            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| FOOTPRINT |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_FOOTPRINT_WIDTH));
            EditorGUILayout.Space(ITEM_SPACING);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_FOOTPRINT_HEIGHT));
        }

        private void DrawSkillsTab()
        {
            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| SKILLS |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_SKILLS), true);
        }

        private void DrawStatesTab()
        {
            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| BASE STATES |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_BASE_STATES), true);

            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| PHASE STATES |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_PHASE_STATES), true);
        }

        private void DrawAiTab()
        {
            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| ENEMY AI |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_ENEMY_AI_PROFILE), new GUIContent("AI Profile"));
            EditorGUILayout.Space(ITEM_SPACING);

            DrawSkillAiOverrides();
        }

        private void DrawSkillAiOverrides()
        {
            SerializedProperty lSkillsProperty = serializedObject.FindProperty(UNIT_SKILLS);
            SerializedProperty lOverridesProperty = serializedObject.FindProperty(UNIT_SKILL_AI_OVERRIDES);

            EditorGUILayout.Space(SECTION_SPACING);
            EditorGUILayout.LabelField("||| SKILL AI OVERRIDES |||", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TITLE_SPACING);

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

        #endregion
    }
}
