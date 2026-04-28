#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class BattleTurnContext
    {
        #region _____________________________| INIT

        public BattleTurnContext(int pRoundIndex, int pTurnIndex, UnitId pUnitId)
        {
            RoundIndex = pRoundIndex;
            TurnIndex = pTurnIndex;
            UnitId = pUnitId;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public UnitId UnitId { get; }

        #endregion
    }
}
