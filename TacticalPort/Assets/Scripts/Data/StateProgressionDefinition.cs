using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class StateProgressionLevel
    {
        [Required]
        public StateDefinition State;

        [Min(1)]
        public int Stacks = 1;
    }

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

        [TabGroup("Progression")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [ValidateInput(nameof(HasValidLevels), "Progression levels must have a state, valid stacks, and unique { State, Stacks } pairs.")]
        [SerializeField] private List<StateProgressionLevel> _Levels = new List<StateProgressionLevel>();

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public IReadOnlyList<StateDefinition> States => _States;
        public IReadOnlyList<StateProgressionLevel> Levels => _Levels;
        public int LevelCount => _Levels != null && _Levels.Count > 0 ? _Levels.Count : _States?.Count ?? 0;

        public StateDefinition GetLevelState(int pIndex) => UsesStackLevels
            ? _Levels[pIndex]?.State
            : _States[pIndex];

        public int GetLevelStacks(int pIndex) => UsesStackLevels
            ? Math.Max(1, _Levels[pIndex]?.Stacks ?? 1)
            : 1;

        private bool UsesStackLevels => _Levels != null && _Levels.Count > 0;

        private bool HasValidStates()
        {
            if (_States == null)
                return true;

            HashSet<StateDefinition> lUniqueStates = new HashSet<StateDefinition>();
            for (int lIndex = 0; lIndex < _States.Count; lIndex++)
            {
                if (_States[lIndex] == null || !lUniqueStates.Add(_States[lIndex]))
                    return false;
            }

            return true;
        }

        private bool HasValidLevels()
        {
            if (_Levels == null)
                return true;

            HashSet<(StateDefinition State, int Stacks)> lUniqueLevels = new HashSet<(StateDefinition, int)>();
            for (int lIndex = 0; lIndex < _Levels.Count; lIndex++)
            {
                StateProgressionLevel lLevel = _Levels[lIndex];
                if (lLevel?.State == null
                    || lLevel.Stacks < 1
                    || lLevel.Stacks > lLevel.State.MaxStacks
                    || !lUniqueLevels.Add((lLevel.State, lLevel.Stacks)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
