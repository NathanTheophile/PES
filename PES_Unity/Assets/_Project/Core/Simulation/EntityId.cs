// Utility: this script defines a lightweight domain identifier used to reference entities
// (units, actors, targets) in a deterministic and serializable way throughout the simulation.
namespace PES.Core.Simulation
{
    /// <summary>
    /// Immutable value object representing a stable entity identifier inside the battle simulation.
    /// Keeping IDs as plain integers makes save/load, event logs, and multiplayer replication easier.
    /// </summary>
    public readonly struct EntityId
    {
        /// <summary>
        /// Creates an entity identifier from a raw integer value.
        /// </summary>
        public EntityId(int value)
        {
            // We store the value once at construction time because this struct is immutable.
            Value = value;
        }

        /// <summary>
        /// Raw numeric value of the identifier.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Human-readable format useful for debugging and logs.
        /// </summary>
        public override string ToString()
        {
            return $"Entity({Value})";
        }
    }
}
