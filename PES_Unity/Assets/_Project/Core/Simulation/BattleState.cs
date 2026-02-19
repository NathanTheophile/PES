using System.Collections.Generic;

namespace PES.Core.Simulation
{
    public sealed class BattleState
    {
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
    }
}
