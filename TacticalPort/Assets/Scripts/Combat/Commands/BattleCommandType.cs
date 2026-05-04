#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

namespace TacticalPort.Combat
{
    public enum BattleCommandType
    {
        Move = 0,
        UseSkill = 1,
        EndTurn = 2,
        PlaceUnit = 3,
        ReadyPlacement = 4
    }
}
