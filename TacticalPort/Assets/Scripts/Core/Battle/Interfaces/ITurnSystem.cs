#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public interface ITurnSystem
    {
        BattleTurnContext CurrentTurn { get; }
        int RoundIndex { get; }

        void Initialize(IEnumerable<UnitRuntime> units);
        bool TryStartNextTurn(out BattleTurnContext turnContext);
        void CompleteCurrentTurn();
        void AddUnit(UnitRuntime unit, UnitId afterUnitId = default);
        void RemoveUnit(UnitId unitId);
    }
}
