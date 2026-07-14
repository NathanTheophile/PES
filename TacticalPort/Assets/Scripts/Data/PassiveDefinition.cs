#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    public enum PassiveTrigger
    {
        None = 0,
        OwnerDealtSkillDamageToEnemy = 1,
        OwnerTookSkillDamageFromEnemy = 2
    }

    [CreateAssetMenu(fileName = "PassiveDefinition", menuName = "Project/Data/Passive Definition")]
    public sealed class PassiveDefinition : ScriptableObject
    {
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

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public Sprite Icon => _Icon;
        public PassiveTrigger Trigger => _Trigger;
        public StateProgressionDefinition StateProgression => _StateProgression;
        public int ProgressionStepsPerTrigger => Mathf.Max(1, _ProgressionStepsPerTrigger);
        public bool TriggerOnOwnerVariantExecutions => _TriggerOnOwnerVariantExecutions;
        #endregion
    }
}
