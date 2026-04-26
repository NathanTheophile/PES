using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace TacticalPort.Data
{
    public enum SkillAoeShape
    {
        Single = 0,
        Circle = 1,
        Cross = 2
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
        #region _____________________________| VALUES

        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private string _DisplayName = string.Empty;
        [SerializeField, TextArea] private string _Description = string.Empty;

        [SerializeField] private SkillTargetType _TargetType = SkillTargetType.Unit;
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
        [SerializeField] private bool _UseDirectionalModifiers;
        [SerializeField] private int _FrontDamageModifier = 0;
        [SerializeField] private int _SideDamageModifier = 0;
        [SerializeField] private int _BackDamageModifier = 0;
        [SerializeField] private Sprite _Icon;

        #endregion

        #region _____________________________| ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public SkillTargetType TargetType => _TargetType;
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

        #region _____________________________| FACTORIES

        public static SkillDefinition CreateRuntime(
            string pId,
            string pDisplayName,
            SkillTargetType pTargetType,
            SkillPrimaryEffectType pPrimaryEffectType,
            SkillAdditionalEffectType pAdditionalEffectType,
            int pRange,
            int pPower,
            int pActionPointCost,
            string pDescription = "",
            int pRangeMin = 0,
            bool pLinear = false,
            bool pRequiresLineOfSight = false,
            SkillAoeShape pAoeShape = SkillAoeShape.Single,
            int pAoeSize = 0,
            int pUsePerTurn = 0,
            int pUsePerTarget = 0,
            int pCooldownTurns = 0)
        {
            SkillDefinition lDefinition = CreateInstance<SkillDefinition>();
            lDefinition.hideFlags = HideFlags.DontSave;
            lDefinition.name = string.IsNullOrWhiteSpace(pDisplayName) ? "SkillRuntimeDefinition" : pDisplayName;
            lDefinition._Id = string.IsNullOrWhiteSpace(pId) ? lDefinition.name.ToLowerInvariant().Replace(" ", "_") : pId;
            lDefinition._DisplayName = string.IsNullOrWhiteSpace(pDisplayName) ? lDefinition._Id : pDisplayName;
            lDefinition._Description = pDescription ?? string.Empty;
            lDefinition._TargetType = pTargetType;
            lDefinition._LegacyEffectType = pPrimaryEffectType == SkillPrimaryEffectType.Heal ? SkillEffectType.Heal : SkillEffectType.Damage;
            lDefinition._PrimaryEffectType = pPrimaryEffectType;
            lDefinition._AdditionalEffectType = pAdditionalEffectType;
            lDefinition._HasMigratedEffectSetup = true;
            lDefinition._Range = Mathf.Max(0, pRange);
            lDefinition._RangeMin = Mathf.Clamp(pRangeMin, 0, Mathf.Max(0, pRange));
            lDefinition._RangeMax = Mathf.Max(0, pRange);
            lDefinition._Linear = pLinear;
            lDefinition._TargetAlignment = pLinear ? SkillTargetAlignment.Orthogonal : SkillTargetAlignment.Any;
            lDefinition._RequiresLineOfSight = pRequiresLineOfSight;
            lDefinition._CanAffectCaster = true;
            lDefinition._AoeShape = pAoeShape;
            lDefinition._AoeSize = Mathf.Max(0, pAoeShape == SkillAoeShape.Single ? 0 : pAoeSize);
            lDefinition._UsePerTurn = Mathf.Max(0, pUsePerTurn);
            lDefinition._UsePerTarget = Mathf.Max(0, pUsePerTarget);
            lDefinition._CooldownTurns = Mathf.Max(0, pCooldownTurns);
            lDefinition._Power = Mathf.Max(0, pPower);
            lDefinition._ActionPointCost = Mathf.Max(0, pActionPointCost);
            lDefinition._PushDistance = Mathf.Max(0, pPower);
            lDefinition._SummonUnit = null;
            lDefinition._SummonTeamRule = SkillSummonTeamRule.Definition;
            lDefinition._GlyphDurationTurns = 1;
            lDefinition._GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;
            lDefinition._UseDirectionalModifiers = false;
            lDefinition._FrontDamageModifier = 0;
            lDefinition._SideDamageModifier = 0;
            lDefinition._BackDamageModifier = 0;
            lDefinition._Icon = null;
            return lDefinition;
        }

        #endregion

        #region _____________________________| HELPERS

        private SkillPrimaryEffectType ResolvePrimaryEffectType()
        {
            if (_HasMigratedEffectSetup)
                return _PrimaryEffectType;

            return _LegacyEffectType == SkillEffectType.Heal
                ? SkillPrimaryEffectType.Heal
                : _LegacyEffectType == SkillEffectType.Damage
                    ? SkillPrimaryEffectType.Damage
                    : SkillPrimaryEffectType.None;
        }

        private SkillAdditionalEffectType ResolveAdditionalEffectType()
        {
            if (_HasMigratedEffectSetup)
                return _AdditionalEffectType;

            return _LegacyEffectType switch
            {
                SkillEffectType.Push => SkillAdditionalEffectType.Push,
                SkillEffectType.Teleport => SkillAdditionalEffectType.Teleport,
                SkillEffectType.SwitchPositions => SkillAdditionalEffectType.SwitchPositions,
                SkillEffectType.Summon => SkillAdditionalEffectType.Summon,
                SkillEffectType.CreateGlyph => SkillAdditionalEffectType.CreateGlyph,
                _ => SkillAdditionalEffectType.None
            };
        }

        #endregion
    }
}
