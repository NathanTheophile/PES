using System;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class BattleUnitStateEntry
    {
        [SerializeField] private StateDefinition _State;
        [SerializeField, Min(1)] private int _Stacks = 1;

        public StateDefinition State => _State;
        public int Stacks => Mathf.Max(1, _Stacks);
    }
}
