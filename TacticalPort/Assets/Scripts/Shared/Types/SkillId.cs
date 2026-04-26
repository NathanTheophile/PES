using System;

namespace TacticalPort.Shared
{
    [Serializable]
    public readonly struct SkillId : IEquatable<SkillId>
    {
        public static SkillId None => new SkillId(string.Empty);

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public SkillId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool Equals(SkillId other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is SkillId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(SkillId left, SkillId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SkillId left, SkillId right)
        {
            return !left.Equals(right);
        }
    }
}
