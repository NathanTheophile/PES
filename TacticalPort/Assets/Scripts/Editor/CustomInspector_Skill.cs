using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Editors
{
    [CustomEditor(typeof(SkillDefinition))]
    public class CustomInspector_Skill : Editor
    {
        #region Editor Settings

        private const uint TITLE_SPACING = 8;
        private const uint ITEM_SPACING = 1;
        private const uint SECTION_SPACING = 25;

        private const string METADATA_HEADER = "Metadata";
        private const string CASTING_HEADER = "Casting";
        private const string USAGE_HEADER = "Usage";
        private const string EFFECTS_HEADER = "Effects";

        #endregion

        #region Object Properties

        #region _____________________________/ METADATA
        private const string SKILL_ID = "_Id";
        private const string SKILL_DISPLAY_NAME = "_DisplayName";
        private const string SKILL_DESCRIPTION = "_Description";
        private const string SKILL_ICON = "_Icon";

        #endregion

        #region _____________________________/ CASTING
        private const string SKILL_PRIMARY_EFFECT_TYPE = "_PrimaryEffectType";
        private const string SKILL_ADDITIONAL_EFFECT_TYPE = "_AdditionalEffectType";

        private const string SKILL_RANGE_MIN = "_RangeMin";
        private const string SKILL_RANGE_MAX = "_RangeMax";

        private const string SKILL_TARGET_ALIGNMENT = "_TargetAlignment";
        private const string SKILL_REQUIRES_LINE_OF_SIGHT = "_RequiresLineOfSight";
        private const string SKILL_CAN_AFFECT_CASTER = "_CanAffectCaster";

        private const string SKILL_AOE_SHAPE = "_AoeShape";
        private const string SKILL_AOE_SIZE = "_AoeSize";

        #endregion

        #region _____________________________/ USAGE
        private const string SKILL_ACTION_POINT_COST = "_ActionPointCost";

        private const string SKILL_USE_PER_TURN = "_UsePerTurn";
        private const string SKILL_USE_PER_TARGET = "_UsePerTarget";
        private const string SKILL_COOLDOWN_TURNS = "_CooldownTurns";

        #endregion

        #region _____________________________/ EFFECTS
        private const string SKILL_POWER = "_Power";

        private const string SKILL_PUSH_DISTANCE = "_PushDistance";
        private const string SKILL_SUMMON_UNIT = "_SummonUnit";
        private const string SKILL_SUMMON_TEAM_RULE = "_SummonTeamRule";
        private const string SKILL_GLYPH_DURATION_TURNS = "_GlyphDurationTurns";
        private const string SKILL_GLYPH_TARGET_RULE = "_GlyphTargetRule";
        private const string SKILL_APPLIED_STATE = "_AppliedState";
        private const string SKILL_APPLIED_STATE_STACKS = "_AppliedStateStacks";
        private const string SKILL_APPLIED_STATE_DURATION_TURNS = "_AppliedStateDurationTurns";

        #endregion
        #endregion

        #region Tab Management

        private const string TAB_A_NAME = METADATA_HEADER;
        private const string TAB_B_NAME = CASTING_HEADER;
        private const string TAB_C_NAME = USAGE_HEADER;
        private const string TAB_D_NAME = EFFECTS_HEADER;

        private int currentTabIndex;
        private string[] tabs = new string[] { TAB_A_NAME, TAB_B_NAME, TAB_C_NAME, TAB_D_NAME };

        #endregion

        public override void OnInspectorGUI()
        {
            //To work with the last version of the serializedObject.
            serializedObject.Update();

            //Tab Management
            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabs);

            switch (currentTabIndex)
            {
                case 0:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| NAMING |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_DISPLAY_NAME));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_ID));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);
                    
                    EditorGUILayout.LabelField("◆◆◆| MISC |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);                    

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_DESCRIPTION));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_ICON));
                    break;

                case 1:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| DEFINITION |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_PRIMARY_EFFECT_TYPE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_ADDITIONAL_EFFECT_TYPE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_CAN_AFFECT_CASTER));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);   

                    EditorGUILayout.LabelField("◆◆◆| RANGE |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);                   

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_RANGE_MIN));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_RANGE_MAX));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| TARGETTING |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_TARGET_ALIGNMENT));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_REQUIRES_LINE_OF_SIGHT));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| ZONE |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);                    

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_AOE_SHAPE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_AOE_SIZE));
                    break;

                case 2:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| RULES |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_ACTION_POINT_COST));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_USE_PER_TURN));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_USE_PER_TARGET));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_COOLDOWN_TURNS));
                    break;

                case 3:
                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| POWER |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);         

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_POWER));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| PUSH & GRAB |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);
                    
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_PUSH_DISTANCE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| SUMMONING |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_SUMMON_UNIT));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_SUMMON_TEAM_RULE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("◆◆◆| GLYPH |◆◆◆", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_GLYPH_DURATION_TURNS));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_GLYPH_TARGET_RULE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.Space(SECTION_SPACING);

                    EditorGUILayout.LabelField("â—†â—†â—†| STATES |â—†â—†â—†", EditorStyles.toolbarButton);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_APPLIED_STATE));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_APPLIED_STATE_STACKS));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SKILL_APPLIED_STATE_DURATION_TURNS));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
