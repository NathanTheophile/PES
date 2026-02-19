using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public sealed class BattleState
    {
        private readonly Dictionary<EntityId, Position3> _entityPositions = new();
        private readonly List<string> _eventLog = new();

        public IReadOnlyList<string> EventLog => _eventLog;

        public int Tick { get; private set; }

        public void AdvanceTick()
        {
            Tick++;
        }

        public void AddEvent(string evt)
        {
            _eventLog.Add(evt);
        }

        public void SetEntityPosition(EntityId entityId, Position3 position)
        {
            _entityPositions[entityId] = position;
        }

        public bool TryGetEntityPosition(EntityId entityId, out Position3 position)
        {
            return _entityPositions.TryGetValue(entityId, out position);
        }

        public bool TryMoveEntity(EntityId entityId, Position3 expectedOrigin, Position3 destination)
        {
            if (!_entityPositions.TryGetValue(entityId, out var current) || !current.Equals(expectedOrigin))
            {
                return false;
            }

            _entityPositions[entityId] = destination;
            return true;
        }
    }

    public readonly struct Position3
    {
        public Position3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
