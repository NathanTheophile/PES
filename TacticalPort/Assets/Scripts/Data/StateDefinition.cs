using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "StateDefinition", menuName = "TacticalPort/Data/State Definition")]
    public sealed class StateDefinition : ScriptableObject
    {
        #region _____________________________| VALUES

        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private string _DisplayName = string.Empty;
        [SerializeField, TextArea] private string _Description = string.Empty;

        [SerializeField, Min(0)] private int _DurationTurns = 0;
        [SerializeField, Min(1)] private int _MaxStacks = 1;
        [SerializeField] private bool _IsPassiveMarker;

        [SerializeField] private int _DamageModifierPerStack = 0;
        [SerializeField, Min(0)] private int _DamageReductionPerStack = 0;
        [SerializeField] private int _RangeModifierPerStack = 0;
        [SerializeField] private int _ActionPointModifierPerStack = 0;
        [SerializeField] private int _MovementModifierPerStack = 0;
        [SerializeField] private Sprite _Icon;

        #endregion

        #region _____________________________| ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public int DurationTurns => Mathf.Max(0, _DurationTurns);
        public int MaxStacks => Mathf.Max(1, _MaxStacks);
        public bool IsPassiveMarker => _IsPassiveMarker;
        public int DamageModifierPerStack => _DamageModifierPerStack;
        public int DamageReductionPerStack => Mathf.Max(0, _DamageReductionPerStack);
        public int RangeModifierPerStack => _RangeModifierPerStack;
        public int ActionPointModifierPerStack => _ActionPointModifierPerStack;
        public int MovementModifierPerStack => _MovementModifierPerStack;

        #endregion
    }
}
