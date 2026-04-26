using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Editors
{
    [CustomEditor(typeof(UnitDefinition))]
    public class CustomInspector_BattleUnit : Editor
    {
        #region Editor Settings

        private const uint TITLE_SPACING = 8;
        private const uint ITEM_SPACING = 1;
        private const uint SECTION_SPACING = 25;

        private const string METADATA_HEADER = "Metadata";
        private const string STATS_HEADER = "Stats";
        private const string SKILLS_AND_STATES_HEADER = "Skills & States";
        private const string RUNTIME_HEADER = "Runtime";

        #endregion

        #region Object Properties

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

        #region _____________________________/ SKILLS & STATES
        private const string UNIT_SKILLS = "_Skills";
        private const string UNIT_BASE_STATES = "_BaseStates";
        #endregion

        #region _____________________________/ RUNTIME
        private const string UNIT_PHASE_STATES = "_PhaseStates";
        #endregion

        #endregion

        #region Tab Management

        private const string TAB_A_NAME = METADATA_HEADER;
        private const string TAB_B_NAME = STATS_HEADER;
        private const string TAB_C_NAME = SKILLS_AND_STATES_HEADER;
        private const string TAB_D_NAME = RUNTIME_HEADER;

        private int currentTabIndex;
        private string[] tabs = new string[] { TAB_A_NAME, TAB_B_NAME, TAB_C_NAME, TAB_D_NAME };

        #endregion

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabs);

            switch (currentTabIndex)
            {
                case 0:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| NAMING |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_DISPLAY_NAME));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_ID));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_DESCRIPTION));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| PRESENTATION |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_VIEW_PREFAB));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_PORTRAIT));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_TINT));
                    break;

                case 1:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| CORE STATS |◆◆◆", EditorStyles.toolbarButton);
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
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| FOOTPRINT |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_FOOTPRINT_WIDTH));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_FOOTPRINT_HEIGHT));
                    break;

                case 2:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| SKILLS |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_SKILLS), true);
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| BASE STATES |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_BASE_STATES), true);
                    break;

                case 3:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| PHASE STATES |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(UNIT_PHASE_STATES), true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}