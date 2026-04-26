using System;

namespace TacticalPort.Shared
{
    [Serializable]
    public readonly struct UnitId : IEquatable<UnitId>
    {
        public static UnitId None => new UnitId(0);

        public int Value { get; }
        public bool IsValid => Value > 0;

        public UnitId(int value)
        {
            Value = value;
        }

        public bool Equals(UnitId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(UnitId left, UnitId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnitId left, UnitId right)
        {
            return !left.Equals(right);
        }
    }
}
