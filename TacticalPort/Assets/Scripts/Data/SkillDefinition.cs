using TacticalPort.Shared;
using UnityEngine;

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

    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "TacticalPort/Data/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        #region _____________________________| VALUES

        [Header("Identity")]
        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private string _DisplayName = string.Empty;
        [SerializeField, TextArea] private string _Description = string.Empty;

        [Header("Rules")]
        [SerializeField] private SkillTargetType _TargetType = SkillTargetType.Unit;
        [SerializeField] private SkillEffectType _EffectType = SkillEffectType.Damage;
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

        [Header("Advanced Combat")]
        [SerializeField, Min(0)] private int _PushDistance = 0;
        [SerializeField] private BattleUnitDefinition _SummonUnit;
        [SerializeField] private SkillSummonTeamRule _SummonTeamRule = SkillSummonTeamRule.Definition;
        [SerializeField, Min(1)] private int _GlyphDurationTurns = 1;
        [SerializeField] private SkillGlyphTargetRule _GlyphTargetRule = SkillGlyphTargetRule.EnemiesOnly;
        [SerializeField] private bool _UseDirectionalModifiers;
        [SerializeField] private int _FrontDamageModifier = 0;
        [SerializeField] private int _SideDamageModifier = 0;
        [SerializeField] private int _BackDamageModifier = 0;

        [Header("Presentation")]
        [SerializeField] private Sprite _Icon;

        #endregion

        #region _____________________________| ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public SkillTargetType TargetType => _TargetType;
        public SkillEffectType EffectType => _EffectType;
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
        public BattleUnitDefinition SummonUnit => _SummonUnit;
        public SkillSummonTeamRule SummonTeamRule => _SummonTeamRule;
        public int GlyphDurationTurns => Mathf.Max(1, _GlyphDurationTurns);
        public SkillGlyphTargetRule GlyphTargetRule => _GlyphTargetRule;
        public bool UseDirectionalModifiers => _UseDirectionalModifiers && _EffectType == SkillEffectType.Damage;
        public int FrontDamageModifier => _FrontDamageModifier;
        public int SideDamageModifier => _SideDamageModifier;
        public int BackDamageModifier => _BackDamageModifier;
        public Sprite Icon => _Icon;

        #endregion

        #region _____________________________| FACTORIES

        public static SkillDefinition CreateRuntime(
            string pId,
            string pDisplayName,
            SkillTargetType pTargetType,
            SkillEffectType pEffectType,
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
            lDefinition._EffectType = pEffectType;
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
    }
}
