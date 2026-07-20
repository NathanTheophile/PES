#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using TacticalPort.Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
        ConeReverse = 8,
        PerpendicularLine = 9
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

    public enum SkillTargetType
    {
        Cell = 0,
        Unit = 1,
        Self = 2
    }

    public enum SkillTargetRelation
    {
        Anyone = 0,
        AlliesOnly = 1,
        EnemiesOnly = 2
    }

    public enum SkillTargetScope
    {
        Area = 0,
        AllMatchingUnits = 1
    }

    public enum UnitTargetType
    {
        AllUnits = 0,
        CharactersOnly = 1,
        SummonsOnly = 2
    }

    public enum SkillVariantTrigger
    {
        StateEnabler = 0,
        PrimaryTargetOwnedState = 1
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
        [LabelText("English Display Name (Fallback)")]
        [SerializeField] private string _DisplayName = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("English Description (Fallback)")]
        [SerializeField, TextArea] private string _Description = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Icon")]
        [PreviewField(64)]
        [SerializeField] private Sprite _Icon;

        [TabGroup("Casting")]
        [LabelText("Category")]
        [SerializeField] private SkillCategory _Category = SkillCategory.None;

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
        [LabelText("Target Type")]
        [SerializeField] private SkillTargetType _TargetType = SkillTargetType.Cell;

        [TabGroup("Casting")]
        [LabelText("Target Relation")]
        [SerializeField] private SkillTargetRelation _TargetRelation = SkillTargetRelation.Anyone;

        [TabGroup("Casting")]
        [LabelText("Override Effect Relation")]
        [SerializeField] private bool _OverrideEffectTargetRelation;

        [TabGroup("Casting")]
        [ShowIf(nameof(_OverrideEffectTargetRelation))]
        [LabelText("Effect Target Relation")]
        [SerializeField] private SkillTargetRelation _EffectTargetRelation = SkillTargetRelation.Anyone;

        [TabGroup("Casting")]
        [LabelText("Exclude Primary Target From Effects")]
        [SerializeField] private bool _ExcludePrimaryTargetFromEffects;

        [TabGroup("Casting")]
        [LabelText("Primary Target Must Be Owned")]
        [SerializeField] private bool _RequiresPrimaryTargetOwnedByCaster;

        [TabGroup("Casting")]
        [LabelText("Required Primary Target State")]
        [SerializeField] private StateDefinition _RequiredPrimaryTargetState;

        [TabGroup("Casting")]
        [LabelText("Target Scope")]
        [SerializeField] private SkillTargetScope _TargetScope = SkillTargetScope.Area;

        [TabGroup("Casting")]
        [LabelText("Target Unit Type")]
        [SerializeField] private UnitTargetType _TargetUnitType = UnitTargetType.AllUnits;

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
        [LabelText("Requires Visibility")]
        [FormerlySerializedAs("_RequiresLineOfSight")]
        [SerializeField] private bool _RequiresVisibility;

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
        [LabelText("Energy Cost")]
        [SerializeField, Min(0)] private int _EnergyCost = 1;

        [TabGroup("Usage")]
        [LabelText("Use/Turn")]
        [SerializeField, Min(0)] private int _UsePerTurn = 0;

        [TabGroup("Usage")]
        [LabelText("Use/Target")]
        [SerializeField, Min(0)] private int _UsePerTarget = 0;

        [TabGroup("Usage")]
        [LabelText("Cooldown Turns")]
        [SerializeField, Min(0)] private int _CooldownTurns = 0;

        [TabGroup("Variant")]
        [LabelText("Variant")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [ValidateInput(nameof(IsVariantValid), "A skill cannot reference itself as its variant.")]
        [SerializeField] private SkillDefinition _Variant;

        [TabGroup("Variant")]
        [LabelText("Variant Trigger")]
        [SerializeField] private SkillVariantTrigger _VariantTrigger = SkillVariantTrigger.StateEnabler;

        [TabGroup("Variant")]
        [ShowIf(nameof(UsesTargetVariantTrigger))]
        [LabelText("Variant Target State")]
        [SerializeField] private StateDefinition _VariantTargetRequiredState;

        [TabGroup("Effects")]
        [LabelText("Power")]
        [SerializeField, Min(0)] private int _Power = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesDamageEffect))]
        [LabelText("Life Steal")]
        [Tooltip("Heals the caster for 50% of health actually removed from enemy units.")]
        [SerializeField] private bool _HasLifeSteal;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesPushEffect))]
        [LabelText("Forced Movement Distance")]
        [SerializeField, Min(0)] private int _PushDistance = 0;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesRepairToggleEffect))]
        [LabelText("Repair Amount")]
        [SerializeField, Min(0)] private int _RepairAmount = 0;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesRepairToggleEffect))]
        [LabelText("Toggle State A")]
        [SerializeField] private StateDefinition _ToggleStateA;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesRepairToggleEffect))]
        [LabelText("Toggle State B")]
        [SerializeField] private StateDefinition _ToggleStateB;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesPassiveProgressionEffect))]
        [LabelText("Progression Steps")]
        [SerializeField, Min(1)] private int _PassiveProgressionSteps = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesDurationReductionEffect))]
        [LabelText("State Duration Reduction")]
        [SerializeField, Min(1)] private int _StateDurationReduction = 1;

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
        [ShowIf(nameof(UsesSummonEffect))]
        [LabelText("Replace Owned Summon Of Same Type")]
        [SerializeField] private bool _ReplaceOwnedSummonOfSameDefinition;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphEffect))]
        [LabelText("Glyph Duration")]
        [SerializeField, Min(1)] private int _GlyphDurationTurns = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphEffect))]
        [LabelText("Glyph Definition")]
        [Tooltip("Optional generic definition. When empty, the legacy glyph fields below remain in use.")]
        [SerializeField] private GlyphDefinition _GlyphDefinition;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphEffect))]
        [LabelText("Glyph Target")]
        [SerializeField] private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphEffect))]
        [LabelText("Glyph Applied State")]
        [SerializeField] private StateDefinition _GlyphAppliedState;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphState))]
        [LabelText("Glyph State Stacks")]
        [SerializeField, Min(1)] private int _GlyphAppliedStateStacks = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesGlyphState))]
        [LabelText("Glyph State Duration")]
        [SerializeField] private int _GlyphAppliedStateDurationTurns = -1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonEffect))]
        [LabelText("Summon Spawn State")]
        [SerializeField] private StateDefinition _SummonSpawnState;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonSpawnState))]
        [LabelText("Summon Spawn State Stacks")]
        [SerializeField, Min(1)] private int _SummonSpawnStateStacks = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonSpawnState))]
        [LabelText("Summon Spawn State Duration")]
        [SerializeField] private int _SummonSpawnStateDurationTurns = -1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonSpawnState))]
        [LabelText("Summon Spawn State Area")]
        [SerializeField, Min(0)] private int _SummonSpawnStateAreaSize = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesSummonSpawnState))]
        [LabelText("Summon Spawn State Target")]
        [SerializeField] private SkillGlyphTargetRule _SummonSpawnStateTargetRule = SkillGlyphTargetRule.EnemiesOnly;

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

        [TabGroup("Effects")]
        [LabelText("Caster Applied State")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [SerializeField] private StateDefinition _CasterAppliedState;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesCasterAppliedState))]
        [LabelText("Caster State Stacks")]
        [SerializeField, Min(1)] private int _CasterAppliedStateStacks = 1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesCasterAppliedState))]
        [LabelText("Caster State Duration")]
        [Tooltip("Use -1 to keep the duration defined by the state asset.")]
        [SerializeField] private int _CasterAppliedStateDurationTurns = -1;

        [TabGroup("Effects")]
        [ShowIf(nameof(UsesCasterAppliedState))]
        [LabelText("Caster State Stacks Per Affected Target")]
        [SerializeField] private bool _CasterStateStacksPerAffectedTarget;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string EnglishDisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string EnglishDescription => _Description;
        public string DisplayName => GameLocalization.GetContentName(GameLocalization.SkillsTable, Id, EnglishDisplayName);
        public string Description => GameLocalization.GetContentDescription(GameLocalization.SkillsTable, Id, EnglishDescription);
        public SkillCategory Category => _Category;
        public bool HasExplicitCategory => _Category != SkillCategory.None;
        public SkillPrimaryEffectType PrimaryEffectType => _PrimaryEffectType;
        public SkillAdditionalEffectType AdditionalEffectType => _AdditionalEffectType;
        public int Range => RangeMax;
        public int RangeMin => Mathf.Clamp(_RangeMin, 0, RangeMax);
        public int RangeMax => Mathf.Max(0, _RangeMax);
        public SkillTargetAlignment TargetAlignment => _TargetAlignment;
        public bool RequiresVisibility => _RequiresVisibility;
        public bool CanAffectCaster => _CanAffectCaster;
        public SkillTargetType TargetType => _TargetType;
        public SkillTargetRelation TargetRelation => _TargetRelation;
        public SkillTargetRelation EffectTargetRelation => _OverrideEffectTargetRelation ? _EffectTargetRelation : _TargetRelation;
        public bool ExcludePrimaryTargetFromEffects => _ExcludePrimaryTargetFromEffects;
        public bool RequiresPrimaryTargetOwnedByCaster => _RequiresPrimaryTargetOwnedByCaster;
        public StateDefinition RequiredPrimaryTargetState => _RequiredPrimaryTargetState;
        public SkillTargetScope TargetScope => _TargetScope;
        public UnitTargetType TargetUnitType => _TargetUnitType;
        public SkillAoeShape AoeShape => _AoeShape;
        public int AoeSize => Mathf.Max(0, _AoeShape == SkillAoeShape.Single ? 0 : _AoeSize);
        public bool UseAoeDamageFalloff => _AoeShape != SkillAoeShape.Single && _AoeDamageFalloffPercentPerCell > 0;
        public int AoeDamageFalloffPercentPerCell => UseAoeDamageFalloff ? FixedAoeDamageFalloffPercentPerCell : 0;
        public int UsePerTurn => Mathf.Max(0, _UsePerTurn);
        public int UsePerTarget => Mathf.Max(0, _UsePerTarget);
        public int CooldownTurns => Mathf.Max(0, _CooldownTurns);
        public SkillDefinition Variant => _Variant != this ? _Variant : null;
        public SkillVariantTrigger VariantTrigger => _VariantTrigger;
        public StateDefinition VariantTargetRequiredState => _VariantTargetRequiredState;
        public int Power => Mathf.Max(0, _Power);
        public bool HasLifeSteal => _HasLifeSteal && PrimaryEffectType == SkillPrimaryEffectType.Damage;
        public int EnergyCost => Mathf.Max(0, _EnergyCost);
        public int PushDistance => Mathf.Max(0, _PushDistance > 0 ? _PushDistance : _Power);
        public int RepairAmount => Mathf.Max(0, _RepairAmount);
        public StateDefinition ToggleStateA => _ToggleStateA;
        public StateDefinition ToggleStateB => _ToggleStateB;
        public int PassiveProgressionSteps => Mathf.Max(1, _PassiveProgressionSteps);
        public int StateDurationReduction => Mathf.Max(1, _StateDurationReduction);
        public UnitDefinition SummonUnit => _SummonUnit;
        public SkillSummonTeamRule SummonTeamRule => _SummonTeamRule;
        public bool ReplaceOwnedSummonOfSameDefinition => _ReplaceOwnedSummonOfSameDefinition;
        public int GlyphDurationTurns => Mathf.Max(1, _GlyphDurationTurns);
        public GlyphDefinition GlyphDefinition => _GlyphDefinition;
        public SkillGlyphTargetRule GlyphTargetRule => _GlyphTargetRule;
        public StateDefinition GlyphAppliedState => _GlyphAppliedState;
        public int GlyphAppliedStateStacks => Mathf.Max(1, _GlyphAppliedStateStacks);
        public int GlyphAppliedStateDurationTurns => _GlyphAppliedStateDurationTurns;
        public StateDefinition SummonSpawnState => _SummonSpawnState;
        public int SummonSpawnStateStacks => Mathf.Max(1, _SummonSpawnStateStacks);
        public int SummonSpawnStateDurationTurns => _SummonSpawnStateDurationTurns;
        public int SummonSpawnStateAreaSize => Mathf.Max(0, _SummonSpawnStateAreaSize);
        public SkillGlyphTargetRule SummonSpawnStateTargetRule => _SummonSpawnStateTargetRule;
        public StateDefinition AppliedState => _AppliedState;
        public int AppliedStateStacks => Mathf.Max(1, _AppliedStateStacks);
        public int AppliedStateDurationTurns => _AppliedStateDurationTurns;
        public StateDefinition CasterAppliedState => _CasterAppliedState;
        public int CasterAppliedStateStacks => Mathf.Max(1, _CasterAppliedStateStacks);
        public int CasterAppliedStateDurationTurns => _CasterAppliedStateDurationTurns;
        public bool CasterStateStacksPerAffectedTarget => _CasterStateStacksPerAffectedTarget;
        public Sprite Icon => _Icon;

        public DamageRangeType CategoryDamageRange =>
            _Category switch
            {
                SkillCategory.MeleeAtk => DamageRangeType.Melee,
                SkillCategory.RangedAtk => DamageRangeType.Ranged,
                _ => DamageRangeType.None
            };

        #endregion

        #region _____________________________| ODIN

        private bool IsRangeValid() => _RangeMin <= _RangeMax;
        private bool IsVariantValid() => _Variant == null || _Variant != this;
        private bool UsesTargetVariantTrigger() => _VariantTrigger == SkillVariantTrigger.PrimaryTargetOwnedState;
        private bool UsesDamageEffect() => _PrimaryEffectType == SkillPrimaryEffectType.Damage;
        private bool HasAreaOfEffect() => _AoeShape != SkillAoeShape.Single;
        private bool UsesPushEffect() => _AdditionalEffectType is SkillAdditionalEffectType.Push or SkillAdditionalEffectType.Pull;
        private bool UsesPassiveProgressionEffect() => _AdditionalEffectType == SkillAdditionalEffectType.AdvanceActivePassiveProgression;
        private bool UsesDurationReductionEffect() => _AdditionalEffectType == SkillAdditionalEffectType.ReduceStateDurations;
        private bool UsesRepairToggleEffect() => _AdditionalEffectType == SkillAdditionalEffectType.RepairAndToggleStates;
        private bool UsesSummonEffect() => _AdditionalEffectType == SkillAdditionalEffectType.Summon;
        private bool UsesGlyphEffect() => _AdditionalEffectType == SkillAdditionalEffectType.CreateGlyph;
        private bool UsesGlyphState() => UsesGlyphEffect() && _GlyphAppliedState != null;
        private bool UsesSummonSpawnState() => UsesSummonEffect() && _SummonSpawnState != null;
        private bool UsesAppliedState() => _AppliedState != null;
        private bool UsesCasterAppliedState() => _CasterAppliedState != null;

        #endregion
    }
}
