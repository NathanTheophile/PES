#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

namespace TacticalPort.Core.Runtime
{
    public enum BattleActionType
    {
        Move = 0,
        Skill = 1,
        EndTurn = 2,
        Hazard = 3,
        Placement = 4
    }
}
