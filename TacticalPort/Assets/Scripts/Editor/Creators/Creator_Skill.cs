#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;
using static TacticalPort.EditorTools.Creator_FileSaver;

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
        private bool _UseAoeDamageFalloff;
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
            DrawTitle("Create Skill Definition", SkillsFolder);

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            DrawSectionHeader("Rules");
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
                _UseAoeDamageFalloff = EditorGUILayout.Toggle("Use AoE Damage Falloff", _UseAoeDamageFalloff);
            }
            else
            {
                _AoeSize = 0;
                _UseAoeDamageFalloff = false;
            }

            _UsePerTurn = Mathf.Max(0, EditorGUILayout.IntField("Use / Turn", _UsePerTurn));
            _UsePerTarget = Mathf.Max(0, EditorGUILayout.IntField("Use / Target / Turn", _UsePerTarget));
            _CooldownTurns = Mathf.Max(0, EditorGUILayout.IntField("Cooldown", _CooldownTurns));
            _Power = Mathf.Max(0, EditorGUILayout.IntField("Power", _Power));
            _ActionPointCost = Mathf.Max(0, EditorGUILayout.IntField("AP Cost", _ActionPointCost));

            DrawAdvancedCombat();

            DrawSectionHeader("Presentation");
            _Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _Icon, typeof(Sprite), false);

            if (DrawCreateButton("Create Skill Definition"))
                CreateAsset();
        }

        private void DrawAdvancedCombat()
        {
            DrawSectionHeader("Advanced Combat");

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
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = CreateSerializedAsset(out SkillDefinition lAsset);
            string lId = SanitizeId(_Id, SanitizeId(_DisplayName, "skill"));

            SetString(lSerializedObject, "_Id", lId);
            SetString(lSerializedObject, "_DisplayName", string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim());
            SetString(lSerializedObject, "_Description", _Description);
            SetEnum(lSerializedObject, "_PrimaryEffectType", (int)_PrimaryEffectType);
            SetEnum(lSerializedObject, "_AdditionalEffectType", (int)_AdditionalEffectType);
            SetInt(lSerializedObject, "_RangeMin", Mathf.Max(0, _RangeMin));
            SetInt(lSerializedObject, "_RangeMax", Mathf.Max(_RangeMin, _RangeMax));
            SetEnum(lSerializedObject, "_TargetAlignment", (int)_TargetAlignment);
            SetBool(lSerializedObject, "_RequiresLineOfSight", _RequiresLineOfSight);
            SetBool(lSerializedObject, "_CanAffectCaster", _CanAffectCaster);
            SetEnum(lSerializedObject, "_AoeShape", (int)_AoeShape);
            SetInt(lSerializedObject, "_AoeSize", _AoeShape == SkillAoeShape.Single ? 0 : Mathf.Max(1, _AoeSize));
            SetInt(lSerializedObject, "_AoeDamageFalloffPercentPerCell", _AoeShape != SkillAoeShape.Single && _UseAoeDamageFalloff ? SkillDefinition.FixedAoeDamageFalloffPercentPerCell : 0);
            SetInt(lSerializedObject, "_UsePerTurn", Mathf.Max(0, _UsePerTurn));
            SetInt(lSerializedObject, "_UsePerTarget", Mathf.Max(0, _UsePerTarget));
            SetInt(lSerializedObject, "_CooldownTurns", Mathf.Max(0, _CooldownTurns));
            SetInt(lSerializedObject, "_Power", Mathf.Max(0, _Power));
            SetInt(lSerializedObject, "_ActionPointCost", Mathf.Max(0, _ActionPointCost));
            SetInt(lSerializedObject, "_PushDistance", Mathf.Max(0, _PushDistance));
            SetObject(lSerializedObject, "_SummonUnit", _SummonUnit);
            SetEnum(lSerializedObject, "_SummonTeamRule", (int)_SummonTeamRule);
            SetInt(lSerializedObject, "_GlyphDurationTurns", Mathf.Max(1, _GlyphDurationTurns));
            SetEnum(lSerializedObject, "_GlyphTargetRule", (int)_GlyphTargetRule);
            SetObject(lSerializedObject, "_AppliedState", _AppliedState);
            SetInt(lSerializedObject, "_AppliedStateStacks", Mathf.Max(1, _AppliedStateStacks));
            SetInt(lSerializedObject, "_AppliedStateDurationTurns", _AppliedStateDurationTurns);
            SetObject(lSerializedObject, "_Icon", _Icon);

            ApplyAndSave(lSerializedObject);
            Creator_FileSaver.CreateAsset(lAsset, SkillsFolder, BuildFileName("Skill", lId, _DisplayName, "skill"));
            Close();
        }

        #endregion
    }
}
