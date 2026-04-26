using System;

namespace TacticalPort.Shared
{
    [Serializable]
    public readonly struct BattleUnitId : IEquatable<BattleUnitId>
    {
        public static BattleUnitId None => new BattleUnitId(0);

        public int Value { get; }
        public bool IsValid => Value > 0;

        public BattleUnitId(int value)
        {
            Value = value;
        }

        public bool Equals(BattleUnitId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleUnitId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(BattleUnitId left, BattleUnitId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BattleUnitId left, BattleUnitId right)
        {
            return !left.Equals(right);
        }
    }
}
