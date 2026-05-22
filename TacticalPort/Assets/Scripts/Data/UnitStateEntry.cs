#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class UnitStateEntry
    {
        #region _____________________________/ VALUES

        [SerializeField] private StateDefinition _State;
        [SerializeField, Min(1)] private int _Stacks = 1;

        #endregion

        #region _____________________________/ ACCESSORS

        public StateDefinition State => _State;
        public int Stacks => Mathf.Max(1, _Stacks);

        #endregion
    }
}
