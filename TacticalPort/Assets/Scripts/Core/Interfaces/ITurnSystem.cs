using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;

namespace TacticalPort.Core.Interfaces
{
    public interface ITurnSystem
    {
        BattleTurnContext CurrentTurn { get; }
        int RoundIndex { get; }

        void Initialize(IEnumerable<BattleUnitRuntime> units);
        bool TryStartNextTurn(out BattleTurnContext turnContext);
        void CompleteCurrentTurn();
        void AddUnit(BattleUnitRuntime unit);
        void RemoveUnit(BattleUnitId unitId);
    }
}
