#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace TacticalPort.Data
{
    public enum PassiveTrigger
    {
        None = 0,
        OwnerDealtSkillDamageToEnemy = 1,
        OwnerTookSkillDamageFromEnemy = 2,
        OwnerBeginTurn = 3
    }

    public enum PassiveBeginTurnEffect
    {
        None = 0,
        RestoreOwnedUnits = 1,
        ApplyOwnedUnitAuras = 2
    }

    public enum PassiveAutomaticTargetSelection
    {
        None = 0,
        LowestHealthPercentageAlly = 1
    }

    public enum PassiveHealthLossSourceRule
    {
        Anyone = 0,
        EnemiesOnly = 1,
        AlliesOnly = 2
    }

    public enum PassiveHealthLossReactionTarget
    {
        None = 0,
        MarkedUnit = 1,
        MarkedUnitAlliesExceptMarked = 2
    }

    [Serializable]
    public sealed class PassiveOwnedUnitAura
    {
        [SerializeField] private UnitDefinition _SourceUnit;
        [SerializeField] private StateDefinition _RequiredSourceState;
        [SerializeField] private StateDefinition _ExcludedBeneficiaryState;
        [SerializeField] private StateDefinition _AppliedState;
        [SerializeField, Min(1)] private int _Range = 1;

        public UnitDefinition SourceUnit => _SourceUnit;
        public StateDefinition RequiredSourceState => _RequiredSourceState;
        public StateDefinition ExcludedBeneficiaryState => _ExcludedBeneficiaryState;
        public StateDefinition AppliedState => _AppliedState;
        public int Range => Mathf.Max(1, _Range);
    }

    [CreateAssetMenu(fileName = "PassiveDefinition", menuName = "Project/Data/Passive Definition")]
    public sealed class PassiveDefinition : ScriptableObject
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

        [TabGroup("Gameplay")]
        [LabelText("Trigger")]
        [SerializeField] private PassiveTrigger _Trigger = PassiveTrigger.None;

        [TabGroup("Gameplay")]
        [LabelText("State Progression")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [SerializeField] private StateProgressionDefinition _StateProgression;

        [TabGroup("Gameplay")]
        [LabelText("Progression Steps / Trigger")]
        [SerializeField, Min(1)] private int _ProgressionStepsPerTrigger = 1;

        [TabGroup("Gameplay")]
        [LabelText("Trigger On Owner Variant")]
        [SerializeField] private bool _TriggerOnOwnerVariantExecutions = true;

        [TabGroup("Begin Turn")]
        [LabelText("Effect")]
        [SerializeField] private PassiveBeginTurnEffect _BeginTurnEffect;

        [TabGroup("Begin Turn")]
        [LabelText("Owned Unit Required State")]
        [SerializeField] private StateDefinition _OwnedUnitRequiredState;

        [TabGroup("Begin Turn")]
        [LabelText("Restore Health")]
        [SerializeField, Min(0)] private int _OwnedUnitRestoreHealth;

        [TabGroup("Begin Turn")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<PassiveOwnedUnitAura> _OwnedUnitAuras = new List<PassiveOwnedUnitAura>();

        [TabGroup("Observed Health Loss")]
        [LabelText("Automatic Target Selection")]
        [SerializeField] private PassiveAutomaticTargetSelection _AutomaticTargetSelection;

        [TabGroup("Observed Health Loss")]
        [LabelText("Target Marker")]
        [SerializeField] private StateDefinition _AutomaticTargetMarker;

        [TabGroup("Observed Health Loss")]
        [LabelText("Automatic Target Unit Type")]
        [FormerlySerializedAs("_ExcludeSummonsFromAutomaticTarget")]
        [SerializeField] private UnitTargetType _AutomaticTargetUnitType = UnitTargetType.CharactersOnly;

        [TabGroup("Observed Health Loss")]
        [LabelText("Damage Source Rule")]
        [SerializeField] private PassiveHealthLossSourceRule _HealthLossSourceRule = PassiveHealthLossSourceRule.EnemiesOnly;

        [TabGroup("Observed Health Loss")]
        [LabelText("Reaction Targets")]
        [SerializeField] private PassiveHealthLossReactionTarget _HealthLossReactionTarget;

        [TabGroup("Observed Health Loss")]
        [LabelText("Reaction State")]
        [SerializeField] private StateDefinition _HealthLossReactionState;

        [TabGroup("Observed Health Loss")]
        [LabelText("Reaction State Stacks")]
        [SerializeField, Min(1)] private int _HealthLossReactionStateStacks = 1;

        [TabGroup("Observed Health Loss")]
        [LabelText("Include Summon Beneficiaries")]
        [SerializeField] private bool _IncludeSummonsAsReactionBeneficiaries;

        [TabGroup("Observed Health Loss")]
        [LabelText("Clear Reaction State On Owner Begin Turn")]
        [SerializeField] private bool _ClearReactionStateOnOwnerBeginTurn;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string EnglishDisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string EnglishDescription => _Description;
        public string DisplayName => GameLocalization.GetContentName(GameLocalization.PassivesTable, Id, EnglishDisplayName);
        public string Description => GameLocalization.GetContentDescription(GameLocalization.PassivesTable, Id, EnglishDescription);
        public Sprite Icon => _Icon;
        public PassiveTrigger Trigger => _Trigger;
        public StateProgressionDefinition StateProgression => _StateProgression;
        public int ProgressionStepsPerTrigger => Mathf.Max(1, _ProgressionStepsPerTrigger);
        public bool TriggerOnOwnerVariantExecutions => _TriggerOnOwnerVariantExecutions;
        public PassiveBeginTurnEffect BeginTurnEffect => _BeginTurnEffect;
        public StateDefinition OwnedUnitRequiredState => _OwnedUnitRequiredState;
        public int OwnedUnitRestoreHealth => Mathf.Max(0, _OwnedUnitRestoreHealth);
        public IReadOnlyList<PassiveOwnedUnitAura> OwnedUnitAuras => _OwnedUnitAuras;
        public PassiveAutomaticTargetSelection AutomaticTargetSelection => _AutomaticTargetSelection;
        public StateDefinition AutomaticTargetMarker => _AutomaticTargetMarker;
        public UnitTargetType AutomaticTargetUnitType => _AutomaticTargetUnitType;
        public bool ExcludeSummonsFromAutomaticTarget => _AutomaticTargetUnitType == UnitTargetType.CharactersOnly;
        public PassiveHealthLossSourceRule HealthLossSourceRule => _HealthLossSourceRule;
        public PassiveHealthLossReactionTarget HealthLossReactionTarget => _HealthLossReactionTarget;
        public StateDefinition HealthLossReactionState => _HealthLossReactionState;
        public int HealthLossReactionStateStacks => Mathf.Max(1, _HealthLossReactionStateStacks);
        public bool IncludeSummonsAsReactionBeneficiaries => _IncludeSummonsAsReactionBeneficiaries;
        public bool ClearReactionStateOnOwnerBeginTurn => _ClearReactionStateOnOwnerBeginTurn;
        #endregion
    }
}
