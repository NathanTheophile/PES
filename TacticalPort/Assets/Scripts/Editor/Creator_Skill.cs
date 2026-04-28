#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class Creator_Skill : EditorWindow
    {
        #region _____________________________/ VALUES

        private string _Id = string.Empty;
        private string _DisplayName = "New Skill";
        private string _Description = string.Empty;
        private SkillPrimaryEffectType _PrimaryEffectType = SkillPrimaryEffectType.Damage;
        private SkillAdditionalEffectType _AdditionalEffectType = SkillAdditionalEffectType.None;
        private int _RangeMin = 0;
        private int _RangeMax = 1;
        private SkillTargetAlignment _TargetAlignment = SkillTargetAlignment.Any;
        private bool _RequiresLineOfSight;
        private bool _CanAffectCaster = true;
        private SkillAoeShape _AoeShape = SkillAoeShape.Single;
        private int _AoeSize = 0;
        private int _AoeDamageFalloffPercentPerCell = 20;
        private int _UsePerTurn = 0;
        private int _UsePerTarget = 0;
        private int _CooldownTurns = 0;
        private int _Power = 1;
        private int _ActionPointCost = 1;
        private int _PushDistance = 1;
        private UnitDefinition _SummonUnit;
        private SkillSummonTeamRule _SummonTeamRule = SkillSummonTeamRule.Definition;
        private int _GlyphDurationTurns = 1;
        private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;
        private StateDefinition _AppliedState;
        private int _AppliedStateStacks = 1;
        private int _AppliedStateDurationTurns = -1;
        private bool _UseDirectionalModifiers;
        private int _FrontDamageModifier = 0;
        private int _SideDamageModifier = 0;
        private int _BackDamageModifier = 0;
        private Sprite _Icon;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Create/Skill Definition...")]
        public static void Open()
        {
            Creator_Skill lWindow = GetWindow<Creator_Skill>("Create Skill");
            lWindow.minSize = new Vector2(360f, 360f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            Creator_FileSaver.DrawTitle("Create Skill Definition", Creator_FileSaver.SkillsFolder);

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);
            _PrimaryEffectType = (SkillPrimaryEffectType)EditorGUILayout.EnumPopup("Effect Type", _PrimaryEffectType);
            _AdditionalEffectType = (SkillAdditionalEffectType)EditorGUILayout.EnumPopup("Additional Effect", _AdditionalEffectType);
            _RangeMin = Mathf.Max(0, EditorGUILayout.IntField("Range Min", _RangeMin));
            _RangeMax = Mathf.Max(_RangeMin, EditorGUILayout.IntField("Range Max", _RangeMax));
            _TargetAlignment = (SkillTargetAlignment)EditorGUILayout.EnumPopup("Target Alignment", _TargetAlignment);
            _RequiresLineOfSight = EditorGUILayout.Toggle("Requires Line Of Sight", _RequiresLineOfSight);
            _CanAffectCaster = EditorGUILayout.Toggle("Can Affect Caster", _CanAffectCaster);
            _AoeShape = (SkillAoeShape)EditorGUILayout.EnumPopup("AoE Shape", _AoeShape);
            if (_AoeShape != SkillAoeShape.Single)
            {
                _AoeSize = Mathf.Max(1, EditorGUILayout.IntField("AoE Size", _AoeSize));
                _AoeDamageFalloffPercentPerCell = Mathf.Clamp(
                    EditorGUILayout.IntField("AoE Damage Falloff % / Cell", _AoeDamageFalloffPercentPerCell),
                    0,
                    100);
            }
            else
            {
                _AoeSize = 0;
                _AoeDamageFalloffPercentPerCell = 0;
            }

            _UsePerTurn = Mathf.Max(0, EditorGUILayout.IntField("Use / Turn", _UsePerTurn));
            _UsePerTarget = Mathf.Max(0, EditorGUILayout.IntField("Use / Target / Turn", _UsePerTarget));
            _CooldownTurns = Mathf.Max(0, EditorGUILayout.IntField("Cooldown", _CooldownTurns));
            _Power = Mathf.Max(0, EditorGUILayout.IntField("Power", _Power));
            _ActionPointCost = Mathf.Max(0, EditorGUILayout.IntField("AP Cost", _ActionPointCost));

            DrawAdvancedCombat();

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Presentation", EditorStyles.boldLabel);
            _Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _Icon, typeof(Sprite), false);

            if (Creator_FileSaver.DrawCreateButton("Create Skill Definition"))
                CreateAsset();
        }

        private void DrawAdvancedCombat()
        {
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Advanced Combat", EditorStyles.boldLabel);

            if (_AdditionalEffectType == SkillAdditionalEffectType.Push)
                _PushDistance = Mathf.Max(1, EditorGUILayout.IntField("Push Distance", _PushDistance));
            else if (_AdditionalEffectType == SkillAdditionalEffectType.Teleport)
                EditorGUILayout.HelpBox("Teleport has no extra parameters for now.", MessageType.None);
            else if (_AdditionalEffectType == SkillAdditionalEffectType.SwitchPositions)
                EditorGUILayout.HelpBox("Switch Positions has no extra parameters for now.", MessageType.None);

            if (_AdditionalEffectType == SkillAdditionalEffectType.Summon)
            {
                _SummonUnit = (UnitDefinition)EditorGUILayout.ObjectField("Summon Unit", _SummonUnit, typeof(UnitDefinition), false);
                _SummonTeamRule = (SkillSummonTeamRule)EditorGUILayout.EnumPopup("Summon Team Rule", _SummonTeamRule);
            }

            if (_AdditionalEffectType == SkillAdditionalEffectType.CreateGlyph)
            {
                _GlyphDurationTurns = Mathf.Max(1, EditorGUILayout.IntField("Glyph Duration", _GlyphDurationTurns));
                _GlyphTargetRule = (SkillGlyphTargetRule)EditorGUILayout.EnumPopup("Glyph Target Rule", _GlyphTargetRule);
            }

            _AppliedState = (StateDefinition)EditorGUILayout.ObjectField("Applied State", _AppliedState, typeof(StateDefinition), false);
            if (_AppliedState != null)
            {
                _AppliedStateStacks = Mathf.Max(1, EditorGUILayout.IntField("State Stacks", _AppliedStateStacks));
                _AppliedStateDurationTurns = EditorGUILayout.IntField("State Duration Override", _AppliedStateDurationTurns);
            }
            else
            {
                _AppliedStateStacks = 1;
                _AppliedStateDurationTurns = -1;
            }

            if (_PrimaryEffectType == SkillPrimaryEffectType.Damage)
            {
                _UseDirectionalModifiers = EditorGUILayout.Toggle("Directional Modifiers", _UseDirectionalModifiers);
                if (_UseDirectionalModifiers)
                {
                    _FrontDamageModifier = EditorGUILayout.IntField("Front Modifier", _FrontDamageModifier);
                    _SideDamageModifier = EditorGUILayout.IntField("Side Modifier", _SideDamageModifier);
                    _BackDamageModifier = EditorGUILayout.IntField("Back Modifier", _BackDamageModifier);
                }
            }
            else
            {
                _UseDirectionalModifiers = false;
                _FrontDamageModifier = 0;
                _SideDamageModifier = 0;
                _BackDamageModifier = 0;
            }
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = Creator_FileSaver.CreateSerializedAsset(out SkillDefinition lAsset);

            string lId = Creator_FileSaver.SanitizeId(_Id, Creator_FileSaver.SanitizeId(_DisplayName, "skill"));

            lSerializedObject.FindProperty("_Id").stringValue = lId;
            lSerializedObject.FindProperty("_DisplayName").stringValue = string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim();
            lSerializedObject.FindProperty("_Description").stringValue = _Description ?? string.Empty;
            lSerializedObject.FindProperty("_LegacyEffectType").enumValueIndex = _PrimaryEffectType == SkillPrimaryEffectType.Heal ? (int)SkillEffectType.Heal : (int)SkillEffectType.Damage;
            lSerializedObject.FindProperty("_PrimaryEffectType").enumValueIndex = (int)_PrimaryEffectType;
            lSerializedObject.FindProperty("_AdditionalEffectType").enumValueIndex = (int)_AdditionalEffectType;
            lSerializedObject.FindProperty("_HasMigratedEffectSetup").boolValue = true;
            lSerializedObject.FindProperty("_Range").intValue = Mathf.Max(0, _RangeMax);
            lSerializedObject.FindProperty("_RangeMin").intValue = Mathf.Max(0, _RangeMin);
            lSerializedObject.FindProperty("_RangeMax").intValue = Mathf.Max(_RangeMin, _RangeMax);
            lSerializedObject.FindProperty("_Linear").boolValue = _TargetAlignment == SkillTargetAlignment.Orthogonal;
            lSerializedObject.FindProperty("_TargetAlignment").enumValueIndex = (int)_TargetAlignment;
            lSerializedObject.FindProperty("_RequiresLineOfSight").boolValue = _RequiresLineOfSight;
            lSerializedObject.FindProperty("_CanAffectCaster").boolValue = _CanAffectCaster;
            lSerializedObject.FindProperty("_AoeShape").enumValueIndex = (int)_AoeShape;
            lSerializedObject.FindProperty("_AoeSize").intValue = _AoeShape == SkillAoeShape.Single ? 0 : Mathf.Max(1, _AoeSize);
            lSerializedObject.FindProperty("_AoeDamageFalloffPercentPerCell").intValue = _AoeShape == SkillAoeShape.Single ? 0 : Mathf.Clamp(_AoeDamageFalloffPercentPerCell, 0, 100);
            lSerializedObject.FindProperty("_UsePerTurn").intValue = Mathf.Max(0, _UsePerTurn);
            lSerializedObject.FindProperty("_UsePerTarget").intValue = Mathf.Max(0, _UsePerTarget);
            lSerializedObject.FindProperty("_CooldownTurns").intValue = Mathf.Max(0, _CooldownTurns);
            lSerializedObject.FindProperty("_Power").intValue = Mathf.Max(0, _Power);
            lSerializedObject.FindProperty("_ActionPointCost").intValue = Mathf.Max(0, _ActionPointCost);
            lSerializedObject.FindProperty("_PushDistance").intValue = Mathf.Max(0, _PushDistance);
            lSerializedObject.FindProperty("_SummonUnit").objectReferenceValue = _SummonUnit;
            lSerializedObject.FindProperty("_SummonTeamRule").enumValueIndex = (int)_SummonTeamRule;
            lSerializedObject.FindProperty("_GlyphDurationTurns").intValue = Mathf.Max(1, _GlyphDurationTurns);
            lSerializedObject.FindProperty("_GlyphTargetRule").enumValueIndex = (int)_GlyphTargetRule;
            lSerializedObject.FindProperty("_AppliedState").objectReferenceValue = _AppliedState;
            lSerializedObject.FindProperty("_AppliedStateStacks").intValue = Mathf.Max(1, _AppliedStateStacks);
            lSerializedObject.FindProperty("_AppliedStateDurationTurns").intValue = _AppliedStateDurationTurns;
            lSerializedObject.FindProperty("_UseDirectionalModifiers").boolValue = _UseDirectionalModifiers;
            lSerializedObject.FindProperty("_FrontDamageModifier").intValue = _FrontDamageModifier;
            lSerializedObject.FindProperty("_SideDamageModifier").intValue = _SideDamageModifier;
            lSerializedObject.FindProperty("_BackDamageModifier").intValue = _BackDamageModifier;
            lSerializedObject.FindProperty("_Icon").objectReferenceValue = _Icon;

            Creator_FileSaver.ApplyAndSave(lSerializedObject);

            string lFileName = Creator_FileSaver.BuildFileName("Skill", lId, _DisplayName, "skill");
            Creator_FileSaver.CreateAsset(lAsset, Creator_FileSaver.SkillsFolder, lFileName);
            Close();
        }

        #endregion
    }
}
