using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class CreateSkillDefinitionWindow : EditorWindow
    {
        #region _____________________________| VALUES

        private string _Id = string.Empty;
        private string _DisplayName = "New Skill";
        private string _Description = string.Empty;
        private SkillTargetType _TargetType = SkillTargetType.Unit;
        private SkillEffectType _EffectType = SkillEffectType.Damage;
        private int _RangeMin = 0;
        private int _RangeMax = 1;
        private SkillTargetAlignment _TargetAlignment = SkillTargetAlignment.Any;
        private bool _RequiresLineOfSight;
        private bool _CanAffectCaster = true;
        private SkillAoeShape _AoeShape = SkillAoeShape.Single;
        private int _AoeSize = 0;
        private int _UsePerTurn = 0;
        private int _UsePerTarget = 0;
        private int _CooldownTurns = 0;
        private int _Power = 1;
        private int _ActionPointCost = 1;
        private int _PushDistance = 1;
        private BattleUnitDefinition _SummonUnit;
        private SkillSummonTeamRule _SummonTeamRule = SkillSummonTeamRule.Definition;
        private int _GlyphDurationTurns = 1;
        private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;
        private bool _UseDirectionalModifiers;
        private int _FrontDamageModifier = 0;
        private int _SideDamageModifier = 0;
        private int _BackDamageModifier = 0;
        private Sprite _Icon;

        #endregion

        #region _____________________________| MENU

        [MenuItem("TacticalPort/Create/Skill Definition...")]
        public static void Open()
        {
            CreateSkillDefinitionWindow lWindow = GetWindow<CreateSkillDefinitionWindow>("Create Skill");
            lWindow.minSize = new Vector2(360f, 360f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            TacticalAssetEditorUtility.DrawTitle("Create Skill Definition", TacticalAssetEditorUtility.SkillsFolder);

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);
            _TargetType = (SkillTargetType)EditorGUILayout.EnumPopup("Target Type", _TargetType);
            _EffectType = (SkillEffectType)EditorGUILayout.EnumPopup("Effect Type", _EffectType);
            _RangeMin = Mathf.Max(0, EditorGUILayout.IntField("Range Min", _RangeMin));
            _RangeMax = Mathf.Max(_RangeMin, EditorGUILayout.IntField("Range Max", _RangeMax));
            _TargetAlignment = (SkillTargetAlignment)EditorGUILayout.EnumPopup("Target Alignment", _TargetAlignment);
            _RequiresLineOfSight = EditorGUILayout.Toggle("Requires Line Of Sight", _RequiresLineOfSight);
            _CanAffectCaster = EditorGUILayout.Toggle("Can Affect Caster", _CanAffectCaster);
            _AoeShape = (SkillAoeShape)EditorGUILayout.EnumPopup("AoE Shape", _AoeShape);
            if (_AoeShape != SkillAoeShape.Single)
                _AoeSize = Mathf.Max(1, EditorGUILayout.IntField("AoE Size", _AoeSize));
            else
                _AoeSize = 0;

            _UsePerTurn = Mathf.Max(0, EditorGUILayout.IntField("Use / Turn", _UsePerTurn));
            _UsePerTarget = Mathf.Max(0, EditorGUILayout.IntField("Use / Target", _UsePerTarget));
            _CooldownTurns = Mathf.Max(0, EditorGUILayout.IntField("Cooldown", _CooldownTurns));
            _Power = Mathf.Max(0, EditorGUILayout.IntField("Power", _Power));
            _ActionPointCost = Mathf.Max(0, EditorGUILayout.IntField("AP Cost", _ActionPointCost));

            DrawAdvancedCombat();

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Presentation", EditorStyles.boldLabel);
            _Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _Icon, typeof(Sprite), false);

            if (TacticalAssetEditorUtility.DrawCreateButton("Create Skill Definition"))
                CreateAsset();
        }

        private void DrawAdvancedCombat()
        {
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Advanced Combat", EditorStyles.boldLabel);

            if (_EffectType == SkillEffectType.Push)
                _PushDistance = Mathf.Max(1, EditorGUILayout.IntField("Push Distance", _PushDistance));
            else if (_EffectType == SkillEffectType.Teleport)
                EditorGUILayout.HelpBox("Teleport has no extra parameters for now.", MessageType.None);
            else if (_EffectType == SkillEffectType.SwitchPositions)
                EditorGUILayout.HelpBox("Switch Positions has no extra parameters for now.", MessageType.None);

            if (_EffectType == SkillEffectType.Summon)
            {
                _SummonUnit = (BattleUnitDefinition)EditorGUILayout.ObjectField("Summon Unit", _SummonUnit, typeof(BattleUnitDefinition), false);
                _SummonTeamRule = (SkillSummonTeamRule)EditorGUILayout.EnumPopup("Summon Team Rule", _SummonTeamRule);
            }

            if (_EffectType == SkillEffectType.CreateGlyph)
            {
                _GlyphDurationTurns = Mathf.Max(1, EditorGUILayout.IntField("Glyph Duration", _GlyphDurationTurns));
                _GlyphTargetRule = (SkillGlyphTargetRule)EditorGUILayout.EnumPopup("Glyph Target Rule", _GlyphTargetRule);
            }

            if (_EffectType == SkillEffectType.Damage)
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
            SerializedObject lSerializedObject = TacticalAssetEditorUtility.CreateSerializedAsset(out SkillDefinition lAsset);

            string lId = TacticalAssetEditorUtility.SanitizeId(_Id, TacticalAssetEditorUtility.SanitizeId(_DisplayName, "skill"));

            lSerializedObject.FindProperty("_Id").stringValue = lId;
            lSerializedObject.FindProperty("_DisplayName").stringValue = string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim();
            lSerializedObject.FindProperty("_Description").stringValue = _Description ?? string.Empty;
            lSerializedObject.FindProperty("_TargetType").enumValueIndex = (int)_TargetType;
            lSerializedObject.FindProperty("_EffectType").enumValueIndex = (int)_EffectType;
            lSerializedObject.FindProperty("_Range").intValue = Mathf.Max(0, _RangeMax);
            lSerializedObject.FindProperty("_RangeMin").intValue = Mathf.Max(0, _RangeMin);
            lSerializedObject.FindProperty("_RangeMax").intValue = Mathf.Max(_RangeMin, _RangeMax);
            lSerializedObject.FindProperty("_Linear").boolValue = _TargetAlignment == SkillTargetAlignment.Orthogonal;
            lSerializedObject.FindProperty("_TargetAlignment").enumValueIndex = (int)_TargetAlignment;
            lSerializedObject.FindProperty("_RequiresLineOfSight").boolValue = _RequiresLineOfSight;
            lSerializedObject.FindProperty("_CanAffectCaster").boolValue = _CanAffectCaster;
            lSerializedObject.FindProperty("_AoeShape").enumValueIndex = (int)_AoeShape;
            lSerializedObject.FindProperty("_AoeSize").intValue = _AoeShape == SkillAoeShape.Single ? 0 : Mathf.Max(1, _AoeSize);
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
            lSerializedObject.FindProperty("_UseDirectionalModifiers").boolValue = _UseDirectionalModifiers;
            lSerializedObject.FindProperty("_FrontDamageModifier").intValue = _FrontDamageModifier;
            lSerializedObject.FindProperty("_SideDamageModifier").intValue = _SideDamageModifier;
            lSerializedObject.FindProperty("_BackDamageModifier").intValue = _BackDamageModifier;
            lSerializedObject.FindProperty("_Icon").objectReferenceValue = _Icon;

            TacticalAssetEditorUtility.ApplyAndSave(lSerializedObject);

            string lFileName = TacticalAssetEditorUtility.BuildFileName("Skill", lId, _DisplayName, "skill");
            TacticalAssetEditorUtility.CreateAsset(lAsset, TacticalAssetEditorUtility.SkillsFolder, lFileName);
            Close();
        }

        #endregion
    }
}
