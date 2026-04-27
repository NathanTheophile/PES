using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Editors
{
    [CustomEditor(typeof(StateDefinition))]
    public class CustomInspector_StateDefinition : Editor
    {
        #region Editor Settings

        private const uint TITLE_SPACING = 8;
        private const uint ITEM_SPACING = 1;
        private const uint SECTION_SPACING = 25;

        private const string METADATA_HEADER = "Metadata";
        private const string RULES_HEADER = "Rules";
        private const string MODIFIERS_HEADER = "Modifiers";

        #endregion

        #region Object Properties

        #region _____________________________/ METADATA
        private const string STATE_ID = "_Id";
        private const string STATE_DISPLAY_NAME = "_DisplayName";
        private const string STATE_DESCRIPTION = "_Description";
        private const string STATE_ICON = "_Icon";

        #endregion

        #region _____________________________/ RULES
        private const string STATE_DURATION_TURNS = "_DurationTurns";
        private const string STATE_MAX_STACKS = "_MaxStacks";
        private const string STATE_IS_PASSIVE_MARKER = "_IsPassiveMarker";
        #endregion

        #region _____________________________/ MODIFIERS
        private const string STATE_DAMAGE_MODIFIER = "_DamageModifierPerStack";
        private const string STATE_DAMAGE_REDUCTION = "_DamageReductionPerStack";
        private const string STATE_RANGE_MODIFIER = "_RangeModifierPerStack";
        private const string STATE_ACTION_POINT_MODIFIER = "_ActionPointModifierPerStack";
        private const string STATE_MOVEMENT_MODIFIER = "_MovementModifierPerStack";
        #endregion

        #endregion

        #region Tab Management

        private const string TAB_A_NAME = METADATA_HEADER;
        private const string TAB_B_NAME = RULES_HEADER;
        private const string TAB_C_NAME = MODIFIERS_HEADER;

        private int currentTabIndex;
        private string[] tabs = new string[] { TAB_A_NAME, TAB_B_NAME, TAB_C_NAME };

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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_DISPLAY_NAME));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_ID));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| MISC |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);                    

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_DESCRIPTION));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_ICON));
                    break;

                case 1:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| RULES |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_DURATION_TURNS));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_MAX_STACKS));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_IS_PASSIVE_MARKER));
                    break;

                case 2:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| MODIFIERS |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_DAMAGE_MODIFIER));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_DAMAGE_REDUCTION));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_RANGE_MODIFIER));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_ACTION_POINT_MODIFIER));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(STATE_MOVEMENT_MODIFIER));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
