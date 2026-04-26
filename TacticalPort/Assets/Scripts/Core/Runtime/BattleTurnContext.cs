using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class BattleTurnContext
    {
        #region _____________________________| INIT

        public BattleTurnContext(int pRoundIndex, int pTurnIndex, BattleUnitId pUnitId)
        {
            RoundIndex = pRoundIndex;
            TurnIndex = pTurnIndex;
            UnitId = pUnitId;
        }

        #endregion

        #region _____________________________| ACCESSORS

        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public BattleUnitId UnitId { get; }

        #endregion
    }
}
