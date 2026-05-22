#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Shared
#endregion

namespace TacticalPort.Shared
{
    public enum BattlePhase
    {
        Setup = 0,
        Placement = 1,
        AwaitingAction = 2,
        ResolvingAction = 3,
        TurnEnd = 4,
        Completed = 5
    }
}
