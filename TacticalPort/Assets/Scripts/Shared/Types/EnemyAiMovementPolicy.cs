#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Shared
#endregion

namespace TacticalPort.Shared
{
    public enum EnemyAiMovementPolicy
    {
        Auto = 0,
        Approach = 1,
        KeepDistance = 2,
        FleeIfThreatened = 3,
        KiteAfterActing = 4
    }
}
