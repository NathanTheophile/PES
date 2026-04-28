#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using TacticalPort.Shared;
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
        #region _____________________________/ VALUES

        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private string _DisplayName = string.Empty;
        [SerializeField, TextArea] private string _Description = string.Empty;

        [FormerlySerializedAs("_EffectType")]
        [SerializeField, HideInInspector] private SkillEffectType _LegacyEffectType = SkillEffectType.Damage;
        [SerializeField] private SkillPrimaryEffectType _PrimaryEffectType = SkillPrimaryEffectType.Damage;
        [SerializeField] private SkillAdditionalEffectType _AdditionalEffectType = SkillAdditionalEffectType.None;
        [SerializeField, HideInInspector] private bool _HasMigratedEffectSetup;
        [SerializeField, Min(0), HideInInspector] private int _Range = 0;
        [SerializeField, Min(0)] private int _RangeMin = 0;
        [SerializeField, Min(0)] private int _RangeMax = 0;
        [SerializeField, HideInInspector] private bool _Linear;
        [SerializeField] private SkillTargetAlignment _TargetAlignment = SkillTargetAlignment.Any;
        [SerializeField] private bool _RequiresLineOfSight;
        [SerializeField] private bool _CanAffectCaster = true;
        [SerializeField] private SkillAoeShape _AoeShape = SkillAoeShape.Single;
        [SerializeField, Min(0)] private int _AoeSize = 0;
        [SerializeField, Range(0, 100)] private int _AoeDamageFalloffPercentPerCell = 20;
        [SerializeField, Min(0)] private int _UsePerTurn = 0;
        [SerializeField, Min(0)] private int _UsePerTarget = 0;
        [SerializeField, Min(0)] private int _CooldownTurns = 0;
        [SerializeField, Min(0)] private int _Power = 1;
        [SerializeField, Min(0)] private int _ActionPointCost = 1;
        [SerializeField, Min(0)] private int _PushDistance = 0;
        [SerializeField] private UnitDefinition _SummonUnit;
        [SerializeField] private SkillSummonTeamRule _SummonTeamRule = SkillSummonTeamRule.Definition;
        [SerializeField, Min(1)] private int _GlyphDurationTurns = 1;
        [SerializeField] private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;
        [SerializeField] private StateDefinition _AppliedState;
        [SerializeField, Min(1)] private int _AppliedStateStacks = 1;
        [SerializeField] private int _AppliedStateDurationTurns = -1;
        [SerializeField] private bool _UseDirectionalModifiers;
        [SerializeField] private int _FrontDamageModifier = 0;
        [SerializeField] private int _SideDamageModifier = 0;
        [SerializeField] private int _BackDamageModifier = 0;
        [SerializeField] private Sprite _Icon;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public SkillPrimaryEffectType PrimaryEffectType => ResolvePrimaryEffectType();
        public SkillAdditionalEffectType AdditionalEffectType => ResolveAdditionalEffectType();
        public int Range => RangeMax;
        public int RangeMin => Mathf.Clamp(_RangeMin, 0, RangeMax);
        public int RangeMax => Mathf.Max(0, _RangeMax > 0 ? _RangeMax : _Range);
        public SkillTargetAlignment TargetAlignment => _TargetAlignment != SkillTargetAlignment.Any
            ? _TargetAlignment
            : (_Linear ? SkillTargetAlignment.Orthogonal : SkillTargetAlignment.Any);
        public bool Linear => TargetAlignment == SkillTargetAlignment.Orthogonal;
        public bool RequiresLineOfSight => _RequiresLineOfSight;
        public bool CanAffectCaster => _CanAffectCaster;
        public SkillAoeShape AoeShape => _AoeShape;
        public int AoeSize => Mathf.Max(0, _AoeShape == SkillAoeShape.Single ? 0 : _AoeSize);
        public int AoeDamageFalloffPercentPerCell => _AoeShape == SkillAoeShape.Single ? 0 : Mathf.Clamp(_AoeDamageFalloffPercentPerCell, 0, 100);
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
        public bool UseDirectionalModifiers => _UseDirectionalModifiers && PrimaryEffectType == SkillPrimaryEffectType.Damage;
        public int FrontDamageModifier => _FrontDamageModifier;
        public int SideDamageModifier => _SideDamageModifier;
        public int BackDamageModifier => _BackDamageModifier;
        public Sprite Icon => _Icon;

        #endregion

        #region _____________________________| UNITY

        private void OnValidate()
        {
            if (_HasMigratedEffectSetup)
                return;

            switch (_LegacyEffectType)
            {
                case SkillEffectType.Damage:
                    _PrimaryEffectType = SkillPrimaryEffectType.Damage;
                    _AdditionalEffectType = SkillAdditionalEffectType.None;
                    break;

                case SkillEffectType.Heal:
                    _PrimaryEffectType = SkillPrimaryEffectType.Heal;
                    _AdditionalEffectType = SkillAdditionalEffectType.None;
                    break;

                case SkillEffectType.Push:
                    _PrimaryEffectType = SkillPrimaryEffectType.None;
                    _AdditionalEffectType = SkillAdditionalEffectType.Push;
                    break;

                case SkillEffectType.Teleport:
                    _PrimaryEffectType = SkillPrimaryEffectType.None;
                    _AdditionalEffectType = SkillAdditionalEffectType.Teleport;
                    break;

                case SkillEffectType.SwitchPositions:
                    _PrimaryEffectType = SkillPrimaryEffectType.None;
                    _AdditionalEffectType = SkillAdditionalEffectType.SwitchPositions;
                    break;

                case SkillEffectType.Summon:
                    _PrimaryEffectType = SkillPrimaryEffectType.None;
                    _AdditionalEffectType = SkillAdditionalEffectType.Summon;
                    break;

                case SkillEffectType.CreateGlyph:
                    _PrimaryEffectType = SkillPrimaryEffectType.None;
                    _AdditionalEffectType = SkillAdditionalEffectType.CreateGlyph;
                    break;
            }

            _HasMigratedEffectSetup = true;
        }

        #endregion

        #region _____________________________| HELPERS

        private SkillPrimaryEffectType ResolvePrimaryEffectType() =>
            _HasMigratedEffectSetup
                ? _PrimaryEffectType
                : _LegacyEffectType == SkillEffectType.Heal
                    ? SkillPrimaryEffectType.Heal
                    : _LegacyEffectType == SkillEffectType.Damage
                        ? SkillPrimaryEffectType.Damage
                        : SkillPrimaryEffectType.None;

        private SkillAdditionalEffectType ResolveAdditionalEffectType() =>
            _HasMigratedEffectSetup
                ? _AdditionalEffectType
                : _LegacyEffectType switch
            {
                SkillEffectType.Push => SkillAdditionalEffectType.Push,
                SkillEffectType.Teleport => SkillAdditionalEffectType.Teleport,
                SkillEffectType.SwitchPositions => SkillAdditionalEffectType.SwitchPositions,
                SkillEffectType.Summon => SkillAdditionalEffectType.Summon,
                SkillEffectType.CreateGlyph => SkillAdditionalEffectType.CreateGlyph,
                _ => SkillAdditionalEffectType.None
            };

        #endregion
    }
}
