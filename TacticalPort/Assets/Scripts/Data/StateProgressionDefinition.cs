using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "StateProgressionDefinition", menuName = "Project/Data/State Progression Definition")]
    public sealed class StateProgressionDefinition : ScriptableObject
    {
        [TabGroup("Metadata")]
        [LabelText("Id")]
        [SerializeField] private string _Id = string.Empty;

        [TabGroup("Progression")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [ValidateInput(nameof(HasValidStates), "Progression states must be non-null and unique.")]
        [SerializeField] private List<StateDefinition> _States = new List<StateDefinition>();

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public IReadOnlyList<StateDefinition> States => _States;

        private bool HasValidStates()
        {
            HashSet<StateDefinition> lUniqueStates = new HashSet<StateDefinition>();
            for (int lIndex = 0; lIndex < _States.Count; lIndex++)
            {
                if (_States[lIndex] == null || !lUniqueStates.Add(_States[lIndex]))
                    return false;
            }

            return true;
        }
    }
}
