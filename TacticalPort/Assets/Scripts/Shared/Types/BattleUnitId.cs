#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Shared
#endregion

using System;

namespace TacticalPort.Shared
{
    [Serializable]
    public readonly struct UnitId : IEquatable<UnitId>
    {
        #region _____________________________/ VALUES

        public static UnitId None => new UnitId(0);

        #endregion

        #region _____________________________/ ACCESSORS

        public int Value { get; }
        public bool IsValid => Value > 0;

        #endregion

        #region _____________________________| INIT

        public UnitId(int value)
        {
            Value = value;
        }

        #endregion

        #region _____________________________| HELPERS

        public bool Equals(UnitId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UnitId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static bool operator ==(UnitId left, UnitId right) => left.Equals(right);
        public static bool operator !=(UnitId left, UnitId right) => !left.Equals(right);

        #endregion
    }
}
