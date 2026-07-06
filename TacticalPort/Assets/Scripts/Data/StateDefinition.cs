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
        [LabelText("Display Name")]
        [SerializeField] private string _DisplayName = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Description")]
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

        [TabGroup("Modifiers")]
        [LabelText("Damage %/Stack")]
        [SerializeField] private int _DamageModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Melee %/Stack")]
        [SerializeField] private int _MeleeDamageModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Ranged %/Stack")]
        [SerializeField] private int _RangedDamageModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Melee Resistance %/Stack")]
        [SerializeField, Range(0, 100)] private int _MeleeResistancePercentPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Ranged Resistance %/Stack")]
        [SerializeField, Range(0, 100)] private int _RangedResistancePercentPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("Range/Stack")]
        [SerializeField] private int _RangeModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("AP/Stack")]
        [SerializeField] private int _ActionPointModifierPerStack = 0;

        [TabGroup("Modifiers")]
        [LabelText("MP/Stack")]
        [SerializeField] private int _MovementModifierPerStack = 0;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public int DurationTurns => Mathf.Max(0, _DurationTurns);
        public int MaxStacks => Mathf.Max(1, _MaxStacks);
        public bool IsPassiveMarker => _IsPassiveMarker;
        public int DamageModifierPerStack => _DamageModifierPerStack;
        public int MeleeDamageModifierPerStack => _MeleeDamageModifierPerStack;
        public int RangedDamageModifierPerStack => _RangedDamageModifierPerStack;
        public int MeleeResistancePercentPerStack => Mathf.Clamp(_MeleeResistancePercentPerStack, 0, 100);
        public int RangedResistancePercentPerStack => Mathf.Clamp(_RangedResistancePercentPerStack, 0, 100);
        public int RangeModifierPerStack => _RangeModifierPerStack;
        public int ActionPointModifierPerStack => _ActionPointModifierPerStack;
        public int MovementModifierPerStack => _MovementModifierPerStack;

        #endregion
    }
}
