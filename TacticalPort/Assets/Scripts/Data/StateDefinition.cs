#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "StateDefinition", menuName = "TacticalPort/Data/State Definition")]
    public sealed class StateDefinition : ScriptableObject
    {
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

        [TabGroup("Rules")]
        [LabelText("Duration Turns")]
        [SerializeField, Min(0)] private int _DurationTurns = 0;

        [TabGroup("Rules")]
        [LabelText("Max Stacks")]
        [SerializeField, Min(1)] private int _MaxStacks = 1;

        [TabGroup("Rules")]
        [LabelText("Passive Marker")]
        [InfoBox("Passive marker states can be used to expose passive effects without applying numeric modifiers.", InfoMessageType.Info)]
        [SerializeField] private bool _IsPassiveMarker;

        [TabGroup("Rules")]
        [LabelText("Blocks Healing")]
        [Tooltip("Prevents standard healing received by the affected unit. Direct mechanical repairs remain allowed.")]
        [SerializeField] private bool _BlocksHealing;

        [TabGroup("Rules")]
        [LabelText("Protects Duration Reduction")]
        [Tooltip("Prevents skill effects from reducing this state's remaining duration.")]
        [SerializeField] private bool _ProtectsDurationReduction;

        [TabGroup("Rules")]
        [LabelText("Source Scoped Instances")]
        [Tooltip("When enabled, the same state keeps one independently capped instance per source unit.")]
        [SerializeField] private bool _UsesSourceScopedInstances;

        [TabGroup("Rules")]
        [LabelText("Enables Skill Variant")]
        [SerializeField] private bool _EnablesSkillVariant;

        [TabGroup("Rules")]
        [ShowIf(nameof(_EnablesSkillVariant))]
        [LabelText("Variant Consumed Replacement")]
        [ValidateInput(nameof(IsVariantReplacementValid), "A state cannot replace itself when consuming a variant.")]
        [SerializeField] private StateDefinition _VariantConsumedReplacementState;

        [TabGroup("Rules")]
        [LabelText("Exclusive Group Id")]
        [Tooltip("States sharing a non-empty group replace each other on the same unit.")]
        [SerializeField] private string _ExclusiveGroupId = string.Empty;

        [TabGroup("Rules")]
        [LabelText("Begin Turn Damage / Stack")]
        [SerializeField, Min(0)] private int _BeginTurnDamagePerStack;

        [TabGroup("Modifiers")]
        [LabelText("General Damage %/Stack")]
        [SerializeField] private int _DamageModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Melee %/Stack")]
        [SerializeField] private int _MeleeDamageModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Ranged %/Stack")]
        [SerializeField] private int _RangedDamageModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Melee Resistance %/Stack")]
        [SerializeField] private int _MeleeResistancePercentPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Ranged Resistance %/Stack")]
        [SerializeField] private int _RangedResistancePercentPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("General Resistance %/Stack")]
        [SerializeField] private int _GeneralResistancePercentPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Wear %/Stack")]
        [Tooltip("Added to the universal 5% wear applied by skill damage.")]
        [SerializeField] private int _WearPercentPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Range/Stack")]
        [SerializeField] private int _RangeModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Energy/Stack")]
        [SerializeField] private int _EnergyModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Mobility/Stack")]
        [SerializeField] private int _MobilityModifierPerStack = 0;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string EnglishDisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string EnglishDescription => _Description;
        public string DisplayName => GameLocalization.GetContentName(GameLocalization.StatesTable, Id, EnglishDisplayName);
        public string Description => GameLocalization.GetContentDescription(GameLocalization.StatesTable, Id, EnglishDescription);
        public int DurationTurns => Mathf.Max(0, _DurationTurns);
        public int MaxStacks => Mathf.Max(1, _MaxStacks);
        public bool IsPassiveMarker => _IsPassiveMarker;
        public bool BlocksHealing => _BlocksHealing;
        public bool ProtectsDurationReduction => _ProtectsDurationReduction;
        public bool UsesSourceScopedInstances => _UsesSourceScopedInstances;
        public bool EnablesSkillVariant => _EnablesSkillVariant;
        public StateDefinition VariantConsumedReplacementState => _VariantConsumedReplacementState != this ? _VariantConsumedReplacementState : null;
        public string ExclusiveGroupId => _ExclusiveGroupId?.Trim() ?? string.Empty;
        public int BeginTurnDamagePerStack => Mathf.Max(0, _BeginTurnDamagePerStack);
        public int DamageModifierPerStack => _DamageModifierPerStack;
        public int MeleeDamageModifierPerStack => _MeleeDamageModifierPerStack;
        public int RangedDamageModifierPerStack => _RangedDamageModifierPerStack;
        public int MeleeResistancePercentPerStack => _MeleeResistancePercentPerStack;
        public int RangedResistancePercentPerStack => _RangedResistancePercentPerStack;
        public int GeneralResistancePercentPerStack => _GeneralResistancePercentPerStack;
        public int WearPercentPerStack => _WearPercentPerStack;
        public int RangeModifierPerStack => _RangeModifierPerStack;
        public int EnergyModifierPerStack => _EnergyModifierPerStack;
        public int MobilityModifierPerStack => _MobilityModifierPerStack;

        private bool IsVariantReplacementValid() => _VariantConsumedReplacementState == null || _VariantConsumedReplacementState != this;

        #endregion
    }
}
