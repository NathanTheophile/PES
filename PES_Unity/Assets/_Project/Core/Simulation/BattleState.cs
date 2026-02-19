// Utility: this script contains the in-memory combat state used by the resolver pipeline.
// It centralizes deterministic data mutation (positions, ticks, and event log entries).
using System.Collections.Generic;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Domain state container for the tactical battle simulation.
    /// This class intentionally contains no Unity-specific API so it remains testable and portable.
    /// </summary>
    public sealed class BattleState
    {
        // Current positions of each known entity.
        // Dictionary gives O(1) lookup/update for most simulation operations.
        private readonly Dictionary<EntityId, Position3> _entityPositions = new();

        // Ordered, append-only list of text events produced by resolved actions.
        private readonly List<string> _eventLog = new();

        /// <summary>
        /// Read-only view over the event log so external code can inspect but not mutate directly.
        /// </summary>
        public IReadOnlyList<string> EventLog => _eventLog;

        /// <summary>
        /// Monotonic simulation counter incremented once per resolved action.
        /// </summary>
        public int Tick { get; private set; }

        /// <summary>
        /// Advances simulation time by one tick.
        /// </summary>
        public void AdvanceTick()
        {
            Tick++;
        }

        /// <summary>
        /// Appends a new textual event to the domain event log.
        /// </summary>
        public void AddEvent(string evt)
        {
            _eventLog.Add(evt);
        }

        /// <summary>
        /// Sets or replaces the position of an entity.
        /// Used for setup/bootstrap or explicit domain updates.
        /// </summary>
        public void SetEntityPosition(EntityId entityId, Position3 position)
        {
            _entityPositions[entityId] = position;
        }

        /// <summary>
        /// Tries to read an entity position.
        /// Returns false when the entity has no known position in current state.
        /// </summary>
        public bool TryGetEntityPosition(EntityId entityId, out Position3 position)
        {
            return _entityPositions.TryGetValue(entityId, out position);
        }

        /// <summary>
        /// Atomically moves an entity only if its current position matches the expected origin.
        /// This protects the simulation against stale commands and race-like ordering issues.
        /// </summary>
        public bool TryMoveEntity(EntityId entityId, Position3 expectedOrigin, Position3 destination)
        {
            // Reject when entity is missing OR when command origin is stale/invalid.
            if (!_entityPositions.TryGetValue(entityId, out var current) || !current.Equals(expectedOrigin))
            {
                return false;
            }

            // Apply movement once precondition passes.
            _entityPositions[entityId] = destination;
            return true;
        }
    }

    /// <summary>
    /// Immutable 3D integer position used in battle state storage.
    /// </summary>
    public readonly struct Position3
    {
        /// <summary>
        /// Creates a new 3D position value.
        /// </summary>
        public Position3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Horizontal X axis component.</summary>
        public int X { get; }

        /// <summary>Horizontal Y axis component.</summary>
        public int Y { get; }

        /// <summary>Vertical Z axis component.</summary>
        public int Z { get; }

        /// <summary>
        /// Human-readable coordinate format for logs/debugging.
        /// </summary>
        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
