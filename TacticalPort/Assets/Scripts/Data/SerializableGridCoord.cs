#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using System;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public struct SerializableGridCoord
    {
        #region _____________________________/ VALUES

        [SerializeField] private int _X;
        [SerializeField] private int _Y;

        #endregion

        #region _____________________________| INIT

        public SerializableGridCoord(int pX, int pY)
        {
            _X = pX;
            _Y = pY;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public int X => _X;
        public int Y => _Y;

        #endregion

        #region _____________________________| CONVERT

        public GridCoord ToRuntime() => new GridCoord(_X, _Y);
        public Vector2Int ToVector2Int() => new Vector2Int(_X, _Y);
        public override string ToString() => $"({_X}, {_Y})";

        #endregion
    }
}
