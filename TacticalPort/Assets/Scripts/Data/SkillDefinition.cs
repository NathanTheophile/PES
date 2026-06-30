#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using TacticalPort.Shared;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    public enum SkillAoeShape
    {
        Single = 0,
        Circle = 1,
        Cross = 2,
        Square = 3,
        X = 4,
        HorizontalLine = 5,
        VerticalLine = 6,
        Cone = 7,
        ConeReverse = 8
    }

    public enum SkillTargetAlignment
    {
        Any = 0,
        Orthogonal = 1,
        Diagonal = 2
    }

    public enum SkillSummonTeamRule
    {
        Definition = 0,
        Caster = 1,
        EnemyOfCaster = 2
    }

    public enum SkillGlyphTargetRule
    {
        Anyone = 0,
        AlliesOnly = 1,
        EnemiesOnly = 2
    }

    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "Project/Data/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        public const int FixedAoeDamageFalloffPercentPerCell = 20;

        #region _____________________________/ VALUES

        [TabGroup("Metadata")]
        [LabelText("Id")]
        [SerializeField] private string _Id = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Display Name")]
        [SerializeField] private string _DisplayName = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Description")]
        [SerializeField, TextArea] private string _Description = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Icon")]
        [PreviewField(64)]
        [SerializeField] private Sprite _Icon;

        [TabGroup("Casting")]
        [LabelText("Primary Effect")]
        [SerializeField] private SkillPrimaryEffectType _PrimaryEffectType = SkillPrimaryEffectType.Damage;

        [TabGroup("Casting")]
        [LabelText("Additional Effect")]
        [SerializeField] private SkillAdditionalEffectType _AdditionalEffectType = SkillAdditionalEffectType.None;

        [TabGroup("Casting")]
        [LabelText("Can Affect Caster")]
        [SerializeField] private bool _CanAffectCaster = true;

        [TabGroup("Casting")]
        [LabelText("Range Min")]
        [ValidateInput(nameof(IsRangeValid), "Range Min must be lower than or equal to Range Max.")]
        [SerializeField, Min(0)] private int _RangeMin = 0;

        [TabGroup("Casting")]
        [LabelText("Range Max")]
        [ValidateInput(nameof(IsRangeValid), "Range Max must be greater than or equal to Range Min.")]
        [SerializeField, Min(0)] private int _RangeMax = 0;

        [TabGroup("Casting")]
        [LabelText("Target Alignment")]
        [SerializeField] private SkillTargetAlignment _TargetAlignment = SkillTargetAlignment.Any;

        [TabGroup("Casting")]
        [LabelText("Requires Line Of Sight")]
        [SerializeField] private bool _RequiresLineOfSight;

        [TabGroup("Casting")]
        [LabelText("AoE Shape")]
        [SerializeField] private SkillAoeShape _AoeShape = SkillAoeShape.Single;

        [TabGroup("Casting")]
        [ShowIf(nameof(HasAreaOfEffect))]
        [LabelText("AoE Size")]
        [SerializeField, Min(0)] private int _AoeSize = 0;

        [SerializeField, HideInInspector, Range(0, 100)] private int _AoeDamageFalloffPercentPerCell = 0;

        [TabGroup("Casting")]
        [ShowIf(nameof(HasAreaOfEffect))]
        [LabelText("Use AoE Falloff")]
        [ShowInInspector]
        private bool UseAoeDamageFalloffInInspector
        {
            get => UseAoeDamageFalloff;
            set => _AoeDamageFalloffPercentPerCell = HasAreaOfEffect() && value ? FixedAoeDamageFalloffPercentPerCell : 0;
        }

        [TabGroup("Usage")]
        [LabelText("AP Cost")]
        [SerializeField, Min(0)] private int _ActionPointCost = 1;

        [TabGroup("Usage")]
        [LabelText("Use/Turn")]
        [SerializeField, Min(0)] private int _UsePerTurn = 0;

        [TabGroup("Usage")]
        [LabelText("Use/Target")]
        [SerializeField, Min(0)] private int _UsePerTarget = 0;

        [TabGroup("Usage")]
        [LabelText("Cooldown Turns")]
        [SerializeField, Min(0)] private int _CooldownTurns = 0;

        [TabGroup("Effects")]
        [LabelText("Power")]
        [SerializeField, Min(0)] private int _Power = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesPushEffect))]
        [LabelText("Push Distance")]
        [SerializeField, Min(0)] private int _PushDistance = 0;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonEffect))]
        [Required("Summon Unit is required when Additional Effect is Summon.")]
        [LabelText("Summon Unit")]
        [SerializeField] private UnitDefinition _SummonUnit;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonEffect))]
        [LabelText("Summon Team")]
        [SerializeField] private SkillSummonTeamRule _SummonTeamRule = SkillSummonTeamRule.Definition;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphEffect))]
        [LabelText("Glyph Duration")]
        [SerializeField, Min(1)] private int _GlyphDurationTurns = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphEffect))]
        [LabelText("Glyph Target")]
        [SerializeField] private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;

        [TabGroup("Effects")]
        [LabelText("Applied State")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [SerializeField] private StateDefinition _AppliedState;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesAppliedState))]
        [LabelText("State Stacks")]
        [SerializeField, Min(1)] private int _AppliedStateStacks = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesAppliedState))]
        [LabelText("State Duration")]
        [Tooltip("Use -1 to keep the duration defined by the state asset.")]
        [SerializeField] private int _AppliedStateDurationTurns = -1;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public SkillPrimaryEffectType PrimaryEffectType => _PrimaryEffectType;
        public SkillAdditionalEffectType AdditionalEffectType => _AdditionalEffectType;
        public int Range => RangeMax;
        public int RangeMin => Mathf.Clamp(_RangeMin, 0, RangeMax);
        public int RangeMax => Mathf.Max(0, _RangeMax);
        public SkillTargetAlignment TargetAlignment => _TargetAlignment;
        public bool RequiresLineOfSight => _RequiresLineOfSight;
        public bool CanAffectCaster => _CanAffectCaster;
        public SkillAoeShape AoeShape => _AoeShape;
        public int AoeSize => Mathf.Max(0, _AoeShape == SkillAoeShape.Single ? 0 : _AoeSize);
        public bool UseAoeDamageFalloff => _AoeShape != SkillAoeShape.Single && _AoeDamageFalloffPercentPerCell > 0;
        public int AoeDamageFalloffPercentPerCell => UseAoeDamageFalloff ? FixedAoeDamageFalloffPercentPerCell : 0;
        public int UsePerTurn => Mathf.Max(0, _UsePerTurn);
        public int UsePerTarget => Mathf.Max(0, _UsePerTarget);
        public int CooldownTurns => Mathf.Max(0, _CooldownTurns);
        public int Power => Mathf.Max(0, _Power);
        public int ActionPointCost => Mathf.Max(0, _ActionPointCost);
        public int PushDistance => Mathf.Max(0, _PushDistance > 0 ? _PushDistance : _Power);
        public UnitDefinition SummonUnit => _SummonUnit;
        public SkillSummonTeamRule SummonTeamRule => _SummonTeamRule;
        public int GlyphDurationTurns => Mathf.Max(1, _GlyphDurationTurns);
        public SkillGlyphTargetRule GlyphTargetRule => _GlyphTargetRule;
        public StateDefinition AppliedState => _AppliedState;
        public int AppliedStateStacks => Mathf.Max(1, _AppliedStateStacks);
        public int AppliedStateDurationTurns => _AppliedStateDurationTurns;
        public Sprite Icon => _Icon;

        #endregion

        #region _____________________________| ODIN

        private bool IsRangeValid() => _RangeMin <= _RangeMax;
        private bool HasAreaOfEffect() => _AoeShape != SkillAoeShape.Single;
        private bool UsesPushEffect() => _AdditionalEffectType == SkillAdditionalEffectType.Push;
        private bool UsesSummonEffect() => _AdditionalEffectType == SkillAdditionalEffectType.Summon;
        private bool UsesGlyphEffect() => _AdditionalEffectType == SkillAdditionalEffectType.CreateGlyph;
        private bool UsesAppliedState() => _AppliedState != null;

        #endregion
    }
}
