#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Shared
#endregion

using System;

namespace TacticalPort.Shared
{
    [Serializable]
    public readonly struct SkillId : IEquatable<SkillId>
    {
        #region _____________________________/ VALUES

        public static SkillId None => new SkillId(string.Empty);

        #endregion

        #region _____________________________/ ACCESSORS

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        #endregion

        #region _____________________________| INIT

        public SkillId(string value)
        {
            Value = value ?? string.Empty;
        }

        #endregion

        #region _____________________________| HELPERS

        public bool Equals(SkillId other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is SkillId other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(SkillId left, SkillId right) => left.Equals(right);
        public static bool operator !=(SkillId left, SkillId right) => !left.Equals(right);

        #endregion
    }
}
