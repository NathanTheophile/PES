#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using System;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class UnitPhaseStateDefinition
    {
        #region _____________________________/ VALUES

        [SerializeField] private StateDefinition _State;
        [SerializeField, Range(0f, 1f)] private float _HealthThresholdNormalized = 0.5f;
        [SerializeField, Min(1)] private int _Stacks = 1;

        #endregion

        #region _____________________________/ ACCESSORS

        public StateDefinition State => _State;
        public float HealthThresholdNormalized => Mathf.Clamp01(_HealthThresholdNormalized);
        public int Stacks => Mathf.Max(1, _Stacks);

        #endregion
    }
}
