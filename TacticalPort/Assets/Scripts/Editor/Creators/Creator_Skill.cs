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
        private SkillCategory _Category = SkillCategory.None;
        private SkillPrimaryEffectType _PrimaryEffectType = SkillPrimaryEffectType.Damage;
        private SkillAdditionalEffectType _AdditionalEffectType = SkillAdditionalEffectType.None;
        private int _RangeMin = 0;
        private int _RangeMax = 1;
        private SkillTargetAlignment _TargetAlignment = SkillTargetAlignment.Any;
        private bool _RequiresLineOfSight;
        private bool _CanAffectCaster = true;
        private SkillTargetType _TargetType = SkillTargetType.Cell;
        private SkillTargetRelation _TargetRelation = SkillTargetRelation.Anyone;
        private SkillTargetScope _TargetScope = SkillTargetScope.Area;
        private SkillAoeShape _AoeShape = SkillAoeShape.Single;
        private int _AoeSize = 0;
        private bool _UseAoeDamageFalloff;
        private int _UsePerTurn = 0;
        private int _UsePerTarget = 0;
        private int _CooldownTurns = 0;
        private SkillDefinition _Variant;
        private int _Power = 1;
        private bool _HasLifeSteal;
        private int _EnergyCost = 1;
        private int _PushDistance = 1;
        private int _PassiveProgressionSteps = 1;
        private UnitDefinition _SummonUnit;
        private SkillSummonTeamRule _SummonTeamRule = SkillSummonTeamRule.Definition;
        private int _GlyphDurationTurns = 1;
        private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;
        private StateDefinition _GlyphAppliedState;
        private int _GlyphAppliedStateStacks = 1;
        private int _GlyphAppliedStateDurationTurns = -1;
        private StateDefinition _SummonSpawnState;
        private int _SummonSpawnStateStacks = 1;
        private int _SummonSpawnStateDurationTurns = -1;
        private int _SummonSpawnStateAreaSize = 1;
        private SkillGlyphTargetRule _SummonSpawnStateTargetRule = SkillGlyphTargetRule.EnemiesOnly;
        private StateDefinition _AppliedState;
        private int _AppliedStateStacks = 1;
        private int _AppliedStateDurationTurns = -1;
        private StateDefinition _CasterAppliedState;
        private int _CasterAppliedStateStacks = 1;
        private int _CasterAppliedStateDurationTurns = -1;
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
            _Category = (SkillCategory)EditorGUILayout.EnumPopup("Category", _Category);
            _PrimaryEffectType = (SkillPrimaryEffectType)EditorGUILayout.EnumPopup("Effect Type", _PrimaryEffectType);
            _AdditionalEffectType = (SkillAdditionalEffectType)EditorGUILayout.EnumPopup("Additional Effect", _AdditionalEffectType);
            _RangeMin = Mathf.Max(0, EditorGUILayout.IntField("Range Min", _RangeMin));
            _RangeMax = Mathf.Max(_RangeMin, EditorGUILayout.IntField("Range Max", _RangeMax));
            _TargetAlignment = (SkillTargetAlignment)EditorGUILayout.EnumPopup("Target Alignment", _TargetAlignment);
            _RequiresLineOfSight = EditorGUILayout.Toggle("Requires Line Of Sight", _RequiresLineOfSight);
            _CanAffectCaster = EditorGUILayout.Toggle("Can Affect Caster", _CanAffectCaster);
            _TargetType = (SkillTargetType)EditorGUILayout.EnumPopup("Target Type", _TargetType);
            _TargetRelation = (SkillTargetRelation)EditorGUILayout.EnumPopup("Target Relation", _TargetRelation);
            _TargetScope = (SkillTargetScope)EditorGUILayout.EnumPopup("Target Scope", _TargetScope);
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
            _Variant = (SkillDefinition)EditorGUILayout.ObjectField("Variant", _Variant, typeof(SkillDefinition), false);
            _Power = Mathf.Max(0, EditorGUILayout.IntField("Power", _Power));
            if (_PrimaryEffectType == SkillPrimaryEffectType.Damage)
                _HasLifeSteal = EditorGUILayout.Toggle("Life Steal (50%)", _HasLifeSteal);
            else
                _HasLifeSteal = false;
            _EnergyCost = Mathf.Max(0, EditorGUILayout.IntField("Energy Cost", _EnergyCost));

            DrawAdvancedCombat();

            DrawSectionHeader("Presentation");
            _Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _Icon, typeof(Sprite), false);

            if (DrawCreateButton("Create Skill Definition"))
                CreateAsset();
        }

        private void DrawAdvancedCombat()
        {
            DrawSectionHeader("Advanced Combat");

            if (_AdditionalEffectType is SkillAdditionalEffectType.Push or SkillAdditionalEffectType.Pull)
                _PushDistance = Mathf.Max(1, EditorGUILayout.IntField("Forced Movement Distance", _PushDistance));
            else if (_AdditionalEffectType == SkillAdditionalEffectType.AdvanceActivePassiveProgression)
                _PassiveProgressionSteps = Mathf.Max(1, EditorGUILayout.IntField("Progression Steps", _PassiveProgressionSteps));
            else if (_AdditionalEffectType == SkillAdditionalEffectType.Teleport)
                EditorGUILayout.HelpBox("Teleport has no extra parameters for now.", MessageType.None);
            else if (_AdditionalEffectType == SkillAdditionalEffectType.SwitchPositions)
                EditorGUILayout.HelpBox("Switch Positions has no extra parameters for now.", MessageType.None);

            if (_AdditionalEffectType == SkillAdditionalEffectType.Summon)
            {
                _SummonUnit = (UnitDefinition)EditorGUILayout.ObjectField("Summon Unit", _SummonUnit, typeof(UnitDefinition), false);
                _SummonTeamRule = (SkillSummonTeamRule)EditorGUILayout.EnumPopup("Summon Team Rule", _SummonTeamRule);
                _SummonSpawnState = (StateDefinition)EditorGUILayout.ObjectField("Spawn State", _SummonSpawnState, typeof(StateDefinition), false);
                if (_SummonSpawnState != null)
                {
                    _SummonSpawnStateStacks = Mathf.Max(1, EditorGUILayout.IntField("Spawn State Stacks", _SummonSpawnStateStacks));
                    _SummonSpawnStateDurationTurns = EditorGUILayout.IntField("Spawn State Duration Override", _SummonSpawnStateDurationTurns);
                    _SummonSpawnStateAreaSize = Mathf.Max(0, EditorGUILayout.IntField("Spawn State Area", _SummonSpawnStateAreaSize));
                    _SummonSpawnStateTargetRule = (SkillGlyphTargetRule)EditorGUILayout.EnumPopup("Spawn State Target Rule", _SummonSpawnStateTargetRule);
                }
            }

            if (_AdditionalEffectType == SkillAdditionalEffectType.CreateGlyph)
            {
                _GlyphDurationTurns = Mathf.Max(1, EditorGUILayout.IntField("Glyph Duration", _GlyphDurationTurns));
                _GlyphTargetRule = (SkillGlyphTargetRule)EditorGUILayout.EnumPopup("Glyph Target Rule", _GlyphTargetRule);
                _GlyphAppliedState = (StateDefinition)EditorGUILayout.ObjectField("Glyph Applied State", _GlyphAppliedState, typeof(StateDefinition), false);
                if (_GlyphAppliedState != null)
                {
                    _GlyphAppliedStateStacks = Mathf.Max(1, EditorGUILayout.IntField("Glyph State Stacks", _GlyphAppliedStateStacks));
                    _GlyphAppliedStateDurationTurns = EditorGUILayout.IntField("Glyph State Duration Override", _GlyphAppliedStateDurationTurns);
                }
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

            _CasterAppliedState = (StateDefinition)EditorGUILayout.ObjectField("Caster Applied State", _CasterAppliedState, typeof(StateDefinition), false);
            if (_CasterAppliedState != null)
            {
                _CasterAppliedStateStacks = Mathf.Max(1, EditorGUILayout.IntField("Caster State Stacks", _CasterAppliedStateStacks));
                _CasterAppliedStateDurationTurns = EditorGUILayout.IntField("Caster State Duration Override", _CasterAppliedStateDurationTurns);
            }
            else
            {
                _CasterAppliedStateStacks = 1;
                _CasterAppliedStateDurationTurns = -1;
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
            SetEnum(lSerializedObject, "_Category", (int)_Category);
            SetEnum(lSerializedObject, "_PrimaryEffectType", (int)_PrimaryEffectType);
            SetEnum(lSerializedObject, "_AdditionalEffectType", (int)_AdditionalEffectType);
            SetInt(lSerializedObject, "_RangeMin", Mathf.Max(0, _RangeMin));
            SetInt(lSerializedObject, "_RangeMax", Mathf.Max(_RangeMin, _RangeMax));
            SetEnum(lSerializedObject, "_TargetAlignment", (int)_TargetAlignment);
            SetBool(lSerializedObject, "_RequiresLineOfSight", _RequiresLineOfSight);
            SetBool(lSerializedObject, "_CanAffectCaster", _CanAffectCaster);
            SetEnum(lSerializedObject, "_TargetType", (int)_TargetType);
            SetEnum(lSerializedObject, "_TargetRelation", (int)_TargetRelation);
            SetEnum(lSerializedObject, "_TargetScope", (int)_TargetScope);
            SetEnum(lSerializedObject, "_AoeShape", (int)_AoeShape);
            SetInt(lSerializedObject, "_AoeSize", _AoeShape == SkillAoeShape.Single ? 0 : Mathf.Max(1, _AoeSize));
            SetInt(lSerializedObject, "_AoeDamageFalloffPercentPerCell", _AoeShape != SkillAoeShape.Single && _UseAoeDamageFalloff ? SkillDefinition.FixedAoeDamageFalloffPercentPerCell : 0);
            SetInt(lSerializedObject, "_UsePerTurn", Mathf.Max(0, _UsePerTurn));
            SetInt(lSerializedObject, "_UsePerTarget", Mathf.Max(0, _UsePerTarget));
            SetInt(lSerializedObject, "_CooldownTurns", Mathf.Max(0, _CooldownTurns));
            SetObject(lSerializedObject, "_Variant", _Variant);
            SetInt(lSerializedObject, "_Power", Mathf.Max(0, _Power));
            SetBool(lSerializedObject, "_HasLifeSteal", _PrimaryEffectType == SkillPrimaryEffectType.Damage && _HasLifeSteal);
            SetInt(lSerializedObject, "_EnergyCost", Mathf.Max(0, _EnergyCost));
            SetInt(lSerializedObject, "_PushDistance", Mathf.Max(0, _PushDistance));
            SetInt(lSerializedObject, "_PassiveProgressionSteps", Mathf.Max(1, _PassiveProgressionSteps));
            SetObject(lSerializedObject, "_SummonUnit", _SummonUnit);
            SetEnum(lSerializedObject, "_SummonTeamRule", (int)_SummonTeamRule);
            SetInt(lSerializedObject, "_GlyphDurationTurns", Mathf.Max(1, _GlyphDurationTurns));
            SetEnum(lSerializedObject, "_GlyphTargetRule", (int)_GlyphTargetRule);
            SetObject(lSerializedObject, "_GlyphAppliedState", _GlyphAppliedState);
            SetInt(lSerializedObject, "_GlyphAppliedStateStacks", Mathf.Max(1, _GlyphAppliedStateStacks));
            SetInt(lSerializedObject, "_GlyphAppliedStateDurationTurns", _GlyphAppliedStateDurationTurns);
            SetObject(lSerializedObject, "_SummonSpawnState", _SummonSpawnState);
            SetInt(lSerializedObject, "_SummonSpawnStateStacks", Mathf.Max(1, _SummonSpawnStateStacks));
            SetInt(lSerializedObject, "_SummonSpawnStateDurationTurns", _SummonSpawnStateDurationTurns);
            SetInt(lSerializedObject, "_SummonSpawnStateAreaSize", Mathf.Max(0, _SummonSpawnStateAreaSize));
            SetEnum(lSerializedObject, "_SummonSpawnStateTargetRule", (int)_SummonSpawnStateTargetRule);
            SetObject(lSerializedObject, "_AppliedState", _AppliedState);
            SetInt(lSerializedObject, "_AppliedStateStacks", Mathf.Max(1, _AppliedStateStacks));
            SetInt(lSerializedObject, "_AppliedStateDurationTurns", _AppliedStateDurationTurns);
            SetObject(lSerializedObject, "_CasterAppliedState", _CasterAppliedState);
            SetInt(lSerializedObject, "_CasterAppliedStateStacks", Mathf.Max(1, _CasterAppliedStateStacks));
            SetInt(lSerializedObject, "_CasterAppliedStateDurationTurns", _CasterAppliedStateDurationTurns);
            SetObject(lSerializedObject, "_Icon", _Icon);

            ApplyAndSave(lSerializedObject);
            Creator_FileSaver.CreateAsset(lAsset, SkillsFolder, BuildFileName("Skill", lId, _DisplayName, "skill"));
            Close();
        }

        #endregion
    }
}
