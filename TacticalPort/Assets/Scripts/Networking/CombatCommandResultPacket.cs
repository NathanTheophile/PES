#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using PurrNet.Packing;
using TacticalPort.Core;

namespace TacticalPort.Networking
{
    public struct CombatCommandResultPacket : IPackedAuto
    {
        public bool IsSuccess;
        public int ActionType;
        public string Message;

        public BattleActionResult ToResult() =>
            IsSuccess
                ? BattleActionResult.Succeeded((BattleActionType)ActionType, Message)
                : BattleActionResult.Failed((BattleActionType)ActionType, Message);

        public static CombatCommandResultPacket From(BattleActionResult pResult) =>
            new CombatCommandResultPacket
            {
                IsSuccess = pResult != null && pResult.IsSuccess,
                ActionType = pResult != null ? (int)pResult.ActionType : (int)BattleActionType.Move,
                Message = pResult != null ? pResult.Message : "No command result."
            };
    }
}
